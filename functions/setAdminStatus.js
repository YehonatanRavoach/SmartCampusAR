/* eslint-disable indent */
/* eslint-disable require-jsdoc */
/* eslint-disable max-len, curly, object-curly-spacing, comma-dangle, brace-style, valid-jsdoc */

/**
 * Cloud Function: setAdminStatus
 * ------------------------------
 * Updates the status of an admin and manages their Firebase Auth custom claims.
 *
 * Workflow:
 *   - Only callable by a signed-in sysadmin.
 *   - Updates the admin's "status" field in Firestore ("Admin_Profiles").
 *   - Sets or removes custom claims based on the new status.
 *   - Status transitions:
 *     - Pending → Active:   Set status to "Active", assign custom claims.
 *     - Pending → Reject:   Set status to "Reject", remove custom claims.
 *     - Active → Reject:    Set status to "Reject", remove custom claims.
 *     - Active → Pending:   Set status to "Pending", remove custom claims.
 *     - Reject → Any:       (not allowed, must recreate admin)
 *
 * Input:
 *   - adminId   (string, required): The Firestore document ID of the admin.
 *   - newStatus (string, required): The new status to assign ("active", "pending", "reject").
 *
 * Output:
 *   - { success: true, message: string }
 *
 * Errors:
 *   - permission-denied, not-found, invalid-argument, failed-precondition, internal
 */

const functions = require("firebase-functions");
const admin = require("firebase-admin");

exports.setAdminStatus = functions.https.onCall(async (data, context) => {
    // ───────────── Auth & Permissions ─────────────
    if (!context.auth)
        throw new functions.https.HttpsError("unauthenticated", "You must be signed in.");
    if (context.auth.token.role !== "sysadmin")
        throw new functions.https.HttpsError("permission-denied", "Only sysadmins can update admin status.");

    // ───────────── Input Validation ─────────────
    const adminId = data && data.adminId ? data.adminId : null;
    const newStatus = data && data.newStatus ? String(data.newStatus).toLowerCase() : null;

    if (!adminId || !["active", "pending", "reject"].includes(newStatus))
        throw new functions.https.HttpsError("invalid-argument", "adminId and valid newStatus ('active', 'pending', 'reject') are required.");

    // ───────────── Fetch Admin Document ─────────────
    const db = admin.firestore();
    const adminRef = db.collection("Admin_Profiles").doc(adminId);
    const adminSnap = await adminRef.get();

    if (!adminSnap.exists)
        throw new functions.https.HttpsError("not-found", `Admin_Profiles/${adminId} does not exist.`);

    const adminData = adminSnap.data();
    const adminEmail = adminData.email;
    const campusId = adminData.campusId;

    if (!adminEmail || !campusId)
        throw new functions.https.HttpsError("failed-precondition", "Admin profile is missing email or campusId.");

    // ───────────── Transition Logic ─────────────
      // Update status in Firestore
    await adminRef.update({ status: newStatus });

    // Handle custom claims according to status transition
    const authUser = await getUserByEmailSafe(adminEmail);
    if (authUser) {
        if (newStatus === "active") {
            // Assign custom claims
            await admin.auth().setCustomUserClaims(authUser.uid, { role: "admin", campusId });
        } else {
            // Remove all claims (set to null)
            await admin.auth().setCustomUserClaims(authUser.uid, null);
        }
    }

    // ───────────── Response ─────────────
    return {
        success: true,
        message: `Admin ${adminEmail} status updated to ${newStatus.toUpperCase()} and custom claims handled.`,
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
        // User not found in Auth (may have been deleted)
        return null;
    }
}
