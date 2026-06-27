using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float gravity = -20f;

    CharacterController controller;
    InputAction moveAction;
    Vector3 velocity;
    bool movementEnabled = true;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        EnsureInputActions();

        if (inputActions == null)
            return;

        moveAction = inputActions.FindActionMap("Player").FindAction("Move");
    }

    void OnEnable()
    {
        moveAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
    }

    void Update()
    {
        if (!movementEnabled || moveAction == null)
            return;

        var moveInput = moveAction.ReadValue<Vector2>();
        var input = new Vector3(moveInput.x, 0f, moveInput.y);
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        controller.Move(input * moveSpeed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (input.sqrMagnitude > 0.01f)
        {
            var lookDirection = new Vector3(input.x, 0f, input.z);
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        if (!movementEnabled)
            velocity = Vector3.zero;
    }

    void EnsureInputActions()
    {
        if (inputActions != null)
            return;

        var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        foreach (var asset in assets)
        {
            if (asset.name == "InputSystem_Actions")
            {
                inputActions = asset;
                return;
            }
        }

        Debug.LogError("InputSystem_Actions asset not found. Assign it on PlayerMovement.");
    }
}
