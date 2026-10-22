using UnityEngine;
using UnityEngine.EventSystems;

// Lets a character card be dragged around the table (never off it).
// Dropped over a character slot, it snaps in; if another card is there, the two swap places.
[RequireComponent(typeof(RectTransform))]
public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    RectTransform rectTransform;
    Canvas canvas;
    Vector3 dragStartPosition;

    void Awake()
    {
        rectTransform = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPosition = transform.position;
        // Draw the dragged card on top of the others.
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        float scale = canvas ? canvas.scaleFactor : 1f;
        rectTransform.anchoredPosition += eventData.delta / scale;
        KeepOnTable();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        foreach (var slot in FindObjectsByType<CharacterSlot>(FindObjectsInactive.Exclude))
        {
            if (!slot.Contains(transform.position)) continue;

            // Anyone already in this slot moves to where this card came from.
            foreach (var other in FindObjectsByType<DraggableCard>(FindObjectsInactive.Exclude))
            {
                if (other != this && slot.Contains(other.transform.position))
                    other.transform.position = dragStartPosition;
            }
            transform.position = slot.transform.position;
            return;
        }
    }

    // Pushes the card back inside the canvas if any edge went past it.
    void KeepOnTable()
    {
        if (!canvas) return;
        var table = (RectTransform)canvas.transform;
        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector2 min = table.InverseTransformPoint(corners[0]);
        Vector2 max = table.InverseTransformPoint(corners[2]);
        Rect area = table.rect;

        Vector2 shift = Vector2.zero;
        if (min.x < area.xMin) shift.x = area.xMin - min.x;
        else if (max.x > area.xMax) shift.x = area.xMax - max.x;
        if (min.y < area.yMin) shift.y = area.yMin - min.y;
        else if (max.y > area.yMax) shift.y = area.yMax - max.y;

        if (shift != Vector2.zero) transform.position += table.TransformVector(shift);
    }
}
