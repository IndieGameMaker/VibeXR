using UnityEngine;
using UnityEngine.InputSystem;

namespace VibeXR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FactoryFirstPersonController : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
        [SerializeField, Range(0.01f, 1f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Range(1f, 89f)] private float pitchLimit = 85f;
        [SerializeField] private float gravity = -20f;

        private CharacterController controller;
        private float verticalSpeed;
        private float pitch;
        private GUIStyle hintStyle;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (view == null)
            {
                var childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null) view = childCamera.transform;
            }

            if (view == null)
            {
                Debug.LogError("Assign a camera to the first person controller.", this);
                enabled = false;
                return;
            }

            pitch = Mathf.DeltaAngle(0f, view.localEulerAngles.x);
        }

        private void Update()
        {
            if (!controller.enabled) return;

            Vector2 movement = Vector2.zero;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            bool released = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            bool justLocked = false;

            if (!Application.isFocused || released)
            {
                ReleaseCursor();
            }
            else if (Cursor.lockState != CursorLockMode.Locked &&
                     mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                justLocked = true;
            }

            if (Application.isFocused && Cursor.lockState == CursorLockMode.Locked && !justLocked)
            {
                if (keyboard != null)
                {
                    movement.x = (keyboard.dKey.isPressed ? 1f : 0f) -
                                 (keyboard.aKey.isPressed ? 1f : 0f);
                    movement.y = (keyboard.wKey.isPressed ? 1f : 0f) -
                                 (keyboard.sKey.isPressed ? 1f : 0f);
                }
                if (mouse != null) ApplyLook(mouse.delta.ReadValue());
            }

            MoveCharacter(movement, Time.deltaTime);
        }

        private void ApplyLook(Vector2 mouseDelta)
        {
            // Mouse delta already represents motion accumulated during this frame.
            transform.Rotate(0f, mouseDelta.x * mouseSensitivity, 0f, Space.World);
            pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, -pitchLimit, pitchLimit);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void MoveCharacter(Vector2 input, float deltaTime)
        {
            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed += gravity * deltaTime;
            var direction = Vector2.ClampMagnitude(input, 1f);
            var velocity = (transform.right * direction.x + transform.forward * direction.y) * moveSpeed;
            velocity.y = verticalSpeed;
            var flags = controller.Move(velocity * deltaTime);
            if ((flags & CollisionFlags.Below) != 0 && verticalSpeed < 0f) verticalSpeed = -2f;
        }

        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) ReleaseCursor();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) ReleaseCursor();
        }

        private void OnDisable()
        {
            ReleaseCursor();
        }

        private void OnGUI()
        {
            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    wordWrap = true
                };
            }

            string hint = Cursor.lockState == CursorLockMode.Locked
                ? "WASD  Move   |   Mouse  Look   |   Esc  Release cursor"
                : "Click to explore   |   WASD + Mouse";
            float width = Mathf.Min(470f, Screen.width - 24f);
            GUI.Box(new Rect(12f, Screen.height - 58f, width, 44f), hint, hintStyle);
        }
    }
}
