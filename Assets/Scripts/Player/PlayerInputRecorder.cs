using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputRecorder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerRecorder ghostRecorder;

    // Reference to the PlayerInput component if using the new Input System
    private PlayerInput playerInput;

    private void Awake()
    {
        // Get references
        playerInput = GetComponent<PlayerInput>();

        if (ghostRecorder == null)
        {
            ghostRecorder = GetComponent<PlayerRecorder>();
        }

        if (ghostRecorder == null)
        {
            Debug.LogError("InputRecorder requires a GhostRecorder component!");
        }
    }

    private void OnEnable()
    {
        // Subscribe to input events
        if (playerInput != null)
        {
            playerInput.onActionTriggered += OnInputActionTriggered;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from input events
        if (playerInput != null)
        {
            playerInput.onActionTriggered -= OnInputActionTriggered;
        }
    }

    /// <summary>
    /// Handle input actions from the new Input System
    /// </summary>
    private void OnInputActionTriggered(InputAction.CallbackContext context)
    {
        if (ghostRecorder == null || !ghostRecorder.IsRecording)
            return;

        string actionName = context.action.name;

        // Record button presses and releases
        if (context.performed)
        {
            // For button actions
            ghostRecorder.RecordInput(actionName, true);
        }
        else if (context.canceled)
        {
            // For button actions
            ghostRecorder.RecordInput(actionName, false);
        }
        // Value type inputs (like axis) would need special handling
    }

    // Alternative implementation for the old Input system
    // You can use this if you're not using the new Input System
    #region Legacy Input System

    private void Update()
    {
        // Only use this method if not using the new Input System
        if (playerInput != null)
            return;

        if (ghostRecorder == null || !ghostRecorder.IsRecording)
            return;

        // Check for key presses/releases
        // These are just examples, adapt to your input scheme

        // Jump
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ghostRecorder.RecordInput("Jump", true);
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            ghostRecorder.RecordInput("Jump", false);
        }

        // Move Left
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ghostRecorder.RecordInput("MoveLeft", true);
        }
        if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow))
        {
            ghostRecorder.RecordInput("MoveLeft", false);
        }

        // Move Right
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ghostRecorder.RecordInput("MoveRight", true);
        }
        if (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow))
        {
            ghostRecorder.RecordInput("MoveRight", false);
        }

        // Add more inputs as needed
    }

    #endregion
}
