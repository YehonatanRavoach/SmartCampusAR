// functions/setManualClaims.js
const functions = require("firebase-functions");
const admin = require("firebase-admin");

exports.setManualClaims = functions.https.onRequest(async (req, res) => {
  try {
    const email = req.query.email || req.body.email;
    const campusId = req.query.campusId || req.body.campusId;
    const role = req.query.role || req.body.role || "admin";

    if (!email || !campusId) {
      return res.status(400).json({error: "Missing email or campusId"});
    }

    const user = await admin.auth().getUserByEmail(email);
    await admin.auth().setCustomUserClaims(user.uid, {role, campusId});

    return res.json({
      message: `Custom claims set for ${email}`,
      claims: {role, campusId},
    });
  } catch (err) {
    console.error("setManualClaims error:", err);
    res.status(500).json({error: err.message});
  }
});
