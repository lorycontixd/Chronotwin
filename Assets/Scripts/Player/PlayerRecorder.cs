using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class PlayerInputEvent
{
    public float timestamp;
    public string inputType;
    public bool isPressed;

    public PlayerInputEvent(float timestamp, string inputType, bool isPressed)
    {
        this.timestamp = timestamp;
        this.inputType = inputType;
        this.isPressed = isPressed;
    }

    public override string ToString()
    {
        return $"[{timestamp:F2}] {inputType} {(isPressed ? "Pressed" : "Released")}";
    }
}

[Serializable]
public class PlayerKeyframe
{
    public float timestamp;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public float angularVelocity;
    public string animationState;
    public int facingDirection;
    public Dictionary<string, object> customData;

    public PlayerKeyframe(float timestamp, Vector3 position, Quaternion rotation, Vector3 velocity, float angularVelocity, string animationState, int facingDirection, Dictionary<string, object> customData = null)
    {
        this.timestamp = timestamp;
        this.position = position;
        this.rotation = rotation;
        this.velocity = velocity;
        this.angularVelocity = angularVelocity;
        this.animationState = animationState;
        this.facingDirection = facingDirection;
        this.customData = customData != null ? customData : new Dictionary<string, object>();
    }

    public void ApplyToGameobject(GameObject gameObject, bool applyPhysics = true)
    {
        // Apply the keyframe data to the game object
        gameObject.transform.position = position;
        //gameObject.transform.rotation = rotation; // implement for 2d simple rotation

        // Assuming the GameObject has a Rigidbody component
        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;
        }

        // Handle animation state and custom data as needed
        // e.g. gameObject.GetComponent<Animator>().Play(animationState);
    }
}

/// <summary>
/// Container for a complete ghost recording
/// </summary>
[Serializable]
public class PlayerRecording 
{
    public string recordingId;
    public float startTime;
    public float duration;
    public List<PlayerKeyframe> keyframes;
    public List<PlayerInputEvent> inputEvents = new List<PlayerInputEvent>();


    public PlayerRecording(float startTime)
    {
        keyframes = new List<PlayerKeyframe>();
        inputEvents = new List<PlayerInputEvent>();
        recordingId = Guid.NewGuid().ToString();
    }

    public PlayerRecording(string recordingId, float startTime, float duration, List<PlayerKeyframe> keyframes, List<PlayerInputEvent> inputEvents)
    {
        this.recordingId = recordingId;
        this.startTime = startTime;
        this.duration = duration;
        this.keyframes = keyframes;
        this.inputEvents = inputEvents;
    }

    public PlayerKeyframe GetKeyframeAtTime(float time)
    {
        if (keyframes.Count == 0)
            return null;

        // If time is before first keyframe, return first keyframe
        if (time <= keyframes[0].timestamp)
            return keyframes[0];

        // If time is after last keyframe, return last keyframe
        if (time >= keyframes[keyframes.Count - 1].timestamp)
            return keyframes[keyframes.Count - 1];

        // Binary search for nearest keyframe
        int low = 0;
        int high = keyframes.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;

            if (keyframes[mid].timestamp == time)
                return keyframes[mid];

            if (keyframes[mid].timestamp < time && (mid == keyframes.Count - 1 || keyframes[mid + 1].timestamp > time))
                return keyframes[mid];

            if (keyframes[mid].timestamp < time)
                low = mid + 1;
            else
                high = mid - 1;
        }

        return keyframes[low > 0 ? low - 1 : 0];
    }

    public List<PlayerInputEvent> GetInputEventsInTimeRange(float startTime, float endTime)
    {
        List<PlayerInputEvent> events = new List<PlayerInputEvent>();

        foreach (var inputEvent in inputEvents)
        {
            if (inputEvent.timestamp >= startTime && inputEvent.timestamp <= endTime)
            {
                events.Add(inputEvent);
            }
        }

        return events;
    }

    #region Save/Load Functionality
    // These methods would implement saving/loading functionality
    // Could use Unity's JsonUtility, PlayerPrefs, or a custom serialization approach

    public void Save()
    {
        // Example implementation using PlayerPrefs
        string json = JsonUtility.ToJson(this);
        PlayerPrefs.SetString("GhostRecording_" + recordingId, json);
        PlayerPrefs.Save();
    }

    public static PlayerRecording Load(string recordingId)
    {
        if (PlayerPrefs.HasKey("GhostRecording_" + recordingId))
        {
            string json = PlayerPrefs.GetString("GhostRecording_" + recordingId);
            return JsonUtility.FromJson<PlayerRecording>(json);
        }

        return null;
    }
    #endregion
}



