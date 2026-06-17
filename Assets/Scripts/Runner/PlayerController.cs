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
    }

    private void Start()
    {
        // Tag the player to ensure other scripts detect collisions correctly
        gameObject.tag = "Player";

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
        float currentY = 0.5f; // Ground level Y offset (half player scale height)

        // Interpolate Jump vertical position
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float normalizedTime = jumpTimer / jumpDuration;
            if (normalizedTime >= 1.0f)
            {
                isJumping = false;
                currentY = 0.5f;
            }
            else
            {
                currentY = 0.5f + Mathf.Sin(normalizedTime * Mathf.PI) * jumpHeight;
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
            }
            else
            {
                currentY = 0.25f; // Half height center
            }
        }

        // Smoothly update horizontal position, lock forward/backward Z position at Z = -6f
        Vector3 currentPos = transform.position;
        float newX = Mathf.MoveTowards(currentPos.x, targetX, laneSwitchSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, currentY, -6f);
    }
}
