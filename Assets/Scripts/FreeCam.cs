using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A simple free camera to be added to a Unity game object.
/// 
/// Keys:
///	wasd / arrows	- movement
///	q/e 			- up/down (local space)
///	r/f 			- up/down (world space)
///	pageup/pagedown	- up/down (world space)
///	hold shift		- enable fast movement mode
///	right mouse  	- enable free look
///	mouse			- free look / rotation
///     
/// </summary>
public class FreeCam : MonoBehaviour
{
    public static LocalTransform LocalTransform;

    /// <summary>
    /// Normal speed of camera movement.
    /// </summary>
    public float movementSpeed = 10f;

    /// <summary>
    /// Speed of camera movement when shift is held down,
    /// </summary>
    public float fastMovementSpeed = 100f;

    /// <summary>
    /// Sensitivity for free look.
    /// </summary>
    public float freeLookSensitivity = 3f;

    /// <summary>
    /// Amount to zoom the camera when using the mouse wheel.
    /// </summary>
    public float zoomSensitivity = 10f;

    /// <summary>
    /// Amount to zoom the camera when using the mouse wheel (fast mode).
    /// </summary>
    public float fastZoomSensitivity = 50f;

    /// <summary>
    /// Set to true when free looking (on right mouse button).
    /// </summary>
    private bool looking = false;
    private static bool _InputEnabled = true;

    void Update()
    {
        bool fastMode = false;
        if (_InputEnabled)
        {
            var movementSpeed = Keyboard.current.shiftKey.isPressed ? this.fastMovementSpeed : this.movementSpeed;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                transform.position = transform.position + (-transform.right * movementSpeed * Time.unscaledDeltaTime);
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                transform.position = transform.position + (transform.right * movementSpeed * Time.unscaledDeltaTime);
            }

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                transform.position = transform.position + (transform.forward * movementSpeed * Time.unscaledDeltaTime);
            }

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                transform.position = transform.position + (-transform.forward * movementSpeed * Time.unscaledDeltaTime);
            }

            if (Keyboard.current.qKey.isPressed)
            {
                transform.position = transform.position + (transform.up * movementSpeed * Time.unscaledDeltaTime);
            }

            if (Keyboard.current.eKey.isPressed)
            {
                transform.position = transform.position + (-transform.up * movementSpeed * Time.unscaledDeltaTime);
            }

            if (Keyboard.current.rKey.isPressed)
            {
                transform.position = transform.position + (Vector3.up * movementSpeed * Time.unscaledDeltaTime);
            }

            if (Keyboard.current.fKey.isPressed)
            {
                transform.position = transform.position + (-Vector3.up * movementSpeed * Time.unscaledDeltaTime);
            }

            if (looking)
            {
                float newRotationX = transform.localEulerAngles.y + Pointer.current.delta.x.ReadValue() * freeLookSensitivity;
                float newRotationY = transform.localEulerAngles.x - Pointer.current.delta.y.ReadValue() * freeLookSensitivity;
                transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
            }

            float axis = Mouse.current.scroll.y.ReadValue();
            if (axis != 0)
            {
                var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
                transform.position = transform.position + transform.forward * axis * zoomSensitivity;
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                StartLooking();
            }
            else if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                StopLooking();
            }
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
            _InputEnabled = _InputEnabled == false;

        LocalTransform = LocalTransform.FromMatrix(transform.localToWorldMatrix);
    }

    void OnDisable()
    {
        StopLooking();
    }

    /// <summary>
    /// Enable free looking.
    /// </summary>
    public void StartLooking()
    {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Disable free looking.
    /// </summary>
    public void StopLooking()
    {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}