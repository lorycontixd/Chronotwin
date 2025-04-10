using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class GhostPlayer : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Rigidbody2D ghostRigidbody;
    [SerializeField] private Animator ghostAnimator;
    [SerializeField] private SpriteRenderer ghostRenderer;

    [Header("Collision Settings")]
    [SerializeField] private bool canCollideWithPlayer = false;
    [SerializeField] private string playerTag = "Player";

    // State tracking
    private PlayerRecording recording; // The recording to replay
    private bool isPlaying = false; // Whether the ghost is currently playing back a recording
    private float playbackTime = 0f; // Current playback time
    private int currentKeyframeIndex = 0;
    private PlayerKeyframe currentKeyframe;
    private PlayerKeyframe nextKeyframe;

    // Player collision
    private List<Collider2D> playerColliders = new List<Collider2D>();
    private List<Collider2D> ghostColliders = new List<Collider2D>();

    // Events
    public UnityEvent OnReplayStarted;
    public UnityEvent OnReplayCompleted;
    public UnityEvent<string> OnInputProcessed;

    // Properties
    public bool IsPlaying => isPlaying;
    public float PlaybackTime => playbackTime;
    public PlayerRecording CurrentRecording => recording;


    #region Unity Methods
    private void Awake()
    {
        if (ghostRigidbody == null)
            ghostRigidbody = GetComponent<Rigidbody2D>();
        if (ghostAnimator == null)
            ghostAnimator = GetComponent<Animator>();
        if (ghostRenderer == null)
            ghostRenderer = GetComponent<SpriteRenderer>();
    }
    private void Update()
    {
        //Debug.Log($"Ghost Player Update: {this.gameObject.name} - IsPlaying: {isPlaying}, PlaybackTime: {playbackTime:F2}s, KeyframeIndex: {currentKeyframeIndex}, rec: {this.recording == null}");
        if (!isPlaying || this.recording == null)
            return;

        // Update playback time
        playbackTime += Time.deltaTime;

        // Check if we need to process a new keyframe
        if (playbackTime >= recording.duration)
        {
            StopReplay();
            return;
        }

        // Update keyframes if needed
        UpdateKeyframes();

        // Interpolate between keyframes
        InterpolateKeyframes();
    }
    public void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Entered with: {collision.gameObject.name}");
    }
    #endregion

    public void StartReplay(PlayerRecording recording)
    {
        // Cannot start replay if already playing or recording is null
        if (isPlaying || recording == null)
            return;
        this.recording = recording;
        isPlaying = true;
        playbackTime = 0f;
        currentKeyframeIndex = 0;

        // Initialize keyframes
        UpdateKeyframes();

        // Apply initial position/rotation
        if (currentKeyframe != null)
        {
            transform.position = currentKeyframe.position;
            transform.rotation = Quaternion.Euler(0, 0, currentKeyframe.rotation.z);

            // Zero out velocity
            if (ghostRigidbody != null)
            {
                ghostRigidbody.velocity = Vector2.zero;
                ghostRigidbody.angularVelocity = 0f;
                ghostRigidbody.isKinematic = true; // Use kinematic for precise control
            }

            // Set initial animation
            UpdateAnimation(currentKeyframe);
        }

        OnReplayStarted?.Invoke();

        Debug.Log($"Ghost replay started (keyframe-only). Recording ID: {recording.recordingId}, Duration: {recording.duration:F2}s");

    }
    public void StopReplay()
    {
        if (!isPlaying)
            return;

        isPlaying = false;

        // Reset any ongoing effects
        if (ghostRigidbody != null)
        {
            ghostRigidbody.velocity = Vector2.zero;
        }

        OnReplayCompleted?.Invoke();

        Debug.Log("Ghost replay stopped");
    }
    public void PauseReplay()
    {
        if (!isPlaying)
            return;

        isPlaying = false;

        // Optionally pause physics or animations
        if (ghostRigidbody != null)
        {
            ghostRigidbody.velocity = Vector2.zero;
        }

        Debug.Log("Ghost replay paused");
    }
    public void ResumeReplay()
    {
        if (isPlaying || recording == null)
            return;

        isPlaying = true;

        Debug.Log("Ghost replay resumed");
    }
    /// <summary>
    /// Update current and next keyframes based on playback time
    /// </summary>
    private void UpdateKeyframes()
    {
        if (recording == null || recording.keyframes.Count < 2)
            return;

        // Find the appropriate keyframes for current playback time
        for (int i = 0; i < recording.keyframes.Count - 1; i++)
        {
            if (playbackTime >= recording.keyframes[i].timestamp &&
                playbackTime < recording.keyframes[i + 1].timestamp)
            {
                // Only update if keyframes have changed
                if (currentKeyframeIndex != i)
                {
                    currentKeyframeIndex = i;
                    currentKeyframe = recording.keyframes[i];
                    nextKeyframe = recording.keyframes[i + 1];

                    Debug.Log($"Ghost using keyframes at {currentKeyframe.timestamp:F2}s and {nextKeyframe.timestamp:F2}s");
                }
                return;
            }
        }

        // If we've passed the last keyframe pair, use the last two keyframes
        if (currentKeyframeIndex != recording.keyframes.Count - 2)
        {
            currentKeyframeIndex = recording.keyframes.Count - 2;
            currentKeyframe = recording.keyframes[currentKeyframeIndex];
            nextKeyframe = recording.keyframes[currentKeyframeIndex + 1];
        }
    }
    /// <summary>
    /// Interpolate between current and next keyframe
    /// </summary>
    private void InterpolateKeyframes()
    {
        if (currentKeyframe == null || nextKeyframe == null)
            return;

        // Calculate interpolation factor (0 to 1)
        float t = Mathf.InverseLerp(
            currentKeyframe.timestamp,
            nextKeyframe.timestamp,
            playbackTime
        );

        // Interpolate position
        transform.position = Vector2.Lerp(
            currentKeyframe.position,
            nextKeyframe.position,
            t
        );

        // Interpolate rotation
        float rotationDiff = Mathf.DeltaAngle(currentKeyframe.rotation.z, nextKeyframe.rotation.z);
        float interpolatedRotation = currentKeyframe.rotation.z + rotationDiff * t;
        transform.rotation = Quaternion.Euler(0, 0, interpolatedRotation);

        // Interpolate facing direction for sprite flip
        //UpdateFacingDirection(t);

        // Update animation based on keyframes
        UpdateAnimation(t < 0.5f ? currentKeyframe : nextKeyframe);
    }

    /// <summary>
    /// Update animation based on keyframe
    /// </summary>
    private void UpdateAnimation(PlayerKeyframe keyframe)
    {
        if (ghostAnimator == null || keyframe == null)
            return;

        // Set animation state
        if (!string.IsNullOrEmpty(keyframe.animationState))
        {
            ghostAnimator.Play(keyframe.animationState);
        }
    }

    private PlayerKeyframe FindNearestKeyframe(float time)
    {
        return null;
    }
    private void ApplyKeyframeCorrection()
    {

    }
    private void UpdateAnimationState()
    {

    }
}
