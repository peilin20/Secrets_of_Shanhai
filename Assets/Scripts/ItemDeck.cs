using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Item card pile: click to draw a card, then click a character card to give it to that player.
// The drawn card shrinks and flies to the character card, then joins their items.
public class ItemDeck : MonoBehaviour
{
    public List<ItemData> cards = new List<ItemData>();

    [Header("UI")]
    public Button pileButton;
    public Text countText;
    public Text promptText;
    public ItemCard itemCardPrefab;
    public RectTransform drawArea;  // Where the drawn card is shown (the canvas).

    [Header("Animation")]
    public float flyTime = 0.5f;
    public float endScale = 0.1f;

    readonly List<ItemData> pile = new List<ItemData>();
    ItemCard drawnCard;
    bool flying;
    string defaultPrompt;

    void Awake()
    {
        if (promptText) defaultPrompt = promptText.text;
        if (pileButton) pileButton.onClick.AddListener(Draw);
    }

    void Start()
    {
        // Each card goes into the pile as many times as its 总量 says.
        foreach (var card in cards)
            if (card) for (int c = 0; c < card.copiesInPile; c++) pile.Add(card);
        for (int i = pile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pile[i], pile[j]) = (pile[j], pile[i]);
        }
        if (promptText) promptText.gameObject.SetActive(false);
        UpdateCount();
    }

    // Takes the top consumable (消耗品) out of the pile, e.g. for 行脚商人's starting items. Null if none is left.
    public ItemData TakeConsumable()
    {
        for (int i = pile.Count - 1; i >= 0; i--)
        {
            if (!pile[i] || pile[i].kind != ItemKind.Consumable) continue;
            var item = pile[i];
            pile.RemoveAt(i);
            UpdateCount();
            return item;
        }
        return null;
    }

    // Takes one copy of this card out of the pile. Null if none is left.
    public ItemData Take(ItemData card)
    {
        int i = pile.LastIndexOf(card);
        if (i < 0) return null;
        pile.RemoveAt(i);
        UpdateCount();
        return card;
    }

    public void Draw()
    {
        // A drawn card nobody may take (e.g. 专属 to a class no one plays): clicking the pile discards it.
        if (drawnCard && !flying && !AnyoneCanTake(drawnCard.item))
        {
            Destroy(drawnCard.gameObject);
            drawnCard = null;
            if (promptText) promptText.gameObject.SetActive(false);
            return;
        }
        if (drawnCard || flying || pile.Count == 0) return;

        var item = pile[pile.Count - 1];
        pile.RemoveAt(pile.Count - 1);
        UpdateCount();

        drawnCard = Instantiate(itemCardPrefab, drawArea);
        drawnCard.SetItem(item);
        var rt = (RectTransform)drawnCard.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 170f);
        drawnCard.transform.SetAsLastSibling();

        if (promptText)
        {
            promptText.text = AnyoneCanTake(item) ? defaultPrompt
                : "没有玩家能拿这张牌（" + (item.exclusiveTo ? item.itemName + " 是" + item.exclusiveTo.className + "专属" : "大家的道具栏都满了")
                  + "），点击道具牌堆弃掉";
            promptText.gameObject.SetActive(true);
            promptText.transform.SetAsLastSibling();
        }
    }

    // Draws a card and sends it straight to this player. If they can't take it, it waits for a click
    // like a normal draw. Returns false if no card could be drawn (one is already waiting, or the pile is empty).
    public bool DrawFor(CharacterCard character)
    {
        if (drawnCard || flying || pile.Count == 0) return false;
        Draw();
        if (drawnCard && character.CanHold(drawnCard.item) && character.HasFreeItemSlot) StartCoroutine(FlyTo(character));
        return true;
    }

    // Someone at the table has a free slot and may hold this card (专属 cards only go to that class).
    static bool AnyoneCanTake(ItemData item)
    {
        foreach (var c in FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude))
            if (c.HasFreeItemSlot && c.CanHold(item)) return true;
        return false;
    }

    // Called when a character card is clicked.
    public void GiveTo(CharacterCard character)
    {
        if (!drawnCard || flying) return;
        var item = drawnCard.item;
        if (!character.CanHold(item) && item.exclusiveTo)
        {
            // 专属: only a player of that class may have it.
            if (promptText) promptText.text = item.itemName + " 是" + item.exclusiveTo.className + "专属，只能给" + item.exclusiveTo.className;
            return;
        }
        if (!character.HasFreeItemSlot)
        {
            // Full: the card stays out so it can go to someone else.
            if (promptText) promptText.text = character.playerName + " 的背包满了（道具和负伤共 " + CharacterCard.ItemSlots + " 格），请选别的玩家";
            return;
        }
        StartCoroutine(FlyTo(character));
    }

    IEnumerator FlyTo(CharacterCard character)
    {
        flying = true;
        if (promptText) promptText.gameObject.SetActive(false);

        var card = drawnCard.transform;
        Vector3 startPos = card.position;
        Vector3 startScale = card.localScale;
        for (float t = 0f; t < flyTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / flyTime);
            card.position = Vector3.Lerp(startPos, character.transform.position, k);
            card.localScale = Vector3.Lerp(startScale, startScale * endScale, k);
            yield return null;
        }

        character.TryAddItem(drawnCard.item);
        Destroy(drawnCard.gameObject);
        drawnCard = null;
        flying = false;
    }

    void UpdateCount()
    {
        if (countText) countText.text = pile.Count > 0 ? pile.Count + " left" : "Empty";
    }
}
