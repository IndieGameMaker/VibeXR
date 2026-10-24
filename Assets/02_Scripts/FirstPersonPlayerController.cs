using UnityEngine;
using UnityEngine.InputSystem;

namespace VibeXR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayerController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0.1f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Range(30f, 89f)] private float lookLimit = 80f;
        [SerializeField] private Transform viewPivot;

        private CharacterController characterController;
        private MobileVirtualJoystick mobileJoystick;
        private float verticalVelocity;
        private float pitch;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (viewPivot == null && Camera.main != null)
            {
                Transform cameraTransform = Camera.main.transform;
                viewPivot = cameraTransform.parent;
                if (viewPivot == transform) viewPivot = cameraTransform;
            }

            if (Application.isMobilePlatform || Touchscreen.current != null)
                mobileJoystick = MobileVirtualJoystick.Create();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Mouse mouse = Mouse.current;
            if (!Application.isMobilePlatform && mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (!Application.isMobilePlatform && mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(Vector3.up, look.x, Space.World);
                pitch = Mathf.Clamp(pitch - look.y, -lookLimit, lookLimit);
                if (viewPivot != null) viewPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector2 movement = Vector2.zero;
            if (mobileJoystick != null && mobileJoystick.isActiveAndEnabled)
            {
                movement = mobileJoystick.Input;
            }
            else if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) movement.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) movement.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) movement.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) movement.y += 1f;
            }

            Vector3 move = transform.right * movement.x + transform.forward * movement.y;
            if (move.sqrMagnitude > 1f) move.Normalize();

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 velocity = move * moveSpeed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void OnDisable()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
