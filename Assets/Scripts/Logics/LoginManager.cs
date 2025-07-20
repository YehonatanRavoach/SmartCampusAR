using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine.SceneManagement;
using System.Threading;
using System.Threading.Tasks;
/*
    LoginManager.cs

    This script handles user login, Firebase authentication, and session management.
    It retrieves user claims from the JWT token to determine user roles and navigates accordingly.
*/
 
public class LoginManager : MonoBehaviour
{
    /* ─────────── UI refs ─────────── */
    [Header("UI References")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public LoginUI        loginUI;
    public GameObject     loadingSpinner;
 
    /* ─────────── Firebase ─────────── */
    private FirebaseAuth      auth;
    private FirebaseFirestore db;
 
    /* ─────────── Unity lifecycle ─────────── */
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db   = FirebaseFirestore.DefaultInstance;
    }
 
    /* ─────────── Login button ─────────── */
    public void OnLoginClicked()
    {
        loadingSpinner.SetActive(true);
        Thread.Sleep(2000);                        // demo delay
        loginUI.HideMessage();
 
        string email    = emailField.text.Trim();
        string password = passwordField.text;
 
        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
        {
            /* ① sign-in failed */
            if (task.IsFaulted || task.Exception != null)
            {
                string err = task.Exception?.InnerExceptions[0].Message.ToLower() ?? "unknown";
 
                loadingSpinner.SetActive(false);
                if (err.Contains("no user record"))
                    loginUI.ShowUserNotFound();                         // key: err_user_not_found
                else if (err.Contains("password is invalid"))
                    loginUI.ShowWrongPassword();                       // key: err_wrong_password
                else
                    loginUI.ShowMessage("err_login_failed", "error");  // key only
 
                return;
            }
 
            /* ② sign-in ok → fetch claims */
            FirebaseUser user = task.Result.User;
            user.TokenAsync(true).ContinueWithOnMainThread(tokenTask =>
            {
                if (tokenTask.IsFaulted)
                {
                    loadingSpinner.SetActive(false);
                    loginUI.ShowMessage("err_token", "error");
                    return;
                }
 
                string jwt = tokenTask.Result;
                SessionData.Instance.IdToken = jwt;
                Dictionary<string, object> claims = DecodeJwt(jwt);
 
                if (!claims.ContainsKey("role"))
                {
                    loadingSpinner.SetActive(false);
                    loginUI.ShowMessage("err_no_role", "error");
                    return;
                } 
 
                string role = claims["role"].ToString();
                string uid  = user.UserId;
                
 
                /* ③ sysadmin path */
                if (role == "sysadmin")
                {   
                    loadingSpinner.SetActive(false);
                    loginUI.ShowMessage("msg_login_success", "success");              // key: msg_login_success
                    StartCoroutine(loadwaiting("System Administrator 2")); // wait 1.2 s before loading scene
                    
                    return;
                }

                /* ④ campus admin path */
                if (role == "admin")
                {
                    string campusId = claims.ContainsKey("campusId")
                                     ? claims["campusId"].ToString()
                                     : null;

                    if (campusId == null)
                    {
                        loadingSpinner.SetActive(false);
                        loginUI.ShowMessage("err_missing_campus", "error");
                        return;
                    }

                    SessionData.Instance.CampusId = campusId;
                    SessionData.Instance.AdminId = uid;

                    /* admin profile */
                    db.Collection("Admin_Profiles").Document(uid)
                      .GetSnapshotAsync().ContinueWithOnMainThread(profileTask =>
                    {
                        if (profileTask.IsFaulted || !profileTask.Result.Exists)
                        {
                            loadingSpinner.SetActive(false);
                            loginUI.ShowUserNotFound();                 // key: err_user_not_found
                            return;
                        }

                        var doc = profileTask.Result;
                        string status = doc.GetValue<string>("status");
                        string adminName = doc.GetValue<string>("adminName");

                        switch (status)
                        {
                            case "pending":
                                loadingSpinner.SetActive(false);
                                loginUI.ShowWaitingApproval();         // key: info_waiting_approval
                                return;
                            case "reject":
                                loadingSpinner.SetActive(false);
                                loginUI.ShowRejected();                // key: err_registration_rejected
                                return;
                            case "active":
                                break;                                 // continue
                            default:
                                loadingSpinner.SetActive(false);
                                loginUI.ShowMessage("err_account_status", "error");
                                return;
                        }

                        /* fetch campus name (optional) */
                        db.Collection("Campuses").Document(campusId)
                        .GetSnapshotAsync().ContinueWithOnMainThread(
                             campusTask =>       //  ← add async
                        {
                            if (!campusTask.IsFaulted && campusTask.Result.Exists)
                            {
                                var campusName = campusTask.Result.GetValue<string>("name");
                                Debug.Log("Campus Name: " + campusName);
                            }
                    
                            loadingSpinner.SetActive(false);
                            loginUI.ShowMessage("msg_login_success", "success");
                            StartCoroutine(loadwaiting("MyBuildings"));

                        });
                        return;
                    });
                }
 
                
                
            });
        });
    }

    IEnumerator loadwaiting(string name)
    {
        yield return new WaitForSeconds(0.5f);   // or WaitForSecondsRealtime

        SceneManager.LoadScene(name);
 
    }
 
    /* ─────────── decode JWT helper ─────────── */
    private Dictionary<string, object> DecodeJwt(string jwt)
    {
        string payload = jwt.Split('.')[1]
                            .Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }

        string json = Encoding.UTF8.GetString(
                          Convert.FromBase64String(payload));

        return MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
    }
}

/*
    --- Key Concepts ---

    JWT (JSON Web Token): A secure token containing user identity and claims (like 'role' or 'campusId') from Firebase.
    Claim: A field in the JWT specifying user permissions or status.
    Singleton Pattern (SessionData): Ensures CampusId, AdminId, and IdToken are globally available for all scenes.
        - Critical for enabling other screens to request and display the correct campus/admin info.
    WCT: Stands for 'With Custom Token' (here, the JWT fetched after login that includes user claims).

    --- Logic Summary ---
    - Handles authentication, error messages, claim extraction, role-based navigation, and session management.
    - Uses SessionData (Singleton) so all subsequent scenes and network requests have access to the correct context.
*/