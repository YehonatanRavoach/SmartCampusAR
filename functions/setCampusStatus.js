/* eslint-disable indent */
/* eslint-disable require-jsdoc */
/* eslint-disable max-len, curly, object-curly-spacing, comma-dangle, brace-style, valid-jsdoc */

/**
 * Cloud Function: setCampusStatus
 * -------------------------------
 * Updates the status of a campus and synchronizes the status (and custom claims) of its admins.
 *
 * Logic:
 *   - pending → active: campus to 'active', ONLY first admin to 'active' (+claims), others unchanged
 *   - active/pending → reject: campus to 'reject', ALL admins to 'reject', remove claims from all
 *   - active → pending: campus to 'pending', ALL admins to 'pending', claims unchanged
 *   - reject → pending: campus to 'pending', ALL admins to 'pending', claims unchanged
 *   - reject → active: campus to 'active', ONLY first admin to 'active' (+claims), others unchanged
 *   - All other transitions: error
 *
 * All status values are always lowercase ('active', 'pending', 'reject').
 * All custom claims ('role') are always lowercase ('admin').
 *
 * Input:
 *   - campusId   (string, required): The Firestore document ID of the campus.
 *   - newStatus  (string, required): The new status to assign ("active", "pending", "reject").
 *
 * Output:
 *   - { success: true, message: string }
 *
 * Errors:
 *   - permission-denied, not-found, invalid-argument, failed-precondition, internal
 */

const functions = require("firebase-functions");
const admin = require("firebase-admin");

exports.setCampusStatus = functions.https.onCall(async (data, context) => {
    // ───────────── Auth & Permissions ─────────────
    if (!context.auth)
        throw new functions.https.HttpsError("unauthenticated", "You must be signed in.");
    if (context.auth.token.role !== "sysadmin")
        throw new functions.https.HttpsError("permission-denied", "Only sysadmins can update campus status.");

    // ───────────── Input Validation ─────────────
    const campusId = data && data.campusId ? data.campusId : null;
    const newStatus = data && data.newStatus ? String(data.newStatus).toLowerCase() : null;

    if (!campusId || !["active", "pending", "reject"].includes(newStatus))
        throw new functions.https.HttpsError("invalid-argument", "campusId and valid newStatus ('active', 'pending', 'reject') are required.");

    // ───────────── Fetch Campus Document ─────────────
    const db = admin.firestore();
    const campusRef = db.collection("Campuses").doc(campusId);
    const campusSnap = await campusRef.get();

    if (!campusSnap.exists)
        throw new functions.https.HttpsError("not-found", `Campuses/${campusId} does not exist.`);

    const campusData = campusSnap.data();
    const prevStatus = (campusData.status || "").toLowerCase();
    const adminIdList = Array.isArray(campusData.adminId) ? campusData.adminId : [];

    // ───────────── Transition Logic ─────────────

    // Define valid transitions
    const isPendingToActive = (prevStatus === "pending" || prevStatus === "reject") && newStatus === "active";
    const isActiveOrPendingToReject = (["active", "pending"].includes(prevStatus)) && newStatus === "reject";
    const isActiveToPending = prevStatus === "active" && newStatus === "pending";
    const isRejectToPending = prevStatus === "reject" && newStatus === "pending";

    if (
        !isPendingToActive &&
        !isActiveOrPendingToReject &&
        !isActiveToPending &&
        !isRejectToPending
    ) {
        throw new functions.https.HttpsError(
            "failed-precondition",
            `Transition from ${prevStatus} to ${newStatus} is not allowed.`
        );
    }

    // ───────────── Update Campus Status ─────────────
    await campusRef.update({ status: newStatus });

    // ───────────── Admins Handling ─────────────
    let updatedAdmins = 0;

    // pending/reject → active: only first admin to 'active', claims, others unchanged
    if (isPendingToActive) {
        if (adminIdList.length > 0) {
            const firstAdminId = adminIdList[0];
            const adminRef = db.collection("Admin_Profiles").doc(firstAdminId);
            const adminSnap = await adminRef.get();
            if (adminSnap.exists) {
                await adminRef.update({ status: "active" });
                updatedAdmins++;

                // Set custom claims
                const adminEmail = adminSnap.data().email;
                const authUser = await getUserByEmailSafe(adminEmail);
                if (authUser) {
                    await admin.auth().setCustomUserClaims(authUser.uid, { role: "admin", campusId });
                }
            }
        }
        // All other admins unchanged
    }
    // active/pending → reject: ALL admins to 'reject', remove claims
    else if (isActiveOrPendingToReject) {
        for (const adminId of adminIdList) {
            const adminRef = db.collection("Admin_Profiles").doc(adminId);
            const adminSnap = await adminRef.get();
            if (!adminSnap.exists) continue;

            await adminRef.update({ status: "reject" });
            updatedAdmins++;

            // Remove custom claims
            const adminEmail = adminSnap.data().email;
            const authUser = await getUserByEmailSafe(adminEmail);
            if (authUser) {
                await admin.auth().setCustomUserClaims(authUser.uid, null);
            }
        }
    }
    // active/reject → pending: ALL admins to 'pending', claims unchanged
    else if (isActiveToPending || isRejectToPending) {
        for (const adminId of adminIdList) {
            const adminRef = db.collection("Admin_Profiles").doc(adminId);
            const adminSnap = await adminRef.get();
            if (!adminSnap.exists) continue;

            await adminRef.update({ status: "pending" });
            updatedAdmins++;
            // Do NOT touch claims
        }
    }

    // ───────────── Response ─────────────
    return {
        success: true,
        message: `Campus status updated to '${newStatus}' (from '${prevStatus}'), ${updatedAdmins} admin(s) updated.`,
    };
});

/**
 * Helper: getUserByEmailSafe
 * @param {string} email
 * @returns {Promise<admin.auth.UserRecord|null>}
 */
async function getUserByEmailSafe(email) {
    try {
        return await admin.auth().getUserByEmail(email);
    } catch (err) {
        return null;
    }
}
