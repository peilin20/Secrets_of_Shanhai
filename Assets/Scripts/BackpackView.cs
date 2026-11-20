using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The bottom row of the combat / social window shows one player's backpack: items, equipment and 负伤 cards.
// Name buttons next to the 背包 label switch whose backpack is shown (picking who goes on stage does too).
public static class BackpackView
{
    // Name buttons right of the "背包" label (made the first time). `onPick` gets the player clicked.
    public static RectTransform NameButtons(RectTransform existing, RectTransform itemRow, Button template,
                                            List<CharacterCard> players, CharacterCard viewed, System.Action<CharacterCard> onPick)
    {
        var row = existing;
        var label = itemRow.parent.Find("ItemsLabel") as RectTransform;
        if (!row)
        {
            var go = new GameObject("BackpackSwitch", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row = (RectTransform)go.transform;
            row.SetParent(itemRow.parent, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0f, 0.5f);
            // Just right of the label's text.
            Vector2 at = label
                ? label.anchoredPosition + new Vector2(-label.rect.width * label.pivot.x + 80f, 0f)
                : itemRow.anchoredPosition + new Vector2(-itemRow.rect.width * 0.5f + 90f, 140f);
            row.anchoredPosition = at;
            row.sizeDelta = new Vector2(420f, 36f);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        }
        if (label)
        {
            // The label's own LocalizedText (old key) would put the old text back on a language change.
            var loc = label.GetComponent<LocalizedText>();
            if (loc) loc.key = "bag.label";
            var t = label.GetComponent<Text>();
            if (t) t.text = L.T("bag.label");
        }
        foreach (Transform child in row) Object.Destroy(child.gameObject);
        foreach (var p in players)
        {
            var b = Object.Instantiate(template, row);
            b.gameObject.SetActive(true);
            b.interactable = true;
            ((RectTransform)b.transform).sizeDelta = new Vector2(100f, 34f);
            b.GetComponent<Image>().color = p == viewed ? new Color(0.95f, 0.75f, 0.35f) : new Color(0.88f, 0.82f, 0.68f);
            var text = b.GetComponentInChildren<Text>();
            text.text = p.playerName;
            text.fontSize = Mathf.Min(text.fontSize, 16);
            var who = p;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => onPick(who));
        }
        return row;
    }

    // A 负伤 card in the row: shown only; hovering tells what it does.
    public static void AddInjury(RectTransform itemRow, RectTransform template, ItemCard prefab, float scale, InjuryData injury, Text detail)
    {
        var entry = Object.Instantiate(template, itemRow);
        entry.gameObject.SetActive(true);
        var card = Object.Instantiate(prefab, entry.Find("CardHolder"));
        card.SetInjury(injury);
        card.draggable = false;
        CombatPanel.Place(card.transform, scale);
        entry.Find("Owner").GetComponent<Text>().text = L.T("bag.injury");
        entry.Find("UseButton").gameObject.SetActive(false);
        var hover = entry.gameObject.AddComponent<SetupHover>();
        hover.onHover = () => { if (detail) detail.text = L.T("items.nameDetail", injury.Title, injury.effect); };
    }
}
