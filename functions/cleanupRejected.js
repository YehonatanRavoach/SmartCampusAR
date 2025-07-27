/* eslint-disable linebreak-style */
/* eslint-disable require-jsdoc */
/* eslint-disable max-len */
/* eslint-disable linebreak-style */
/**
 * Cloud Function: cleanupRejected
 * -------------------------------
 * Deletes all admins and campuses with status 'reject' from Firestore,
 * removes their Firebase Auth users, updates admin lists, and deletes all relevant Storage folders.
 *
 * - Runs scheduled every Sunday at 03:00 Israel time.
 * - Also available as an HTTP function for manual triggering (POST request).
 * - No authentication required (CAUTION: secure for production!).
 *
 * Logic:
 * 1. For each admin with status 'reject':
 *    - Remove from Auth (if email exists)
 *    - Remove admin doc from Firestore
 *    - Remove from adminId[] array in their campus
 *    - Delete their Storage folder
 *    - If it was the last admin in campus, delete the campus too (recursively, with all Storage)
 * 2. For each campus with status 'reject':
 *    - Delete ALL its admins (regardless of their status):
 *        - Remove from Auth, Firestore, Storage
 *    - Delete the campus doc and all subcollections
 *    - Delete its Storage folder
 *
 * Output: { success: true, deletedAdmins: X, deletedCampuses: Y }
 */

const functions = require("firebase-functions");
const admin = require("firebase-admin");
const db = admin.firestore();
const bucket = admin.storage().bucket();

// --- Helper: Recursively delete all subcollections and the doc itself ---
async function recursiveDelete(docRef) {
  const batchSize = 50;
  async function deleteCollection(collectionRef) {
    const query = collectionRef.limit(batchSize);
    const snapshot = await query.get();
    if (snapshot.empty) return;
    const batch = db.batch();
    snapshot.docs.forEach((doc) => batch.delete(doc.ref));
    await batch.commit();
    if (snapshot.size === batchSize) {
      await deleteCollection(collectionRef);
    }
  }
  const subcollections = await docRef.listCollections();
  for (const sub of subcollections) {
    await deleteCollection(sub);
  }
  await docRef.delete();
}

// --- Helper: Delete all files under a folder in Storage ---
async function deleteStorageFolder(folderPath) {
  const [files] = await bucket.getFiles({prefix: folderPath});
  await Promise.all(files.map((file) => file.delete()));
}

// --- Helper: Delete admin from all places (Firestore, Auth, Storage, update campus) ---
async function deleteAdminById(adminId) {
  const adminRef = db.collection("Admin_Profiles").doc(adminId);
  const adminSnap = await adminRef.get();
  if (!adminSnap.exists) return false;

  const adminData = adminSnap.data();
  const adminEmail = adminData.email;
  const campusId = adminData.campusId;

  // Remove from Auth
  if (adminEmail) {
    try {
      const userRecord = await admin.auth().getUserByEmail(adminEmail);
      await admin.auth().deleteUser(userRecord.uid);
    } catch (err) {
      console.warn(`⚠️ Failed to delete Auth user (${adminEmail}):`, err.message);
    }
  }

  // Remove from Firestore
  await adminRef.delete();

  // Remove from Storage
  if (campusId) {
    // Get campus storageFolder
    const campusSnap = await db.collection("Campuses").doc(campusId).get();
    if (campusSnap.exists) {
      const storageFolder = campusSnap.data().storageFolder || campusId;
      const adminFolderPath = `campuses/${storageFolder}/Meta/${adminId}/`;
      await deleteStorageFolder(adminFolderPath);

      // Remove admin from campus adminId array
      let adminIdList = campusSnap.data().adminId || [];
      if (adminIdList.includes(adminId)) {
        adminIdList = adminIdList.filter((id) => id !== adminId);
        await db.collection("Campuses").doc(campusId).update({adminId: adminIdList});

        // If now no admins left, delete the campus as well (recursively, including storage)
        if (adminIdList.length === 0) {
          await deleteCampusById(campusId);
        }
      }
    }
  }
  return true;
}

// --- Helper: Delete campus, all its admins, storage, and subcollections ---
async function deleteCampusById(campusId) {
  const campusRef = db.collection("Campuses").doc(campusId);
  const campusSnap = await campusRef.get();
  if (!campusSnap.exists) return false;

  const campusData = campusSnap.data();
  const adminIds = campusData.adminId || [];
  const storageFolder = campusData.storageFolder || campusId;

  // Delete ALL admins of the campus (regardless of their status)
  for (const adminId of adminIds) {
    await deleteAdminById(adminId);
  }

  // Delete campus doc (and all subcollections)
  await recursiveDelete(campusRef);

  // Delete storage for campus
  const campusFolderPath = `campuses/${storageFolder}/`;
  await deleteStorageFolder(campusFolderPath);

  return true;
}

/**
 * The main cleanup logic.
 */
async function cleanupRejected() {
  let deletedAdmins = 0;
  let deletedCampuses = 0;
  const processedCampuses = new Set();

  // --- 1. Cleanup REJECTED ADMINS ---
  const adminSnapshot = await db.collection("Admin_Profiles")
      .where("status", "==", "reject").get();

  for (const adminDoc of adminSnapshot.docs) {
    const adminId = adminDoc.id;
    const adminData = adminDoc.data();
    const campusId = adminData.campusId;
    // Call deleteAdminById (handles all cross-updates)
    const wasDeleted = await deleteAdminById(adminId);
    if (wasDeleted) deletedAdmins++;
    // Mark this campus as "already processed" if it gets deleted
    if (campusId) processedCampuses.add(campusId);
  }

  // --- 2. Cleanup REJECTED CAMPUSES ---
  const campusSnapshot = await db.collection("Campuses")
      .where("status", "==", "reject").get();

  for (const campusDoc of campusSnapshot.docs) {
    const campusId = campusDoc.id;
    // If already deleted in step 1 (by admin deletion) — skip
    if (processedCampuses.has(campusId)) continue;
    const wasDeleted = await deleteCampusById(campusId);
    if (wasDeleted) deletedCampuses++;
  }

  return {success: true, deletedAdmins, deletedCampuses};
}

// ---------- Scheduled Weekly Cleanup (every Sunday at 03:00 Israel time) ----------
exports.scheduledCleanupRejected = functions.pubsub
    .schedule("0 0 * * 0")
    .timeZone("Asia/Jerusalem")
    .onRun(async (context) => {
      const result = await cleanupRejected();
      console.log(`[Scheduled] Deleted ${result.deletedAdmins} rejected admins and ${result.deletedCampuses} rejected campuses.`);
      return null;
    });

// ---------- Manual HTTP Cleanup (no authentication) ----------
exports.httpCleanupRejected = functions.https.onRequest(async (req, res) => {
  try {
    const result = await cleanupRejected();
    res.json(result);
  } catch (err) {
    console.error("Cleanup failed:", err);
    res.status(500).json({success: false, error: err.toString()});
  }
});
