using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class SelectionManager : MonoBehaviour
{
    public LayerMask selectableLayers = ~0;
    public Color selectionColor = Color.yellow;
    public float clickTolerance = 5f;

    public GameObject SelectedObject { get; private set; }

    private Camera viewCamera;
    private Vector3 clickStart;
    private bool trackingClick;
    private bool dragged;

    private Renderer selectedRenderer;
    private MaterialPropertyBlock originalProperties;

    private static readonly int BaseColor =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorProperty =
        Shader.PropertyToID("_Color");

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        bool navigating = Input.GetMouseButton(0) || Input.GetMouseButton(2);

        if (Input.GetMouseButtonDown(1))
        {
            // Ignore interface clicks and clicks made during navigation.
            trackingClick = !navigating && (EventSystem.current == null ||
                !EventSystem.current.IsPointerOverGameObject());

            clickStart = Input.mousePosition;
            dragged = false;
        }

        if (navigating)
            trackingClick = false;

        if (trackingClick && Input.GetMouseButton(1))
        {
            if (Vector3.Distance(clickStart, Input.mousePosition)
                > clickTolerance)
            {
                dragged = true;
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (trackingClick && !dragged)
                SelectUnderMouse();

            trackingClick = false;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            trackingClick = false;
            ClearSelection();
        }
    }

    private void SelectUnderMouse()
    {
        ClearSelection();

        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(
            ray, out RaycastHit hit, Mathf.Infinity,
            selectableLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        SelectedObject = hit.collider.gameObject;

        // Highlight the renderer belonging to the clicked object.
        selectedRenderer = SelectedObject.GetComponent<Renderer>();

        if (selectedRenderer == null)
            return;

        originalProperties = new MaterialPropertyBlock();
        selectedRenderer.GetPropertyBlock(originalProperties);

        MaterialPropertyBlock highlight =
            new MaterialPropertyBlock();

        selectedRenderer.GetPropertyBlock(highlight);
        highlight.SetColor(BaseColor, selectionColor);
        highlight.SetColor(ColorProperty, selectionColor);
        selectedRenderer.SetPropertyBlock(highlight);
    }

    public bool TryGetOrbitCenter(out Vector3 center)
    {
        center = Vector3.zero;
        if (SelectedObject == null)
            return false;

        Bounds bounds = new Bounds();
        bool hasBounds = false;
        foreach (Renderer renderer in SelectedObject.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
                bounds = renderer.bounds;
            else
                bounds.Encapsulate(renderer.bounds);

            hasBounds = true;
        }

        center = hasBounds ? bounds.center : SelectedObject.transform.position;
        return true;
    }

    public void ClearSelection()
    {
        if (selectedRenderer != null)
            selectedRenderer.SetPropertyBlock(originalProperties);

        selectedRenderer = null;
        originalProperties = null;
        SelectedObject = null;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            trackingClick = false;
    }

    private void OnDisable()
    {
        trackingClick = false;
        ClearSelection();
    }
}