public class PlayerRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    [SerializeField] private float maxRecordingTime = 10f;
    [SerializeField] private float keyframeInterval = 0.5f;

    [Header("References")]
    [SerializeField] private Rigidbody2D playerRigidbody;
    [SerializeField] private Animator playerAnimator;

    // State tracking
    private PlayerRecording currentRecording;
    private bool isRecording = false;
    private float recordingElapsedTime = 0f;
    private float lastKeyframeTime = 0f;
    private int playerFacingDirection = 1;
    private string currentAnimationState = "Idle";

    // Events
    public UnityEvent OnRecordingStarted;
    public UnityEvent<PlayerRecording> OnRecordingStopped;
    public UnityEvent OnKeyframeCapture;

    // Properties
    public bool IsRecording => isRecording;
    public float RecordingElapsedTime => recordingElapsedTime;
    public float MaxRecordingTime => maxRecordingTime;
    public float KeyframeInterval => keyframeInterval;
    public PlayerRecording CurrentRecording => currentRecording;

    #region Unity Methods
    private void Awake()
    {
        // Initialize references if not set in inspector
        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody2D>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();
    }
    private void Start()
    {
        StartRecording();
    }
    private void Update()
    {
        if (!isRecording)
            return;

        // Update recording time
        recordingElapsedTime += Time.deltaTime;

        // Check if we should take a keyframe
         if (recordingElapsedTime - lastKeyframeTime >= keyframeInterval)
        {
            CaptureKeyframe();
            lastKeyframeTime = recordingElapsedTime;
        }

        // Check if we've reached max recording time
        if (recordingElapsedTime >= maxRecordingTime)
        {
            StopRecording();
        }

        // Here you would handle animation state detection
        // This is just a placeholder, can be implemented
        UpdateAnimationState();

    }
    #endregion

    public void StartRecording()
    {
        if (isRecording)
            return;

        currentRecording = new PlayerRecording(Time.time);
        isRecording = true;
        recordingElapsedTime = 0f;
        lastKeyframeTime = -keyframeInterval; // Ensure we capture an initial keyframe

        // Capture the first keyframe immediately
        CaptureKeyframe();

        OnRecordingStarted?.Invoke();

        Debug.Log("Ghost recording started");
    }
    public PlayerRecording StopRecording()
    {
        if (!isRecording)
            return null;

        // Capture final keyframe
        CaptureKeyframe();

        isRecording = false;
        currentRecording.duration = recordingElapsedTime;

        OnRecordingStopped?.Invoke(CurrentRecording);

        Debug.Log($"Ghost recording stopped. Duration: {currentRecording.duration:F2}s, " +
                  $"Keyframes: {currentRecording.keyframes.Count}, " +
                  $"Inputs: {currentRecording.inputEvents.Count}");

        return currentRecording;
    }
    public void CaptureKeyframe()
    {
        if (!isRecording)
            return;

        var keyframe = new PlayerKeyframe(
            recordingElapsedTime,
            transform.position,
            transform.rotation,
            playerRigidbody.velocity,
            playerRigidbody.angularVelocity,
            currentAnimationState,
            playerFacingDirection
        );

        currentRecording.keyframes.Add(keyframe);

        OnKeyframeCapture?.Invoke();
    }
    public void UpdateAnimationState()
    {

    }

    public void RecordInput(string inputType, bool pressed)
    {
        if (!isRecording)
            return;

        var inputEvent = new PlayerInputEvent(recordingElapsedTime, inputType, pressed);
        currentRecording.inputEvents.Add(inputEvent);

    }
}
