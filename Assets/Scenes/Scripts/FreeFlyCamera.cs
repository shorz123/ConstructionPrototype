using UnityEngine;

// Attach to Main Camera. Its parent, when present, carries camera movement.
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class FreeFlyCamera : MonoBehaviour
{

    //bookmark


    [Min(0.01f)] public float walkSpeed = 5f;
    [Min(0.01f)] public float turnSpeed = 90f;
    [Min(1f)] public float dragRange = 150f;
    [Min(0f)] public float dragDeadZone = 5f;
    public float lookSensitivity = 0.2f;
    public float panSpeed = 0.01f;
    [Min(0.1f)] public float fallbackOrbitDistance = 10f;

    private Transform movementRoot;
    private Vector3 dragOrigin;
    private Vector3 previousMouse;
    private float yaw;
    private float pitch;
    private bool walking;
    private bool leftDragging;
    private bool middleDragging;
    private bool looking;
    private bool panning;
    private bool orbiting;
    private Vector3 orbitPivot;
    private Camera viewCamera;
    private SelectionManager selection;
    private float orbitRadius;

    private void OnEnable()
    {
        viewCamera = GetComponent<Camera>();
        selection = GetComponent<SelectionManager>();
        movementRoot = transform.parent != null ? transform.parent : transform;
        yaw = transform.eulerAngles.y;
        pitch = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
        StopNavigation();
    }

    private float DragAmount(float pixels)
    {
        float magnitude = Mathf.Max(0f, Mathf.Abs(pixels) - dragDeadZone);
        return Mathf.Sign(pixels) * Mathf.Clamp01(magnitude / Mathf.Max(1f, dragRange));
    }

    private void Update()
    {
        if (!Application.isFocused) return;
        Vector3 mouse = Input.mousePosition;
        Vector3 delta = mouse - previousMouse;
        previousMouse = mouse;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopNavigation();
            return;
        }

        if (Input.GetMouseButtonDown(0)) { leftDragging = true; delta = Vector3.zero; }
        if (Input.GetMouseButtonDown(2)) { middleDragging = true; delta = Vector3.zero; }
        if (!Input.GetMouseButton(0)) leftDragging = false;
        if (!Input.GetMouseButton(2)) middleDragging = false;

        // Wheel adjusts walking speed rather than moving the viewpoint instantly.
        walkSpeed = Mathf.Clamp(walkSpeed * Mathf.Exp(Input.mouseScrollDelta.y * 0.15f), 0.1f, 100f);
        float boost = Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus) ? 3f : 1f;
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        // Middle-button orbit takes priority; Ctrl + left looks, Shift + left pans.
        bool wantsOrbit = middleDragging && control;
        bool wantsPan = !wantsOrbit && (middleDragging || (leftDragging && shift && !control));
        bool wantsLook = !wantsOrbit && !wantsPan && leftDragging && control;
        bool wantsWalk = !wantsOrbit && !wantsPan && !wantsLook && leftDragging;

        // Switching modifiers mid-drag must not reuse the old walking displacement.
        if (wantsWalk && !walking)
            dragOrigin = mouse;
        if (wantsPan != panning || wantsLook != looking)
            delta = Vector3.zero;
        walking = wantsWalk;
        panning = wantsPan;
        looking = wantsLook;

        if (wantsOrbit && !orbiting)
        {
            // Prefer the selected object's visible center, regardless of pointer position.
            if (selection == null || !selection.TryGetOrbitCenter(out orbitPivot))
            {
                orbitPivot = Physics.Raycast(viewCamera.ScreenPointToRay(mouse), out RaycastHit hit)
                    ? hit.point
                    : transform.position + transform.forward * Mathf.Max(0.1f, fallbackOrbitDistance);
            }

            Vector3 direction = orbitPivot - transform.position;
            orbitRadius = direction.magnitude;
            if (orbitRadius < 0.001f)
            {
                // There is no viewing direction when the camera is at the pivot.
                orbitRadius = Mathf.Max(0.1f, fallbackOrbitDistance);
                direction = transform.forward;
            }
            direction.Normalize();
            yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
            delta = Vector3.zero;
        }
        orbiting = wantsOrbit;

        if (orbiting)
        {
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -89f, 89f);
            Quaternion nextRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 nextPosition = orbitPivot - nextRotation * Vector3.forward * orbitRadius;
            movementRoot.position += nextPosition - transform.position;
        }
        else if (panning)
        {
            // Glide sideways and vertically without changing the view direction.
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            movementRoot.position += (-right * delta.x - Vector3.up * delta.y) * panSpeed;
        }
        else if (looking)
        {
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -89f, 89f);
        }
        else if (walking)
        {
            Vector3 drag = mouse - dragOrigin;
            float forwardAmount = DragAmount(drag.y);
            yaw += DragAmount(drag.x) * turnSpeed * Time.deltaTime;
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            movementRoot.position += forward * forwardAmount * walkSpeed * boost * Time.deltaTime;
        }

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void OnApplicationFocus(bool hasFocus) { StopNavigation(); }
    private void OnDisable() { StopNavigation(); }

    private void StopNavigation()
    {
        walking = looking = panning = orbiting = leftDragging = middleDragging = false;
        previousMouse = Input.mousePosition;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
