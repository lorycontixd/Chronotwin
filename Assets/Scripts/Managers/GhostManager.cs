using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostManager : MonoBehaviour
{
    #region Singleton
    private static GhostManager _instance;
    public static GhostManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }
    #endregion

    [Header("References")]
    [SerializeField] private PlayerRecorder playerRecorder;
    [SerializeField] private GameObject ghostPrefab;

    [Header("Settings")]
    [SerializeField] private int maxActiveGhosts = 3;
    [SerializeField] private string[] recordingSlots = new string[] { "Slot1", "Slot2", "Slot3" };

    // Collection of ghost recordings
    private Dictionary<string, PlayerRecording> savedRecordings = new Dictionary<string, PlayerRecording>();
    private List<GhostPlayer> activeGhosts = new List<GhostPlayer>();

    // Events
    public event Action<GhostPlayer> OnGhostCreated;
    public event Action<GhostPlayer> OnGhostDestroyed;
    public event Action<PlayerRecording> OnRecordingSaved;

    // Properties
    public int ActiveGhostCount => activeGhosts.Count;
    public int MaxActiveGhosts => maxActiveGhosts;
    public string[] RecordingSlots => recordingSlots;


    #region Unity Methods
    private void Start()
    {
        if (playerRecorder == null)
        {
            Debug.LogError("Ghost Manager requires a GhostRecorder reference!");
        }


        // Load any saved recordings
        //LoadAllSavedRecordings();

        // Subscribe to recorder events
        if (playerRecorder != null)
        {
            playerRecorder.OnRecordingStopped.AddListener(OnPlayerRecordingStopped);
        }
    }

    #endregion

    private void OnPlayerRecordingStopped(PlayerRecording recording)
    {
        if (playerRecorder != null && playerRecorder.CurrentRecording != null)
        {
            // Example: Auto-save to the first slot
            if (recordingSlots.Length > 0)
            {
                SaveRecording(recording, recordingSlots[0]);
            }
            CreateGhost(recording);
        }
    }
    private void OnGhostReplayCompleted(GhostPlayer ghost)
    {
        DestroyGhost(ghost);
    }

    public GhostPlayer CreateGhost(PlayerRecording recording)
    {
        if (recording == null)
        {
            Debug.LogError("Cannot create ghost: recording is null");
            return null;
        }
        if (activeGhosts.Count >= maxActiveGhosts)
        {
            Debug.LogWarning($"Cannot create ghost: maximum of {maxActiveGhosts} ghosts already active");
            return null;
        }
        // Instantiate the ghost
        GameObject ghostObject = Instantiate(ghostPrefab);
        ghostObject.name = $"Ghost_{recording.recordingId}";

        // Get or add ghost component
        GhostPlayer ghostPlayer = ghostObject.GetComponent<GhostPlayer>();
        if (ghostPlayer == null)
        {
            ghostPlayer = ghostObject.AddComponent<GhostPlayer>();
        }

        // Add to active ghosts
        activeGhosts.Add(ghostPlayer);

        // Start replay
        ghostPlayer.StartReplay(recording);

        // Subscribe to replay completed event
        ghostPlayer.OnReplayCompleted.AddListener(() => OnGhostReplayCompleted(ghostPlayer));

        OnGhostCreated?.Invoke(ghostPlayer);

        Debug.Log($"Ghost created: {ghostObject.name} ({recording.recordingId})");

        return ghostPlayer;
    }
    public GhostPlayer CreateGhostFromSlot(string slotName)
    {
        if (savedRecordings.TryGetValue(slotName, out PlayerRecording recording))
        {
            return CreateGhost(recording);
        }

        Debug.LogWarning($"Cannot create ghost: no recording found in slot '{slotName}'");
        return null;
    }
    public void DestroyGhost(GhostPlayer ghost)
    {
        if (ghost == null || !activeGhosts.Contains(ghost))
            return;

        // Stop replay
        ghost.StopReplay();

        // Remove from active ghosts
        activeGhosts.Remove(ghost);

        // Destroy game object
        Destroy(ghost.gameObject);

        OnGhostDestroyed?.Invoke(ghost);
    }
    public void StartAllGhosts()
    {
        foreach (var ghost in activeGhosts)
        {
            if (!ghost.IsPlaying)
            {
                ghost.StartReplay(ghost.CurrentRecording);
            }
        }
    }
    public void StopAllGhosts()
    {
        foreach (var ghost in activeGhosts)
        {
            ghost.StopReplay();
        }
    }
    public void SaveRecording(PlayerRecording recording, string slotName)
    {
        if (recording == null)
            return;

        // Save to collection
        savedRecordings[slotName] = recording;

        // Save to persistent storage
        string key = "GhostRecording_" + slotName;
        string json = JsonUtility.ToJson(recording);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();

        OnRecordingSaved?.Invoke(recording);

        Debug.Log($"Saved recording to slot '{slotName}'");
    }
    public PlayerRecording LoadRecording(string slotName)
    {
        // Check if already loaded
        if (savedRecordings.TryGetValue(slotName, out PlayerRecording recording))
        {
            return recording;
        }

        // Try to load from persistent storage
        string key = "GhostRecording_" + slotName;
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            recording = JsonUtility.FromJson<PlayerRecording>(json);
            savedRecordings[slotName] = recording;

            Debug.Log($"Loaded recording from slot '{slotName}'");
            return recording;
        }

        Debug.LogWarning($"No recording found in slot '{slotName}'");
        return null;
    }
    private void LoadAllSavedRecordings()
    {
        foreach (string slotName in recordingSlots)
        {
            LoadRecording(slotName);
        }
    }
    public void ClearRecordingSlot(string slotName)
    {
        string key = "GhostRecording_" + slotName;

        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        if (savedRecordings.ContainsKey(slotName))
        {
            savedRecordings.Remove(slotName);
        }

        Debug.Log($"Cleared recording slot '{slotName}'");
    }
    private void OnPlayerRecordingStopped()
    {
        if (playerRecorder != null && playerRecorder.CurrentRecording != null)
        {
            // You could auto-save to a designated "last recording" slot,
            // or prompt the player to choose a slot
            PlayerRecording recording = playerRecorder.CurrentRecording;

            // Example: Auto-save to the first slot
            if (recordingSlots.Length > 0)
            {
                SaveRecording(recording, recordingSlots[0]);
            }
        }
    }
    public List<GhostPlayer> GetActiveGhosts()
    {
        return new List<GhostPlayer>(activeGhosts);
    }
    public List<string> GetSavedRecordingSlots()
    {
        List<string> slots = new List<string>();

        foreach (string slotName in recordingSlots)
        {
            if (savedRecordings.ContainsKey(slotName) || PlayerPrefs.HasKey("GhostRecording_" + slotName))
            {
                slots.Add(slotName);
            }
        }

        return slots;
    }
}
