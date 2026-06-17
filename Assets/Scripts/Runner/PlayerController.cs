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
    public float slideDuration = 0.7f;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private Vector3 originalScale;
    private BoxCollider boxCollider;
    private Vector3 originalColliderSize;
    private Vector3 originalColliderCenter;
    private CapsuleCollider capsuleCollider;
    private float originalCapsuleHeight;
    private Vector3 originalCapsuleCenter;

    [Header("Rotation Settings")]
    public float maxRotationAngle = 15f;
    public float rotationSpeed = 10f;

    [Header("Ground Level")]
    public float groundY = 0.5f;

    [Header("Input System Actions")]
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private bool laneSwitchPressed = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            originalColliderSize = boxCollider.size;
            originalColliderCenter = boxCollider.center;
        }
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            originalCapsuleHeight = capsuleCollider.height;
            originalCapsuleCenter = capsuleCollider.center;
        }
    }

    private void Start()
    {
        // Tag the player to ensure other scripts detect collisions correctly
        gameObject.tag = "Player";

        // Auto-detect ground Y from initial position
        groundY = transform.position.y;

        // Bind new Input System actions
        var playerMap = InputSystem.actions?.FindActionMap("Player");
        if (playerMap != null)
        {
            playerMap.Enable();
            moveAction = playerMap.FindAction("Move");
            jumpAction = playerMap.FindAction("Jump");
            crouchAction = playerMap.FindAction("Crouch");
        }
        else
        {
            // Fallback: search globally if maps aren't loaded correctly
            moveAction = InputSystem.actions?.FindAction("Move");
            jumpAction = InputSystem.actions?.FindAction("Jump");
            crouchAction = InputSystem.actions?.FindAction("Crouch");
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
                        SwitchLane(-1);
                    }
                    else if (moveX > 0.4f)
                    {
                        SwitchLane(1);
                    }
                    laneSwitchPressed = true;
                }
            }
            else
            {
                laneSwitchPressed = false;
            }
        }

        // 2. Jump trigger
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            if (!isJumping && !isSliding)
            {
                isJumping = true;
                jumpTimer = 0f;
            }
        }

        // 3. Slide/Crouch trigger
        if (crouchAction != null && crouchAction.WasPressedThisFrame())
        {
            if (!isSliding && !isJumping)
            {
                isSliding = true;
                slideTimer = 0f;
                // Visual shrink
                transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.5f, originalScale.z);
                if (boxCollider != null)
                {
                    boxCollider.size = new Vector3(originalColliderSize.x, originalColliderSize.y * 0.5f, originalColliderSize.z);
                    boxCollider.center = new Vector3(originalColliderCenter.x, originalColliderCenter.y * 0.5f, originalColliderCenter.z);
                }
                if (capsuleCollider != null)
                {
                    capsuleCollider.height = originalCapsuleHeight * 0.5f;
                    capsuleCollider.center = new Vector3(originalCapsuleCenter.x, originalCapsuleCenter.y * 0.5f, originalCapsuleCenter.z);
                }
            }
        }
    }

    private void SwitchLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, -1, 1);
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

        // Interpolate Slide/Crouch timer
        if (isSliding)
        {
            slideTimer += Time.deltaTime;
            if (slideTimer >= slideDuration)
            {
                isSliding = false;
                // Restore visual and collider scales
                transform.localScale = originalScale;
                if (boxCollider != null)
                {
                    boxCollider.size = originalColliderSize;
                    boxCollider.center = originalColliderCenter;
                }
                if (capsuleCollider != null)
                {
                    capsuleCollider.height = originalCapsuleHeight;
                    capsuleCollider.center = originalCapsuleCenter;
                }
            }
            else
            {
                // Self-detect feet vs center pivoted model to handle sliding currentY height correctly
                bool isFeetPivoted = false;
                if (capsuleCollider != null && capsuleCollider.center.y > 0.1f) isFeetPivoted = true;
                if (boxCollider != null && boxCollider.center.y > 0.1f) isFeetPivoted = true;

                if (isFeetPivoted)
                {
                    currentY = groundY;
                }
                else
                {
                    currentY = groundY - (originalScale.y * 0.5f); // Lower by half height for center-pivoted models
                }
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
