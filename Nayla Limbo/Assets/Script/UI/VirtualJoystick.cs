using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Components")]
    public RectTransform background;
    public RectTransform handle;
    public Canvas canvas;

    [Header("Settings")]
    [Range(0f, 1f)] public float handleRange = 0.6f;
    [Range(0f, 0.5f)] public float deadZone = 0.1f;

    public bool dynamic = true;

    int pointerId = -1;
    Vector2 input = Vector2.zero;
    Vector2 bgStartPos;

    public Vector2 Direction => input;
    public float Magnitude => input.magnitude;
    public float Horizontal => Direction.x;
    public float Vertical => Direction.y;

    void Awake()
    {
        if (background == null) background = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        bgStartPos = background.anchoredPosition;
    }

    void Start()
    {
        if (dynamic) background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerId == -1)
        {
            pointerId = eventData.pointerId;

            if (dynamic)
            {
                background.gameObject.SetActive(true);

                Vector2 localPoint;
                Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)background.parent, eventData.position, cam, out localPoint))
                {
                    background.anchoredPosition = localPoint;
                }
            }

            OnDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;

        Vector2 localPoint;
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, cam, out localPoint))
            return;

        Vector2 bgSize = background.sizeDelta;
        Vector2 normalized = new Vector2(
            localPoint.x / (bgSize.x * 0.5f),
            localPoint.y / (bgSize.y * 0.5f)
        );

        if (normalized.magnitude > 1f) normalized = normalized.normalized;

        if (normalized.magnitude < deadZone) input = Vector2.zero;
        else input = normalized;

        Vector2 handlePos = new Vector2(
            input.x * (bgSize.x * 0.5f) * handleRange,
            input.y * (bgSize.y * 0.5f) * handleRange
        );

        handle.anchoredPosition = handlePos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;

        pointerId = -1;
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;

        if (dynamic)
        {
            background.gameObject.SetActive(false);
            background.anchoredPosition = bgStartPos;
        }
    }
}
