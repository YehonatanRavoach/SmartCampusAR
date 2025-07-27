const functions = require("firebase-functions");
const admin = require("firebase-admin");

exports.checkEmailExists = functions.https.onRequest(async (req, res) => {
  res.set("Access-Control-Allow-Origin", "*");
  res.set("Access-Control-Allow-Headers", "Content-Type");
  if (req.method === "OPTIONS") return res.status(204).send("");

  const {email} = req.body;
  if (!email) return res.status(400).json({error: "Missing email."});
  try {
    await admin.auth().getUserByEmail(email);
    return res.status(200).json({exists: true});
  } catch (err) {
    if (err.code === "auth/user-not-found") {
      return res.status(200).json({exists: false});
    }
    console.error("CF checkEmailExists error:", err);
    return res.status(500).json({error: "Server error."});
  }
});
