/* eslint-disable require-jsdoc */
/* eslint-disable max-len */
const functions = require("firebase-functions");
const admin = require("firebase-admin");
const {getStorage} = require("firebase-admin/storage");
const {getFirestore} = require("firebase-admin/firestore");


exports.deleteCampus = functions.https.onCall(async (data, context) => {
  const campusId = data.campusId;
  const db = getFirestore();
  const bucket = getStorage().bucket();

  if (!context.auth || context.auth.token.role !== "sysadmin") {
    throw new functions.https.HttpsError("permission-denied", "Only sysadmin can delete campuses.");
  }

  if (!campusId) {
    throw new functions.https.HttpsError("invalid-argument", "campusId is required.");
  }

  try {
    const campusRef = db.collection("Campuses").doc(campusId);
    const campusSnap = await campusRef.get();

    if (!campusSnap.exists) {
      throw new functions.https.HttpsError("not-found", `Campus ${campusId} not found.`);
    }

    const campusData = campusSnap.data();
    const storageFolder = campusData.storageFolder;
    const adminIds = campusData.adminId || [];

    // 1. Delete each admin (Firestore + Firebase Auth)
    for (const adminId of adminIds) {
      const adminDocRef = db.collection("Admin_Profiles").doc(adminId);
      const adminDoc = await adminDocRef.get();

      if (!adminDoc.exists) continue;

      const adminEmail = adminDoc.data().email;

      try {
        const userRecord = await admin.auth().getUserByEmail(adminEmail);
        await admin.auth().deleteUser(userRecord.uid);
      } catch (err) {
        console.warn(`Failed to delete auth user ${adminEmail}:`, err.message);
      }

      await adminDocRef.delete();
    }

    // 2. Delete the campus and all its subcollections
    await recursiveDelete(campusRef);

    // 3. Delete Storage folder
    await deleteStorageFolder(bucket, `campuses/${storageFolder}/`);

    return {success: true, message: `Campus ${campusId} deleted.`};
  } catch (error) {
    console.error("Error deleting campus:", error);
    throw new functions.https.HttpsError("internal", error.message);
  }
});

async function recursiveDelete(docRef) {
  const db = require("firebase-admin").firestore();
  const batchSize = 50;

  async function deleteCollection(collectionRef) {
    const query = collectionRef.limit(batchSize);
    const snapshot = await query.get();

    if (snapshot.empty) return;

    const batch = db.batch();
    snapshot.docs.forEach((doc) => batch.delete(doc.ref));
    await batch.commit();

    if (snapshot.size === batchSize) {
      // Might be more docs to delete
      await deleteCollection(collectionRef);
    }
  }

  const subcollections = await docRef.listCollections();
  for (const sub of subcollections) {
    await deleteCollection(sub);
  }

  await docRef.delete();
}


async function deleteStorageFolder(bucket, folderPath) {
  const [files] = await bucket.getFiles({prefix: folderPath});
  const deletePromises = files.map((file) => file.delete());
  await Promise.all(deletePromises);
}
