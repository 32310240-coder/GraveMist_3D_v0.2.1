using UnityEngine;

public class DragAreaController : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public Camera mainCamera;
    public LayerMask boardLayer;

    [Header("State")]
    private bool dragging = false;
    private bool draggable = true;

    // Drag data
    private Vector2 dragStartScreen;
    private float dragStartTime;
    private Vector3 launchWorldPos;

    public void SetDraggable(bool value)
    {
        draggable = value;
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    // =====================
    // Mouse Input (Editor)
    // =====================
    void HandleMouse()
    {
        if (!draggable) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (RaycastDragArea(Input.mousePosition))
            {
                StartDrag(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(0) && dragging)
        {
            EndDrag(Input.mousePosition);
        }
    }

    // =====================
    // Touch Input (Mobile)
    // =====================
    void HandleTouch()
    {
        if (!draggable || Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            if (RaycastDragArea(touch.position))
            {
                StartDrag(touch.position);
            }
        }
        else if (touch.phase == TouchPhase.Ended && dragging)
        {
            EndDrag(touch.position);
        }
    }

    // =====================
    // Drag Core Logic
    // =====================
    void StartDrag(Vector2 screenPos)
    {
        dragging = true;
        dragStartScreen = screenPos;
        dragStartTime = Time.time;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, boardLayer))
        {
            launchWorldPos = hit.point; // ← Yは触らない
            Debug.Log($"▶ Drag Start / {launchWorldPos}");
        }
    }


    void EndDrag(Vector2 screenPos)
    {
        dragging = false;

        Vector2 dragVec = screenPos - dragStartScreen;
        float dragTime = Time.time - dragStartTime;

        if (dragVec.magnitude < 5f)
        {
            Debug.Log("▶ ドラッグ短すぎ → キャンセル");
            return;
        }

        Vector2 dir2D = dragVec.normalized;
        float distance = dragVec.magnitude;
        float speed = distance / Mathf.Max(dragTime, 0.01f);

        Debug.Log($"▶ Drag End dir={dir2D} dist={distance} speed={speed}");

        gameManager.OnShakeRelease(
            launchWorldPos,
            dir2D,
            distance,
            speed
        );
    }

    // =====================
    // Utility
    // =====================
    bool RaycastDragArea(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit)
            && hit.collider.gameObject == gameObject;
    }
}
