using UnityEngine;

/// <summary>
/// Singleton for storing session data (CampusId, AdminId, IdToken) for the current admin.
/// Persists across scenes and ensures global access to session context.
/// </summary>
public class SessionData : MonoBehaviour
{
    public static SessionData Instance { get; private set; }

    public string CampusId { get; set; }
    public string IdToken { get; set; }
    public string AdminId  { get; set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}