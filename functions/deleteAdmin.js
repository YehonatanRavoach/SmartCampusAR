/* eslint-disable indent */
/* eslint-disable require-jsdoc */
const functions = require("firebase-functions");
const admin = require("firebase-admin");

const db = admin.firestore();
const bucket = admin.storage().bucket();

exports.deleteAdmin = functions.https.onCall(async (data, context) => {
  // --- 1. Auth Check ---
  if (!context.auth || context.auth.token.role !== "sysadmin") {
      throw new functions.https.HttpsError("permission-denied",
          "Only sysadmin can delete admins.");
  }

  const requestingEmail = context.auth.token.email;
  const adminId = data.adminId;

  if (!adminId) {
      throw new functions.https.HttpsError("invalid-argument",
          "adminId is required.");
  }

  // --- 2. Fetch Admin Document ---
  const adminRef = db.collection("Admin_Profiles").doc(adminId);
  const adminSnap = await adminRef.get();

  if (!adminSnap.exists) {
    throw new functions.https.HttpsError("not-found", "Admin not found.");
  }

  const adminData = adminSnap.data();
  const adminEmail = adminData.email;
  const campusId = adminData.campusId;

  if (!adminEmail || !campusId) {
      throw new functions.https.HttpsError("invalid-argument",
          "Admin document is missing required fields.");
  }

  // --- 3. Prevent Self-Deletion ---
  if (adminEmail === requestingEmail) {
      throw new functions.https.HttpsError("failed-precondition",
          "Sysadmin cannot delete themselves.");
  }

  // --- 4. Fetch Campus Document ---
  const campusRef = db.collection("Campuses").doc(campusId);
  const campusSnap = await campusRef.get();

  if (!campusSnap.exists) {
    throw new functions.https.HttpsError("not-found", "Campus not found.");
  }

  const campusData = campusSnap.data();
  const storageFolder = campusData.storageFolder;
  const adminIdList = campusData.adminId || [];

  // --- 5. Delete Auth User ---
  try {
    const userRecord = await admin.auth().getUserByEmail(adminEmail);
    await admin.auth().deleteUser(userRecord.uid);
  } catch (err) {
    console.warn("⚠️ Failed to delete Auth user:", err.message);
  }

  // --- 6. Delete Admin Document ---
  await adminRef.delete();

  // --- 7. Delete Admin's Storage Folder ---
  const adminFolderPath = `campuses/${storageFolder}/Meta/${adminId}/`;
  const [adminFiles] = await bucket.getFiles({prefix: adminFolderPath});
  await Promise.all(adminFiles.map((file) => file.delete()));

  // --- 8. Update or Delete Campus ---
  if (adminIdList.length <= 1) {
    // Last admin — delete entire campus
    await recursiveDelete(campusRef);

    const campusStoragePath = `campuses/${storageFolder}/`;
    const [campusFiles] = await bucket.getFiles({prefix: campusStoragePath});
    await Promise.all(campusFiles.map((file) => file.delete()));

    return {success: true, message: `Admin and campus deleted.`};
  } else {
    // Remove admin from campus admin list
    const updatedAdmins = adminIdList.filter((id) => id !== adminId);
    await campusRef.update({adminId: updatedAdmins});

    return {success: true, message: `Admin deleted.`};
  }
});

// --- 🔁 SDK-Based Recursive Delete (No npx) ---
// eslint-disable-next-line require-jsdoc
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
      await deleteCollection(collectionRef); // Continue recursively
    }
  }

  const subcollections = await docRef.listCollections();
  for (const sub of subcollections) {
    await deleteCollection(sub);
  }

  await docRef.delete(); // Delete the main doc
}
