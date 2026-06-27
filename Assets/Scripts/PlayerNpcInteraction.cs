using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerNpcInteraction : MonoBehaviour
{
    [SerializeField] float interactRange = 3f;

    PlayerMovement movement;
    InputAction interactAction;
    NpcChat nearbyNpc;
    NpcChat activeNpc;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        interactAction = ResolveInteractAction();
    }

    void OnEnable()
    {
        interactAction?.Enable();
    }

    void OnDisable()
    {
        interactAction?.Disable();
    }

    void Update()
    {
        if (activeNpc != null && activeNpc.IsChatOpen)
        {
            if (WasCancelPressed())
                CloseChat();

            return;
        }

        nearbyNpc = FindNearestNpc();

        if (nearbyNpc != null && WasInteractPressed())
            OpenChat(nearbyNpc);
    }

    void OnGUI()
    {
        var promptY = Screen.height - 40f;

        if (activeNpc != null && activeNpc.IsChatOpen)
        {
            promptY = Screen.height - 360f;
            GUI.Label(new Rect(20, promptY, Screen.width - 40, 30), "Press E (outside the text field) or Esc to close chat.");
            return;
        }

        if (nearbyNpc != null)
            GUI.Label(new Rect(20, promptY, Screen.width - 40, 30), "Press E to talk to " + nearbyNpc.DisplayName + ".");
    }

    InputAction ResolveInteractAction()
    {
        var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        foreach (var asset in assets)
        {
            if (asset.name != "InputSystem_Actions")
                continue;

            return asset.FindActionMap("Player").FindAction("Interact");
        }

        return null;
    }

    bool WasInteractPressed()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            return true;

        if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)
            return true;

        return interactAction != null && interactAction.WasPressedThisFrame();
    }

    bool WasCancelPressed()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    NpcChat FindNearestNpc()
    {
        var npcs = FindObjectsByType<NpcChat>(FindObjectsSortMode.None);
        NpcChat closest = null;
        var closestDistance = interactRange;

        foreach (var npc in npcs)
        {
            var distance = Vector3.Distance(transform.position, npc.transform.position);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closest = npc;
            }
        }

        return closest;
    }

    void OpenChat(NpcChat npc)
    {
        activeNpc = npc;
        activeNpc.CloseRequested += CloseChat;
        activeNpc.SetChatOpen(true);
        movement.SetMovementEnabled(false);
    }

    void CloseChat()
    {
        if (activeNpc == null)
            return;

        activeNpc.CloseRequested -= CloseChat;
        activeNpc.SetChatOpen(false);
        activeNpc = null;
        movement.SetMovementEnabled(true);
    }
}
