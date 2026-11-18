using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum Species
{
    Taoyi,
    Fox,
    Baize,
    Human
}

public enum Element
{
    Wood,
    Fire,
    Earth,
    Metal,
    Water
}

public class CharacterCard : MonoBehaviour, IPointerClickHandler
{
    public string playerName;
    public Sprite portrait;
    public Species species;
    public Element primaryElement;
    public BackgroundData background;
    public ClassData characterClass;

    // False during setup until the player picks their primary element; the element badge stays hidden until then.
    [System.NonSerialized] public bool elementChosen = true;

    // items[0..5] are the 6 backpack slots (消耗品; an empty slot is null). Equipment (装备) doesn't take a
    // backpack slot: it's kept after them, items[6], items[7]…  (EnsureSlots keeps it that way).
    public const int ItemSlots = 6;
    public List<ItemData> items = new List<ItemData>();

    // The one equipped item, by slot (-1 = nothing). Locked while a combat is running.
    public int equippedSlot = -1;
    [System.NonSerialized] public bool equipmentLocked;

    // 负伤 (injury) cards this character holds during the game. They take up item slots too.
    [System.NonSerialized] public List<InjuryData> injuries = new List<InjuryData>();
    [System.NonSerialized] public int sitOutEvent;   // "无法在下一次事件中上场": the event they must sit out.

    public ItemData Equipped => equippedSlot >= 0 && equippedSlot < items.Count ? items[equippedSlot] : null;

    [Header("UI")]
    public Text playerNameText;
    public Image portraitImage;
    public Text speciesText;
    public Text elementText;
    public Text equippedText;
    public Image elementBadge;
    public Text elementBadgeText;
    public Text backgroundText;
    public Button backgroundButton;
    public Button itemsButton;
    public RectTransform tokenArea;

    void Awake()
    {
        EnsureSlots();
    }

    void OnEnable() { L.Changed += Refresh; }
    void OnDisable() { L.Changed -= Refresh; }

    void Start()
    {
        Refresh();
        if (backgroundButton) backgroundButton.onClick.AddListener(ShowBackground);
        if (itemsButton) itemsButton.onClick.AddListener(ShowItems);
        // Click the portrait: full screen. Except while a drawn item waits: then the click gives it to this card.
        var zoom = ImageZoom.Enable(portraitImage);
        if (zoom) zoom.passClickToParent = () => { var deck = FindFirstObjectByType<ItemDeck>(); return deck && deck.CardWaiting; };
    }

    // Clicking the card gives it a freshly drawn item card, if one is waiting.
    public void OnPointerClick(PointerEventData eventData)
    {
        var deck = FindFirstObjectByType<ItemDeck>();
        if (deck) deck.GiveTo(this);
    }

    public void ShowItems()
    {
        var popup = FindFirstObjectByType<ItemsPopup>(FindObjectsInactive.Include);
        if (popup) popup.Show(this);
    }

    public static bool IsEquipment(ItemData item) => item && item.kind != ItemKind.Consumable;

