using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Shows one ItemData on a card. In a character's item window it drags between slots;
// elsewhere it can be dragged around inside its parent area.
public class ItemCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public ItemData item;

    // Set when the card sits in a character's item slots; dragging then moves it between slots.
    [HideInInspector] public ItemsPopup popup;

    // Display-only cards (combat, setup) can't be dragged; a click calls onClick instead (e.g. to enlarge the card).
    [HideInInspector] public bool draggable = true;
    public System.Action<ItemCard> onClick;

    [Header("UI")]
    public Text valueText;
    public Text nameText;
    public Image elementCircle;
    public Text elementText;
    public Image itemImage;
    public Text descriptionText;
    public Text effectsText;

    void Start()
    {
        Refresh();
    }

    void OnValidate()
    {
        Refresh();
    }

    public void SetItem(ItemData data)
    {
        item = data;
        Refresh();
    }

    // The same card layout shows a 负伤 card: red, its element, 难度, kind of wound and effect.
    [HideInInspector] public InjuryData injury;
    static readonly Color InjuryColor = new Color(1f, 0.86f, 0.86f);

    public void SetInjury(InjuryData data)
    {
        item = null;
        injury = data;
        if (!data) return;
        if (valueText) valueText.text = "";
        if (nameText) nameText.text = data.Title;
        if (elementCircle) { elementCircle.enabled = !data.special; elementCircle.color = ElementToken.ColorFor(data.element); }
        if (elementText) elementText.text = data.special ? "" : CombatCard.ElementChar(data.element);
        if (itemImage) itemImage.enabled = false;
        if (descriptionText) descriptionText.text = data.special ? "特殊负伤（无法去除）"
            : "负伤（难度 " + data.Level + "）\n" + InjuryData.TypeOf(data.element);
        if (effectsText) effectsText.text = "<b>效果：</b>" + data.effect;
        var background = GetComponent<Image>();
        if (background) background.color = InjuryColor;
    }

    public void Refresh()
    {
        if (!item) return;
        if (valueText) valueText.text = item.value > 0 ? item.value.ToString() : "";
        if (nameText) nameText.text = item.itemName;
        if (elementCircle) { elementCircle.enabled = item.hasElement; elementCircle.color = ElementToken.ColorFor(item.element); }
        if (elementText) elementText.text = item.hasElement ? CombatCard.ElementChar(item.element) : "";
        if (itemImage)
        {
            // Crops to fill the frame instead of stretching.
            itemImage.sprite = item.image;
            itemImage.enabled = item.image != null;
            var fitter = itemImage.GetComponent<AspectRatioFitter>();
            if (fitter && item.image) fitter.aspectRatio = item.image.rect.width / item.image.rect.height;
        }
        if (descriptionText) descriptionText.text = item.description;
        if (effectsText)
        {
            var sb = new StringBuilder();
            foreach (var effect in item.effects)
                sb.Append("<b>").Append(effect.label).Append("：</b>").Append(effect.value).Append('\n');
            effectsText.text = sb.ToString().TrimEnd('\n');
        }
    }

    // In a character's item window, a click (not a drag) equips the card.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (popup) popup.OnCardClicked(this);
        else if (onClick != null) onClick(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (popup)
        {
            popup.BeginCardDrag(this);
            return;
        }
        if (!draggable) return;
        transform.SetAsLastSibling();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (popup) popup.EndCardDrag(this, eventData.pointerCurrentRaycast.gameObject);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (popup)
        {
            popup.DragCard(this, eventData.delta);
            return;
        }
        if (!draggable) return;

        var parent = transform.parent as RectTransform;
        if (!parent) return;
        var rt = (RectTransform)transform;
        Vector3 pos = rt.localPosition + (Vector3)(eventData.delta / parent.lossyScale.x);

        // Keep the whole card inside the parent area.
        Rect area = parent.rect;
        Vector2 size = rt.rect.size;
        pos.x = Mathf.Clamp(pos.x, area.xMin + size.x * rt.pivot.x, area.xMax - size.x * (1 - rt.pivot.x));
        pos.y = Mathf.Clamp(pos.y, area.yMin + size.y * rt.pivot.y, area.yMax - size.y * (1 - rt.pivot.y));
        rt.localPosition = pos;
    }
}
