[System.Serializable]
/*
 * AdminDataLocal.cs
 * -----------------------------------------------------------------------------
 * Plain-old data container (serializable) that represents an administrator
 * record fetched from Firestore. Used by UI managers and list items to display
 * admin details and perform status updates.
 */
public class AdminDataLocal
{
    public string docId;
    public string adminName;
    public string email;
    public string role;

    public string campusId;
    public string adminPhotoURL;
    public string status;
    public string employeeApprovalFileURL;
}
