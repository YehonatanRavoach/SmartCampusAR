/* eslint-disable no-useless-escape */
/* eslint-disable max-len */
const express = require("express");
const cors = require("cors");
const {Readable} = require("stream");
const admin = require("firebase-admin");
const {v4: uuidv4} = require("uuid");
const fileParser = require("express-multipart-file-parser");
const nodemailer = require("nodemailer");

const app = express();

app.use(cors({origin: true}));
app.use(fileParser);

const notifySysadminAboutNewRequest = async ({adminName, email, campusName}) => {
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
    subject: "New Campus Management Request",
    text: `
A new campus management request has been submitted.

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

app.post("/registerAdmin", async (req, res) => {
  try {
    const bucket = admin.storage().bucket();
    const now = admin.firestore.Timestamp.now();

    const {
      email,
      password,
      adminName,
      role,
      campusName,
      city,
      country,
      description,
    } = req.body;

    if (!email || !password || !adminName || !role || !campusName || !city || !country) {
      return res.status(400).json({error: "Missing required form fields."});
    }

    const logo = req.files.find((f) => f.fieldname === "logo");
    const map = req.files.find((f) => f.fieldname === "map");
    const approval = req.files.find((f) => f.fieldname === "approval");
    const profilePicture = req.files.find((f) => f.fieldname === "profilePicture");

    if (!logo || !map || !approval || !profilePicture) {
      return res.status(400).json({error: "Missing required file uploads."});
    }

    const campusQuery = await admin.firestore()
        .collection("Campuses")
        .where("name", "==", campusName)
        .limit(1)
        .get();

    if (!campusQuery.empty) {
      return res.status(400).json({error: "Campus name already exists."});
    }

    let userRecord;
    try {
      userRecord = await admin.auth().getUserByEmail(email);
      if (userRecord) {
        return res.status(400).json({error: "Email is already registered."});
      }
    } catch (err) {
      if (err.code !== "auth/user-not-found") {
        console.error("Firebase Auth lookup error:", err);
        return res.status(500).json({error: "Error checking email."});
      }
    }

    userRecord = await admin.auth().createUser({email, password});
    const uid = userRecord.uid;

    const sanitize = (str) =>
      str.trim().replace(/[\/\\\?\%\*\:\|\"<>\.]/g, "_");

    const campusId = uuidv4();
    const campusFolder = sanitize(campusName);
    const campusPath = `campuses/${campusFolder}`;

    const uploadFile = async (file, destination) => {
      const token = uuidv4();
      const metadata = {
        metadata: {
          firebaseStorageDownloadTokens: token,
        },
        contentType: file.mimetype,
      };

      const fileRef = bucket.file(destination);
      const stream = fileRef.createWriteStream({metadata});
      Readable.from(file.buffer).pipe(stream);

      await new Promise((resolve, reject) => {
        stream.on("finish", resolve);
        stream.on("error", reject);
      });

      return `https://firebasestorage.googleapis.com/v0/b/${bucket.name}/o/${encodeURIComponent(destination)}?alt=media&token=${token}`;
    };

    const uploadedUrls = {
      logoURL: await uploadFile(logo, `${campusPath}/Meta/logo`),
      mapURL: await uploadFile(map, `${campusPath}/Meta/map`),
      approvalURL: await uploadFile(approval, `${campusPath}/Meta/${uid}/approval/${approval.originalname}`),
      profilePictureURL: await uploadFile(profilePicture, `${campusPath}/Meta/${uid}/profile/${profilePicture.originalname}`),
    };

    await admin.firestore().collection("Campuses").doc(campusId).set({
      name: campusName,
      city,
      country,
      description: description || "",
      logoURL: uploadedUrls.logoURL,
      mapImageURL: uploadedUrls.mapURL,
      storageFolder: campusFolder,
      status: "pending",
      createdAt: now,
      adminId: [uid],
    });

    await admin.firestore()
        .collection("Campuses")
        .doc(campusId)
        .collection("Buildings")
        .doc("_placeholder")
        .set({createdAt: now});

    await admin.firestore().collection("Admin_Profiles").doc(uid).set({
      adminName,
      email,
      role,
      campusId,
      status: "Pending",
      createdAt: now,
      employeeApprovalFileURL: uploadedUrls.approvalURL,
      adminPhotoURL: uploadedUrls.profilePictureURL || null,
    });

    await notifySysadminAboutNewRequest({adminName, email, campusName});

    return res.status(200).json({
      success: true,
      message: "Admin registered successfully.",
      adminUID: uid,
      campusId,
      uploadedUrls,
    });
  } catch (error) {
    console.error("Registration error:", error);
    return res.status(500).json({error: "Internal Server Error"});
  }
});

module.exports = {app};
