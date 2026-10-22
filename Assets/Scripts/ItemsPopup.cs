using UnityEngine;
using UnityEngine.UI;

// Window that shows one character's 6 item slots. Item cards sit in the slots and can be
// dragged from one slot to another (dropping on a filled slot swaps the two cards).
// Only the close button closes it.
public class ItemsPopup : MonoBehaviour
{
    public Text titleText;
    public RectTransform row;
    public RectTransform[] slots;
    public ItemCard itemCardPrefab;
    public Text emptyText;
    public Button closeButton;
    public float rowMargin = 32f;
    public float rowTopSpace = 100f;

    CharacterCard owner;
    ItemCard[] cardsInSlots;
    readonly System.Collections.Generic.List<ItemCard> injuryCards = new System.Collections.Generic.List<ItemCard>();
    int dragFrom = -1;

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
    }

    public void Show(CharacterCard character)
    {
        owner = character;
        owner.EnsureSlots();
        cardsInSlots = new ItemCard[slots.Length];

        foreach (var slot in slots)
            foreach (Transform child in slot) Destroy(child.gameObject);

        for (int i = 0; i < slots.Length && i < owner.items.Count; i++)
        {
            var item = owner.items[i];
            if (!item) continue;
            var card = Instantiate(itemCardPrefab);
            card.SetItem(item);
            card.popup = this;
            PlaceCard(card, i);
        }

        // 负伤 cards take the empty slots. They can't be dragged; clicking one shows it (or pays to get rid of it).
        injuryCards.Clear();
        foreach (var injury in owner.injuries)
        {
            if (!injury) continue;
            var card = Instantiate(itemCardPrefab);
            card.SetInjury(injury);
            card.draggable = false;
            card.onClick = c =>
            {
                var deck = InjuryDeck.Find();
                if (deck) StartCoroutine(deck.OfferPaidHeal(owner, c.injury, () => Show(owner)));
            };
            injuryCards.Add(card);
        }
        PlaceInjuries();
        if (emptyText) emptyText.gameObject.SetActive(false);
        UpdateEquippedLook();
        UpdateTitle();
        FitRow(slots.Length);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void UpdateTitle()
    {
        string equipped = owner.Equipped ? owner.Equipped.itemName : "无";
        string hint = (owner.equipmentLocked ? "战斗中不能更换装备" : "点击装备卡来装备") + "，点击负伤牌查看";
        titleText.text = owner.playerName + " 的道具 (" + owner.UsedSlots + "/" + CharacterCard.ItemSlots + ")"
            + "   装备中：" + equipped + "   （" + hint + "）";
    }

    // Clicking a card equips it (or unequips it if it's already equipped).
    public void OnCardClicked(ItemCard card)
    {
        int slot = System.Array.IndexOf(cardsInSlots, card);
        if (slot < 0) return;
        if (card.item && card.item.kind == ItemKind.Consumable)
        {
            titleText.text = owner.playerName + " 的道具   （" + card.item.itemName + " 是消耗品，在战斗中使用，不能装备）";
            return;
        }
        if (!owner.ToggleEquip(slot))
        {
            titleText.text = owner.playerName + " 的道具   （" + (owner.equipmentLocked ? "战斗中不能更换装备"
                : card.item.itemName + " 是" + (card.item.exclusiveTo ? card.item.exclusiveTo.className : "其他职业") + "专属，不能装备") + "）";
            return;
        }
        UpdateEquippedLook();
        UpdateTitle();
    }

    // The equipped card gets a thick gold border.
    void UpdateEquippedLook()
    {
        for (int i = 0; i < cardsInSlots.Length; i++)
        {
            if (!cardsInSlots[i]) continue;
            var outline = cardsInSlots[i].GetComponent<Outline>();
            if (!outline) continue;
            bool equipped = i == owner.equippedSlot;
            outline.effectColor = equipped ? new Color(0.95f, 0.7f, 0.1f) : new Color(0.1f, 0.1f, 0.1f);
            outline.effectDistance = equipped ? new Vector2(9f, -9f) : new Vector2(3f, -3f);
        }
    }

    // Injury cards fill the slots no item is in, in order.
    void PlaceInjuries()
    {
        int next = 0;
        foreach (var card in injuryCards)
        {
            while (next < slots.Length && cardsInSlots[next]) next++;
            if (next >= slots.Length) { Destroy(card.gameObject); continue; }   // More cards than slots (shouldn't happen).
            var rt = (RectTransform)card.transform;
            rt.SetParent(slots[next], false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            next++;
        }
    }

    void PlaceCard(ItemCard card, int slot)
    {
        var rt = (RectTransform)card.transform;
        rt.SetParent(slots[slot], false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        cardsInSlots[slot] = card;
    }

    int SlotUnder(GameObject hit)
    {
        if (!hit) return -1;
        for (int i = 0; i < slots.Length; i++)
            if (hit.transform == slots[i] || hit.transform.IsChildOf(slots[i])) return i;
        return -1;
    }

    public void BeginCardDrag(ItemCard card)
    {
        dragFrom = System.Array.IndexOf(cardsInSlots, card);
        var group = card.GetComponent<CanvasGroup>();
        if (!group) group = card.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;  // So we can see which slot is under the pointer.
        card.transform.SetParent(row.parent, true);
        card.transform.SetAsLastSibling();
    }

    public void DragCard(ItemCard card, Vector2 screenDelta)
    {
        var parent = card.transform.parent;
        card.transform.localPosition += (Vector3)(screenDelta / parent.lossyScale.x);
    }

    public void EndCardDrag(ItemCard card, GameObject dropTarget)
    {
        card.GetComponent<CanvasGroup>().blocksRaycasts = true;
        int target = SlotUnder(dropTarget);
        if (dragFrom < 0) return;
        if (target < 0 || target == dragFrom)
        {
            PlaceCard(card, dragFrom);
            return;
        }

        // Move into the target slot; if something is already there, it swaps into the old slot.
        var other = cardsInSlots[target];
        cardsInSlots[dragFrom] = null;
        if (other) PlaceCard(other, dragFrom);
        PlaceCard(card, target);

        var items = owner.items;
        (items[dragFrom], items[target]) = (items[target], items[dragFrom]);
        // The equipped item keeps being equipped after it moves.
        if (owner.equippedSlot == dragFrom) owner.equippedSlot = target;
        else if (owner.equippedSlot == target) owner.equippedSlot = dragFrom;
        dragFrom = -1;
        PlaceInjuries();
        UpdateEquippedLook();
    }

    // Shrinks the slot row so all slots fit side by side in the window.
    void FitRow(int count)
    {
        var window = (RectTransform)row.parent;
        Vector2 area = window.rect.size - new Vector2(rowMargin * 2f, rowMargin + rowTopSpace);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        float cardWidth = ((RectTransform)itemCardPrefab.transform).rect.width;
        float needed = count * cardWidth + Mathf.Max(0, count - 1) * (layout ? layout.spacing : 0f);
        float scale = needed > area.x ? area.x / needed : 1f;

        row.localScale = new Vector3(scale, scale, 1f);
        row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0.5f);
        row.sizeDelta = area / scale;
        row.anchoredPosition = new Vector2(0f, (rowMargin - rowTopSpace) * 0.5f);
    }
}