    // Keeps the 6 backpack slots first (消耗品 only) and the equipment after them; the equipped item stays equipped.
    public void EnsureSlots()
    {
        if (items == null) items = new List<ItemData>();
        var bag = new ItemData[ItemSlots];
        var gear = new List<ItemData>();
        var extra = new List<ItemData>();
        int newEquipped = -1;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!item) continue;
            if (IsEquipment(item))
            {
                if (i == equippedSlot) newEquipped = ItemSlots + gear.Count;
                gear.Add(item);
            }
            else if (i < ItemSlots && !bag[i]) bag[i] = item;
            else extra.Add(item);
        }
        foreach (var item in extra)
        {
            int free = System.Array.IndexOf(bag, null);
            if (free >= 0) bag[free] = item;
            else gear.Add(item);   // Shouldn't happen: the backpack was over full.
        }
        items = new List<ItemData>(bag);
        items.AddRange(gear);
        equippedSlot = newEquipped;
    }

    public int ItemCount
    {
        get
        {
            int count = 0;
            foreach (var item in items) if (item) count++;
            return count;
        }
    }

    // 消耗品 and 负伤 cards share the 6 backpack slots; equipment doesn't count.
    public int BackpackItemCount
    {
        get
        {
            int count = 0;
            foreach (var item in items) if (item && !IsEquipment(item)) count++;
            return count;
        }
    }

    public int UsedSlots => BackpackItemCount + injuries.Count;

    public bool HasFreeItemSlot => UsedSlots < ItemSlots;

    // Equipment always fits; anything else needs a free backpack slot.
    public bool HasRoomFor(ItemData item) => IsEquipment(item) || HasFreeItemSlot;

    // Puts the item in the first empty slot. Returns false when the backpack is full
    // or the item is 专属 to another class.
    public bool TryAddItem(ItemData item)
    {
        EnsureSlots();
        if (!CanHold(item) || !HasRoomFor(item)) return false;
        if (IsEquipment(item))
        {
            items.Add(item);
            Refresh();
            return true;
        }
        for (int i = 0; i < ItemSlots; i++)
        {
            if (items[i]) continue;
            items[i] = item;
            Refresh();
            return true;
        }
        return false;
    }

    // Only 装备 can be equipped, and a class-exclusive one (专属) only by that class.
    // Exception from the card: a 九尾 (Fox) can equip the 易容面具.
    public bool CanEquip(ItemData item)
    {
        if (!item) return false;
        if (item.mechanic == ItemMechanic.Disguise && species == Species.Fox) return true;
        if (item.kind != ItemKind.Weapon) return false;
        return CanHold(item);
    }

    // A class-exclusive (专属) item can only go to a player of that class.
    public bool CanHold(ItemData item)
    {
        return item && (!item.exclusiveTo || item.exclusiveTo == characterClass);
    }

    // Equips the item in this slot, or unequips it if it's already equipped.
    public bool ToggleEquip(int slot)
    {
        if (equipmentLocked) return false;
        if (equippedSlot != slot && !CanEquip(slot < items.Count ? items[slot] : null)) return false;
        equippedSlot = equippedSlot == slot ? -1 : slot;
        Refresh();
        return true;
    }

    // Adds an element token to this card and returns how many tokens arrived. 负伤 can change that:
    // "无法从任何来源获得水token" blocks an element, "每次获得主属性token时，额外获得3枚木token" adds more.
    public int GainToken(Element element, ElementToken prefab, bool temporary = false)
    {
        if (!prefab || !tokenArea) return 0;
        foreach (var injury in injuries)
        {
            if (!injury) continue;
            var block = System.Text.RegularExpressions.Regex.Match(injury.effect, @"无法从任何来源获得\s*(金|木|水|火|土)\s*token");
            if (block.Success && CombatCard.ElementChar(element) == block.Groups[1].Value) return 0;
        }
        int added = 1;
        PlaceToken(element, prefab, temporary);
        if (element != primaryElement) return added;
        foreach (var injury in injuries)
        {
            if (!injury) continue;
            var bonus = System.Text.RegularExpressions.Regex.Match(injury.effect, @"获得主属性\s*token\s*时，额外获得\s*(\d+)\s*枚\s*(金|木|水|火|土)\s*token");
            if (!bonus.Success) continue;
            for (int i = 0; i < int.Parse(bonus.Groups[1].Value); i++)
            {
                PlaceToken(InjuryDeck.ElementOf(bonus.Groups[2].Value), prefab, temporary);
                added++;
            }
        }
        return added;
    }

    void PlaceToken(Element element, ElementToken prefab, bool temporary)
    {
        var token = Instantiate(prefab, tokenArea);
        token.temporary = temporary;
        token.SetElement(element);
        float half = tokenArea.rect.width * 0.5f;
        ((RectTransform)token.transform).anchoredPosition = token.ClampToParent(new Vector2(Random.Range(-half, half), Random.Range(-20f, 20f)));
    }

    public void ShowBackground()
    {
        var popup = FindFirstObjectByType<BackgroundPopup>(FindObjectsInactive.Include);
        if (popup) popup.Show(background);
    }

    void OnValidate()
    {
        EnsureSlots();
        Refresh();
    }

    public void Refresh()
    {
        if (playerNameText) playerNameText.text = playerName;
        if (portraitImage)
        {
            // The image sits inside a masked frame and is scaled to cover it, so it crops instead of stretching.
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
            var fitter = portraitImage.GetComponent<AspectRatioFitter>();
            if (fitter && portrait) fitter.aspectRatio = portrait.rect.width / portrait.rect.height;
        }
        if (speciesText) speciesText.text = L.T("card.species") + L.SpeciesName(species) + (characterClass ? "   " + L.T("card.class") + characterClass.className : "");
        if (elementText) elementText.text = L.T("card.element") + (elementChosen ? L.ElementName(primaryElement) : "—");
        if (elementBadge) elementBadge.enabled = elementChosen;
        if (elementBadgeText) elementBadgeText.enabled = elementChosen;
        if (equippedText)
        {
            var sb = new System.Text.StringBuilder(L.T("card.equipped") + (Equipped ? Equipped.itemName : "—"));
            if (injuries.Count > 0)
            {
                sb.Append("   负伤：");
                for (int i = 0; i < injuries.Count; i++) sb.Append(i > 0 ? "、" : "").Append(CombatCard.ElementChar(injuries[i].element));
            }
            equippedText.text = sb.ToString();
        }
        // Fixed primary element token in the top right corner (not draggable).
        if (elementBadge) elementBadge.color = ElementToken.ColorFor(primaryElement);
        if (elementBadgeText) elementBadgeText.text = L.ElementName(primaryElement);
        if (backgroundText) backgroundText.text = L.T("card.background") + (background ? background.backgroundName : "");
        if (itemsButton)
        {
            var label = itemsButton.GetComponentInChildren<Text>();
            if (label) label.text = L.T("card.items", UsedSlots, ItemSlots);
        }
    }
}
