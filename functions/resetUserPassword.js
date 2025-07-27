/* eslint-disable object-curly-spacing */
/* eslint-disable indent */
// functions/resetUserPassword.js
const functions = require("firebase-functions");
const admin = require("firebase-admin");

exports.resetUserPassword = functions.https.onRequest(async (req, res) => {
    try {
        const email = req.query.email || req.body.email;
        const newPassword = req.query.newPassword || req.body.newPassword;

        if (!email || !newPassword) {
          return res.status(400).json({ error: "Missing email or newPassword"});
        }

        const user = await admin.auth().getUserByEmail(email);
        await admin.auth().updateUser(user.uid, { password: newPassword });

        return res.json({
            message: `Password updated successfully for ${email}`,
        });
    } catch (err) {
        console.error("resetUserPassword error:", err);
        res.status(500).json({ error: err.message });
    }
});
