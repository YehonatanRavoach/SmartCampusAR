/* eslint-disable linebreak-style */
const functions = require("firebase-functions");
const {getFirestore} = require("firebase-admin/firestore");

const db = getFirestore();

exports.checkCampusExists = functions.https.onRequest(async (req, res) => {
  try {
    const name = ((req.body && req.body.name) || "").trim().toLowerCase();

    if (!name) {
      res.status(400).json({error: "Missing campus name"});
      return;
    }

    const snapshot = await db.collection("Campuses").get();
    let exists = false;

    snapshot.forEach((doc) => {
      const data = doc.data();
      const dbName = (data.name || "").trim().toLowerCase();
      if (dbName === name) {
        exists = true;
      }
    });

    res.status(200).json({exists});
  } catch (error) {
    console.error("Error checking campus name:", error);
    res.status(500).json({error: "Internal server error"});
  }
});
