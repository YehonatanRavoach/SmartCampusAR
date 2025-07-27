/* eslint-disable max-len */
/* eslint-disable no-unused-vars */

// --- Initialize Firebase Admin SDK ---
const {initializeApp} = require("firebase-admin/app");
initializeApp();

const functions = require("firebase-functions");

// --- Express Apps ---
const {app: registerAdminApp} = require("./registerAdmin");
const {app: requestManageApp} = require("./requestManage");

// --- Callable (onCall) Functions ---
const {setManualClaims} = require("./setManualClaims");
const {deleteCampus} = require("./deleteCampus");
const {deleteAdmin} = require("./deleteAdmin");
const {setAdminStatus} = require("./setAdminStatus");
const {setCampusStatus} = require("./setCampusStatus");

// --- Simple HTTP Functions ---
const {checkCampusExists} = require("./checkCampusExists");
const {checkEmailExists} = require("./checkEmailExists");

// --- Cleanup Functions (scheduled + http) ---
const {scheduledCleanupRejected, httpCleanupRejected} = require("./cleanupRejected");

// --- Other Functions ---
const {resetUserPassword} = require("./resetUserPassword");

// --- Expose HTTP endpoints (Express) ---
exports.registerAdmin = functions.https.onRequest(registerAdminApp);
exports.requestManage = functions.https.onRequest(requestManageApp);

// --- Expose callable (onCall) endpoints ---
exports.setManualClaims = setManualClaims;
exports.deleteCampus = deleteCampus;
exports.deleteAdmin = deleteAdmin;
exports.setAdminStatus = setAdminStatus;
exports.setCampusStatus = setCampusStatus;

// --- Expose simple HTTP functions ---
exports.checkCampusExists = checkCampusExists;
exports.checkEmailExists = checkEmailExists;

// --- Expose scheduled and HTTP cleanup functions ---
exports.scheduledCleanupRejected = scheduledCleanupRejected;
exports.httpCleanupRejected = httpCleanupRejected;

// --- Expose other functions ---
exports.resetUserPassword = resetUserPassword;
