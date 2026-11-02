using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    [SerializeField] private Transform viewCamera;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float fastMultiplier = 3f;
    [SerializeField] private float lookSensitivity = 2f;

    private float pitch;

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = Input.GetAxisRaw("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * lookSensitivity;

        transform.Rotate(Vector3.up * mouseX, Space.World);

        pitch = Mathf.Clamp(pitch - mouseY, -85f, 85f);
        viewCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float forward = Input.GetAxisRaw("Vertical");

        float vertical = 0f;
        if (Input.GetKey(KeyCode.E)) vertical += 1f;
        if (Input.GetKey(KeyCode.Q)) vertical -= 1f;

        Vector3 direction =
            transform.right * horizontal +
            transform.forward * forward +
            Vector3.up * vertical;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= fastMultiplier;

        transform.position += direction * speed * Time.deltaTime;
    }
}