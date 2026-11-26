using System;
using Lifetopia.Core;
using Lifetopia.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lifetopia.Controls
{
    /// <summary>
    /// Single source of player intent. Merges keyboard/gamepad actions with on-screen touch
    /// controls and exposes plain values/events, so movement and interaction code never
    /// touches devices directly.
    /// - Move: clamped to length 1 (no diagonal speed bonus); zero while gameplay is blocked.
    /// - InteractPerformed: E / gamepad South / touch button, only while gameplay is not blocked.
    /// - UiSubmitPerformed: the same buttons while a modal (dialogue) blocks gameplay.
    /// - CancelPerformed: Escape / gamepad Start.
    /// - InventoryTogglePerformed: B / I / gamepad North (raised even while a modal blocks gameplay;
    ///   the inventory UI decides what to do).
    /// - HotbarSlotPerformed(index 0-4): keys 1-5 / numpad 1-5.
    /// All held input is released when the app loses focus or the browser tab is hidden.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Header("Input actions (Assets/_Lifetopia/Input/LifetopiaControls)")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private InputActionReference cancelAction;
        [SerializeField] private InputActionReference inventoryAction;
        [Tooltip("Button action whose bindings are keys 1..N in slot order (extra bindings wrap, e.g. numpad).")]
        [SerializeField] private InputActionReference hotbarSlotAction;
        [SerializeField, Min(1)] private int hotbarSlotCount = 5;

        [Header("Touch controls (optional)")]
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private TouchActionButton interactButton;

        [Header("Modal blocking (optional)")]
        [SerializeField] private GameplayInputGate gate;

        [Tooltip("Ignore stick/joystick tilt smaller than this.")]
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;

        public event Action InteractPerformed;
        public event Action UiSubmitPerformed;
        public event Action CancelPerformed;
        public event Action InventoryTogglePerformed;
        public event Action<int> HotbarSlotPerformed;

        /// <summary>Clamped movement intent in the range |v| ≤ 1.</summary>
        public Vector2 Move { get; private set; }

        /// <summary>
        /// Same keys/stick/joystick as <see cref="Move"/> but NOT zeroed while a modal blocks gameplay,
        /// so open menus (market) can use WASD/arrows for navigation. Never moves the player.
        /// </summary>
        public Vector2 NavigateInput { get; private set; }

        /// <summary>Number of times held input was force-released (focus loss). Useful for tests.</summary>
        public int FocusResetCount { get; private set; }

        private bool IsBlocked => gate != null && gate.IsBlocked;

        private void OnEnable()
        {
            Enable(moveAction);
            Enable(interactAction);
            Enable(cancelAction);
            Enable(inventoryAction);
            Enable(hotbarSlotAction);
            if (hotbarSlotAction != null && hotbarSlotAction.action != null) hotbarSlotAction.action.performed += OnHotbarSlot;
            if (interactButton != null) interactButton.Pressed += OnTouchInteract;
            WebFocusBridge.FocusLost += ReleaseAllInput;
        }

        private void OnDisable()
        {
            Disable(moveAction);
            Disable(interactAction);
            Disable(cancelAction);
            Disable(inventoryAction);
            if (hotbarSlotAction != null && hotbarSlotAction.action != null) hotbarSlotAction.action.performed -= OnHotbarSlot;
            Disable(hotbarSlotAction);
            if (interactButton != null) interactButton.Pressed -= OnTouchInteract;
            WebFocusBridge.FocusLost -= ReleaseAllInput;
            Move = Vector2.zero;
            NavigateInput = Vector2.zero;
        }

        private void Update()
        {
            Vector2 raw = joystick != null && joystick.IsHeld
                ? joystick.Value
                : (moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero);

            NavigateInput = MovementMath.ClampInput(raw, deadZone);
            Move = IsBlocked ? Vector2.zero : NavigateInput;

            if (interactAction != null && interactAction.action.WasPressedThisFrame()) RaiseInteract();
            if (cancelAction != null && cancelAction.action.WasPressedThisFrame()) CancelPerformed?.Invoke();
            if (inventoryAction != null && inventoryAction.action.WasPressedThisFrame()) InventoryTogglePerformed?.Invoke();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) WebFocusBridge.NotifyFocusLost();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) WebFocusBridge.NotifyFocusLost();
        }

        /// <summary>Clears every held key, pointer and joystick so nothing keeps moving after focus returns.</summary>
        public void ReleaseAllInput()
        {
            FocusResetCount++;
            Move = Vector2.zero;
            NavigateInput = Vector2.zero;
            if (joystick != null) joystick.ResetInput();
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device.added) InputSystem.ResetDevice(device);
            }
        }

        private void OnTouchInteract() => RaiseInteract();

        private void OnHotbarSlot(InputAction.CallbackContext context)
        {
            int binding = context.action.GetBindingIndexForControl(context.control);
            if (binding < 0) return;
            HotbarSlotPerformed?.Invoke(binding % hotbarSlotCount);
        }

        private void RaiseInteract()
        {
            if (IsBlocked) UiSubmitPerformed?.Invoke();
            else InteractPerformed?.Invoke();
        }

        private static void Enable(InputActionReference reference)
        {
            if (reference != null && reference.action != null) reference.action.Enable();
        }

        private static void Disable(InputActionReference reference)
        {
            if (reference != null && reference.action != null) reference.action.Disable();
        }
    }
}
