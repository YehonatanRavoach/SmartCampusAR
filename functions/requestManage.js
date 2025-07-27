/* eslint-disable max-len */
const express = require("express");
const cors = require("cors");
const admin = require("firebase-admin");
const {Readable} = require("stream");
const {v4: uuidv4} = require("uuid");
const fileParser = require("express-multipart-file-parser");
const nodemailer = require("nodemailer");

const app = express();
app.use(cors({origin: true}));
app.use(fileParser);

// helper – "H.I.T Campus!" → "hit_campus"
const sanitize = (str) =>
  str.trim().toLowerCase().replace(/[^a-z0-9]+/g, "_").replace(/^_|_$/g, "");

const notifySysadminAboutManageRequest = async ({adminName, email, campusName}) => {
  const transporter = nodemailer.createTransport({
    service: "gmail",
    auth: {
      user: "smartcampusar@gmail.com",
      pass: "dwvp infc tysv vtcu",
    },
  });

  const mailOptions = {
    from: "smartcampusar@gmail.com",
    to: "smartcampusar@gmail.com",
    subject: "New Request to Manage Existing Campus",
    text: `
A new request has been submitted to manage an existing campus.

Admin Name: ${adminName}
Email: ${email}
Campus: ${campusName}

Please review this request in the system.
    `.trim(),
  };

  try {
    const info = await transporter.sendMail(mailOptions);
    console.log("System email sent to sysadmin:", info.response);
  } catch (error) {
    console.error("Failed to send sysadmin email:", error);
  }
};

app.post("/requestManage", async (req, res) => {
  try {
    /* ------------------------------------------------------------------
             1. Validate form fields                                           */
    const {email, password, adminName, role, campusName} = req.body;
    if (!email || !password || !adminName || !role || !campusName) {
      return res.status(400).json({error: "Missing required fields."});
    }

    /* ------------------------------------------------------------------
            2. Create Auth user                                               */
    const {uid} = await admin.auth().createUser({email, password});


    /* ------------------------------------------------------------------
             3. Find campus by name → get campusId                            */
    const campusSnap = await admin.firestore()
        .collection("Campuses")
        .where("name", "==", campusName)
        .limit(1)
        .get();

    if (campusSnap.empty) {
      return res.status(404).json({error: "Campus not found"});
    }

    const campusDoc = campusSnap.docs[0];
    const campusId = campusDoc.id;
    const campusFolder = campusDoc.get("storageFolder") || sanitize(campusDoc.get("name"));
    const adminFolder = `campuses/${campusFolder}/Meta/${uid}`;

    /* ------------------------------------------------------------------
             4. Upload optional files                                         */
    const bucket = admin.storage().bucket();
    const uploadFile = async (file, destination) => {
      const token = uuidv4();
      const metadata = {
        contentType: file.mimetype,
        metadata: {firebaseStorageDownloadTokens: token},
      };
      const fileRef = bucket.file(destination);
      const stream = fileRef.createWriteStream({metadata});
      Readable.from(file.buffer).pipe(stream);
      await new Promise((ok, err) => {
        stream.on("finish", ok); stream.on("error", err);
      });
      return `https://firebasestorage.googleapis.com/v0/b/${bucket.name}/o/${encodeURIComponent(destination)}?alt=media&token=${token}`;
    };

    const approval = req.files.find((f) => f.fieldname === "approval");
    const profilePhoto = req.files.find((f) => f.fieldname === "profilePhoto");
    const uploads = {};

    if (approval) {
      uploads.employeeApprovalFileURL =
                await uploadFile(approval, `${adminFolder}/approval/${approval.originalname}`);
    }
    if (profilePhoto) {
      uploads.adminPhotoURL =
                await uploadFile(profilePhoto, `${adminFolder}/profile/${profilePhoto.originalname}`);
    }

    /* ------------------------------------------------------------------
             5. Save admin request with status: "pending"
        ------------------------------------------------------------------ */
    const now = admin.firestore.FieldValue.serverTimestamp();
    await admin.firestore().runTransaction(async (tx) => {
      const campusRef = admin.firestore().collection("Campuses").doc(campusId);

      tx.update(campusRef, {
        adminId: admin.firestore.FieldValue.arrayUnion(uid),
        updatedAt: now,
      });

      tx.set(admin.firestore().collection("Admin_Profiles").doc(uid), {
        adminName,
        email,
        role,
        campusId,
        status: "pending",
        createdAt: now,
        ...uploads,
      });
    });

    await notifySysadminAboutManageRequest({adminName, email, campusName});

    return res.status(200).json({success: true, newAdminUid: uid});
  } catch (err) {
    console.error("requestManage error:", err);
    return res.status(500).json({error: err.message});
  }
});


module.exports = {app};

