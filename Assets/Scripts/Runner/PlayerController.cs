using System;
using System.Collections.Generic;
using extOSC;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Lane Settings")]
    public float laneWidth = 2.0f;
    public float laneSwitchSpeed = 15f;
    private int currentLane = 0; // -1 = Left, 0 = Center, 1 = Right

    [Header("Jump Settings")]
    public float jumpHeight = 2.5f;
    public float jumpDuration = 0.6f;
    private bool isJumping = false;
    private float jumpTimer = 0f;

    [Header("Slide Settings")]
    public float slideDuration = 1.0f;
    public float slideHeightMultiplier = 0.5f;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private CapsuleCollider capsuleCollider;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    [Header("Rotation Settings")]
    public float maxRotationAngle = 15f;
    public float rotationSpeed = 10f;

    [Header("Ground Level")]
    public float groundY = 0.5f;

    [Header("Animation")]
    [Tooltip("Animator with the Animator Controller. Leave empty to auto-find on this object or its children.")]
    [SerializeField] private Animator animator;

    [Header("Input System Actions")]
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction slideAction;
    private bool laneSwitchPressed = false;

    [Header("OSC Input")]
    [SerializeField] private int oscLocalPort = 7001;
    // Each controller idles at a default address/value and fires the bound
    // address below when it flips away from that default.
    [SerializeField] private string oscMoveLeftAddress = "/control/left";   // idle: /control/left/0
    [SerializeField] private string oscMoveRightAddress = "/control/right"; // idle: /control/right/0
    [SerializeField] private string oscJumpAddress = "/control/up";         // idle: /control/up/0
    [SerializeField] private string oscSlideAddress = "/control/down";      // idle: /control/down/1
    private readonly Dictionary<string, bool> oscAddressActive = new Dictionary<string, bool>();

    private void Start()
    {
        // Tag the player to ensure other scripts detect collisions correctly
        gameObject.tag = "Player";

        // Attach (or reuse) the OSC receiver and bind mock commands to player actions.
        var oscReceiver = gameObject.AddComponent<OSCReceiver>();
        if (oscReceiver != null)
        {
            oscReceiver.LocalPort = oscLocalPort;
            oscReceiver.Close();
            oscReceiver.Connect();
            oscReceiver.Bind("/control/left", message => OnOSCControlMessage(message, MoveLeft));
            oscReceiver.Bind("/control/right", message => OnOSCControlMessage(message, MoveRight));
            oscReceiver.Bind("/control/jump", message => OnOSCControlMessage(message, Jump));
            oscReceiver.Bind("/control/duck", message => OnOSCControlMessage(message, Slide));
        }



        // Cache the Animator if not assigned in the Inspector
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        // Auto-detect ground Y from initial position
        groundY = transform.position.y;

        // Cache the CapsuleCollider
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            originalColliderHeight = capsuleCollider.height;
            originalColliderCenter = capsuleCollider.center;
        }

        // Bind new Input System actions
        var playerMap = InputSystem.actions?.FindActionMap("Player");
        if (playerMap != null)
        {
            playerMap.Enable();
            moveAction = playerMap.FindAction("Move");
            jumpAction = playerMap.FindAction("Jump");
            slideAction = playerMap.FindAction("Crouch");
        }
        else
        {
            // Fallback: search globally if maps aren't loaded correctly
            moveAction = InputSystem.actions?.FindAction("Move");
            jumpAction = InputSystem.actions?.FindAction("Jump");
            slideAction = InputSystem.actions?.FindAction("Crouch");
        }
    }

    private void Update()
    {
        // Don't execute controls if game is over or not playing
        if (RunnerGameManager.Instance != null && (!RunnerGameManager.Instance.isPlaying || RunnerGameManager.Instance.isGameOver))
        {
            return;
        }

        HandleInput();
        HandleMovement();
        HandleSliding();
    }
    private void MessageReceived(OSCMessage message)
    {
        Debug.Log("made it here!");
        if (message.Values.Count == 0)
        {
            Debug.Log($"PlayerController: Received OSC message at address {message.Address} with no value");
            return;
        }
        Debug.Log($"PlayerController: Received OSC message at address {message.Address} with value {message.Values[0]}");
    }

    /// <summary>Dispatches a bound OSC control message to its action, firing only on the
    /// rising edge (idle -> active) so a held button doesn't re-trigger every tick.</summary>
    private void OnOSCControlMessage(OSCMessage message, Action onActivated)
    {
        // switch the active value for down control:

        bool isActive = message.Values.Count > 0 && message.Values[0].FloatValue > 0.5f;
        if (message.Address == oscSlideAddress)
        { // slide is active when the value is low (0) and idle when high (1)
            isActive = message.Values.Count > 0 && message.Values[0].FloatValue < 0.5f;
        }
        oscAddressActive.TryGetValue(message.Address, out bool wasActive);
        oscAddressActive[message.Address] = isActive;

        if (isActive && !wasActive)
        {
            onActivated();
        }
    }
    private void HandleInput()
    {


        // 1. Lane switching discrete check
        if (moveAction != null)
        {
            float moveX = moveAction.ReadValue<Vector2>().x;
            if (Mathf.Abs(moveX) > 0.4f)
            {
                if (!laneSwitchPressed)
                {
                    if (moveX < -0.4f)
                    {
                        MoveLeft();
                    }
                    else if (moveX > 0.4f)
                    {
                        MoveRight();
                    }
                    laneSwitchPressed = true;
                }
            }
            else
            {
                laneSwitchPressed = false;
            }
        }

        // 2. Jump trigger`
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            Jump();
        }

        // 3. Slide trigger (C key)
        if (slideAction != null && slideAction.WasPressedThisFrame())
        {
            Slide();
        }
    }

    private void HandleSliding()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                Debug.Log("PlayerController: Slide duration ended, stopping slide.");
                StopSliding();
            }
        }
    }

    private void StopSliding()
    {
        isSliding = false;
        if (capsuleCollider != null)
        {
            capsuleCollider.height = originalColliderHeight;
            capsuleCollider.center = originalColliderCenter;
        }
    }

    // -------------------------------------------------------------------
    // Public API
    // These methods expose the same actions the Input System triggers, so
    // UI buttons, touch controls, or other scripts can drive the player.
    // They respect the game state guard, just like the Input System path.
    // -------------------------------------------------------------------

    /// <summary>Move the player one lane to the left.</summary>
    public void MoveLeft()
    {
        if (!CanControl()) return;
        SwitchLane(-1);
    }

    /// <summary>Move the player one lane to the right.</summary>
    public void MoveRight()
    {
        if (!CanControl()) return;
        SwitchLane(1);
    }

    /// <summary>Trigger a jump if the player is grounded.</summary>
    public void Jump()
    {
        if (!CanControl()) return;
        if (!isJumping)
        {
            if (isSliding)
            {
                StopSliding();
            }
            isJumping = true;
            jumpTimer = 0f;

            if (RunnerGameManager.Instance != null && RunnerGameManager.Instance.isTutorial)
            {
                RunnerGameManager.Instance.RegisterTutorialAction(RunnerGameManager.TutorialAction.Jump);
            }
        }
    }

    /// <summary>Fire the "sliding" trigger on the Animator attached to this GameObject.
    /// Called when the C key is pressed, and exposed for UI buttons, touch controls,
    /// or other scripts.</summary>
    public void Slide()
    {
        if (!CanControl()) return;

        // If jumping, cancel jump and slide (runner dive mechanic)
        if (isJumping)
        {
            isJumping = false;
        }

        if (isSliding)
        {
            return;
            //this is to allow instant sliding
        }

        isSliding = true;
        slideTimer = slideDuration;

        if (capsuleCollider != null)
        {
            capsuleCollider.height = originalColliderHeight * slideHeightMultiplier;
            capsuleCollider.center = new Vector3(
                originalColliderCenter.x,
                originalColliderCenter.y - (originalColliderHeight - capsuleCollider.height) / 2f,
                originalColliderCenter.z
            );
        }

        FireTrigger("Sliding");

        if (RunnerGameManager.Instance != null && RunnerGameManager.Instance.isTutorial)
        {
            RunnerGameManager.Instance.RegisterTutorialAction(RunnerGameManager.TutorialAction.Slide);
        }
    }

    /// <summary>Fire the default trigger (set via the Inspector) on the Animator Controller.</summary>
    /// <summary>Fire a specific trigger by name on the Animator Controller.</summary>
    public void FireTrigger(string trigger)
    {
        if (animator == null)
        {
            Debug.LogWarning("PlayerController: no Animator assigned or found.");
            return;
        }
        if (string.IsNullOrEmpty(trigger))
        {
            Debug.LogWarning("PlayerController: trigger name is empty.");
            return;
        }
        animator.SetTrigger(trigger);
    }

    /// <summary>Resets the player controller's state and position to default for game start.</summary>
    public void ResetPlayer()
    {
        currentLane = 0;
        isJumping = false;
        jumpTimer = 0f;
        isSliding = false;
        slideTimer = 0f;

        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
        }
        if (capsuleCollider != null)
        {
            if (originalColliderHeight == 0f)
            {
                originalColliderHeight = capsuleCollider.height;
                originalColliderCenter = capsuleCollider.center;
            }
            capsuleCollider.height = originalColliderHeight;
            capsuleCollider.center = originalColliderCenter;
        }

        transform.position = new Vector3(0f, groundY, -6f);
        transform.rotation = Quaternion.identity;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    /// <summary>True when the player is allowed to respond to controls.</summary>
    private bool CanControl()
    {
        if (RunnerGameManager.Instance != null &&
            (!RunnerGameManager.Instance.isPlaying || RunnerGameManager.Instance.isGameOver))
        {
            return false;
        }
        return true;
    }

    private void SwitchLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, -1, 1);
        if (RunnerGameManager.Instance != null && RunnerGameManager.Instance.isTutorial)
        {
            RunnerGameManager.Instance.RegisterTutorialAction(RunnerGameManager.TutorialAction.LaneSwitch);
        }
    }

    private void HandleMovement()
    {
        // Interpolate horizontal position
        float targetX = currentLane * laneWidth;
        float currentY = groundY;

        // Interpolate Jump vertical position
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float normalizedTime = jumpTimer / jumpDuration;
            if (normalizedTime >= 1.0f)
            {
                isJumping = false;
                currentY = groundY;
            }
            else
            {
                currentY = groundY + Mathf.Sin(normalizedTime * Mathf.PI) * jumpHeight;
            }
        }

        // Smoothly update horizontal position, lock forward/backward Z position at Z = -6f
        Vector3 currentPos = transform.position;
        float newX = Mathf.MoveTowards(currentPos.x, targetX, laneSwitchSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, currentY, -6f);

        // Smoothly rotate body based on horizontal movement direction
        float targetYRotation = 0f;
        float diffX = newX - currentPos.x;
        if (Mathf.Abs(diffX) > 0.001f)
        {
            targetYRotation = Mathf.Sign(diffX) * maxRotationAngle;
        }

        Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
