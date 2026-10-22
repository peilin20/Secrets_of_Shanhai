using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A resource token of one element.
// A token on a card can be dragged, but only inside its card's token area.
// A library token stays put: dragging it pulls out a copy that can be dropped on any character card.
[RequireComponent(typeof(Image))]
public class ElementToken : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Element element;
    public bool isLibrarySource;
    public bool temporary;   // Only lasts for the current event (e.g. 祈雨符's water tokens); drawn faded.

    RectTransform rectTransform;
    Canvas canvas;
    bool isFreeDragging;

    public static Color ColorFor(Element e)
    {
        switch (e)
        {
            case Element.Wood: return new Color(0.2f, 0.65f, 0.3f);
            case Element.Fire: return new Color(0.85f, 0.2f, 0.15f);
            case Element.Earth: return new Color(0.85f, 0.65f, 0.15f);
            case Element.Metal: return new Color(0.7f, 0.72f, 0.75f);
            case Element.Water: return new Color(0.15f, 0.4f, 0.85f);
        }
        return Color.white;
    }

    void Awake()
    {
        rectTransform = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
    }

    void OnValidate()
    {
        Refresh();
    }

    public void SetElement(Element e)
    {
        element = e;
        name = e + "Token";
        Refresh();
    }

    public void Refresh()
    {
        var color = ColorFor(element);
        if (temporary) color.a = 0.5f;
        GetComponent<Image>().color = color;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLibrarySource)
        {
            // Hand the drag over to a fresh copy so the library token stays in place.
            var copy = Instantiate(this, canvas.transform);
            copy.isLibrarySource = false;
            copy.SetElement(element);
            copy.rectTransform.anchorMin = copy.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            copy.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            copy.rectTransform.sizeDelta = rectTransform.rect.size;
            copy.StartFreeDrag(eventData);
            eventData.pointerDrag = copy.gameObject;
            return;
        }

        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLibrarySource) return;

        if (isFreeDragging)
        {
            FollowPointer(eventData);
            return;
        }

        // Parent scale includes the canvas scale and any card scaling, so the token tracks the mouse exactly.
        float scale = transform.parent ? transform.parent.lossyScale.x : 1f;
        rectTransform.anchoredPosition = ClampToParent(rectTransform.anchoredPosition + eventData.delta / scale);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isFreeDragging) return;
        isFreeDragging = false;
        GetComponent<Image>().raycastTarget = true;

        var hit = eventData.pointerCurrentRaycast.gameObject;
        var card = hit ? hit.GetComponentInParent<CharacterCard>() : null;
        if (card && card.tokenArea)
        {
            // Dropped on a character card: place it in that card's token area where it was let go.
            transform.SetParent(card.tokenArea, false);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(card.tokenArea, eventData.position, eventData.pressEventCamera, out Vector2 local);
            rectTransform.anchoredPosition = ClampToParent(local);
        }
        else
        {
            // Dropped anywhere else: the token goes back to the library.
            Destroy(gameObject);
        }
    }

    void StartFreeDrag(PointerEventData eventData)
    {
        isFreeDragging = true;
        // Let the pointer see through the token so we can tell what it is dropped on.
        GetComponent<Image>().raycastTarget = false;
        transform.SetAsLastSibling();
        FollowPointer(eventData);
    }

    void FollowPointer(PointerEventData eventData)
    {
        var canvasRect = (RectTransform)canvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 local);
        rectTransform.anchoredPosition = local;
    }

    // Keeps the token fully inside its parent's rectangle (token anchored at the parent's center).
    public Vector2 ClampToParent(Vector2 pos)
    {
        var parent = transform.parent as RectTransform;
        if (!parent) return pos;
        Vector2 half = (parent.rect.size - rectTransform.rect.size) * 0.5f;
        pos.x = Mathf.Clamp(pos.x, -half.x, half.x);
        pos.y = Mathf.Clamp(pos.y, -half.y, half.y);
        return pos;
    }
}
