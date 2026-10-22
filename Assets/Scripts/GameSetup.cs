using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Start-of-game setup. One player at a time, their character card moves to the middle of a dimmed screen:
// 1. Choose a primary element: get 2 tokens of it and 1 token of a random other element.
// 2. Choose one of the backgrounds.
// 3. Choose a class: get that class's weapon cards.
// Then the card goes back to its place and the next player sets up.
public class GameSetup : MonoBehaviour
{
    public bool runOnStart = true;
    public List<BackgroundData> backgrounds = new List<BackgroundData>();
    public List<ClassData> classes = new List<ClassData>();
    public ElementToken tokenPrefab;

    [Header("UI")]
    public GameObject screen;            // The dimmed setup screen, shown only during setup.
    public RectTransform cardHolder;     // Where the card sits while its player sets up.
    public Text stepText;
    public RectTransform choiceRow;
    public Button choiceTemplate;        // Inactive; copied once per choice.
    public Text detailText;
    public RectTransform weaponRow;      // Shows the weapon cards a player just got (scale the row to size them).
    public ItemCard itemCardPrefab;
    public float cardScale = 0.9f;

    static readonly Color PlainButton = new Color(0.93f, 0.89f, 0.8f);

    int picked = -1;

    void Start()
    {
        screen.SetActive(false);
        if (runOnStart) StartCoroutine(RunSetup());
    }

    IEnumerator RunSetup()
    {
        var players = new List<CharacterCard>(FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude));
        players.Sort((a, b) => string.Compare(a.playerName, b.playerName, System.StringComparison.Ordinal));
        screen.SetActive(true);
        screen.transform.SetAsLastSibling();

        // Nobody has a primary element until they pick one.
        foreach (var p in players)
        {
            p.elementChosen = false;
            p.Refresh();
        }

        foreach (var player in players)
        {
            // Bring the card to the middle.
            var card = player.transform;
            var oldParent = card.parent;
            int oldIndex = card.GetSiblingIndex();
            Vector3 oldPosition = card.position;
            Vector3 oldScale = card.localScale;
            card.SetParent(cardHolder, false);
            ((RectTransform)card).anchoredPosition = Vector2.zero;
            card.localScale = Vector3.one * cardScale;

            // Start from a clean card.
            player.items.Clear();
            player.EnsureSlots();
            player.equippedSlot = -1;
            if (player.tokenArea) foreach (Transform t in player.tokenArea) Destroy(t.gameObject);

            // 1. Primary element: 2 tokens of it and 1 of a random other element.
            var elements = (Element[])System.Enum.GetValues(typeof(Element));
            var labels = new List<string>();
            foreach (var e in elements) labels.Add(CombatCard.ElementChar(e));
            yield return Choose(player.playerName + "：选择主属性", labels,
                i => ElementToken.ColorFor(elements[i]), i => "");
            var primary = elements[picked];
            player.primaryElement = primary;
            player.elementChosen = true;
            var others = new List<Element>(elements);
            others.Remove(primary);
            var bonus = others[Random.Range(0, others.Count)];
            GiveTokens(player, new[] { primary, primary, bonus });
            player.Refresh();
            yield return Choose(player.playerName + "：获得了 2 个" + CombatCard.ElementChar(primary) + " token 和 1 个"
                + CombatCard.ElementChar(bonus) + " token", new List<string> { "继续" }, i => PlainButton, i => "");

            // 2. Background.
            labels.Clear();
            foreach (var b in backgrounds) labels.Add(b.backgroundName);
            yield return Choose(player.playerName + "：选择背景", labels,
                i => PlainButton, i => backgrounds[i].description);
            player.background = backgrounds[picked];
            player.Refresh();

            // 3. Class, and its weapon cards.
            labels.Clear();
            foreach (var c in classes) labels.Add(c.className);
            yield return Choose(player.playerName + "：选择职业", labels,
                i => PlainButton, i => classes[i].description + "\n\n" + WeaponList(classes[i]));
            var chosen = classes[picked];
            player.characterClass = chosen;
            var slots = new List<int>();
            foreach (var weapon in chosen.weapons)
            {
                player.TryAddItem(weapon);
                slots.Add(player.items.IndexOf(weapon));
            }
            player.Refresh();

            // 4. Show the new weapon cards and let the player equip one (or none).
            ShowWeapons(chosen.weapons);
            labels.Clear();
            foreach (var weapon in chosen.weapons) labels.Add("装备 " + weapon.itemName);
            labels.Add("暂不装备");
            yield return Choose(player.playerName + "：获得武器卡 " + WeaponNames(chosen) + "，要装备哪一张？", labels,
                i => PlainButton, i => "");
            if (picked < slots.Count) player.ToggleEquip(slots[picked]);
            ShowWeapons(null);

            // Back to its place on the table.
            card.SetParent(oldParent, true);
            card.SetSiblingIndex(oldIndex);
            card.localScale = oldScale;
            card.position = oldPosition;
        }

        screen.SetActive(false);
    }

    // Shows the choices as buttons and waits until one is clicked (the index ends up in `picked`).
    IEnumerator Choose(string title, List<string> labels, System.Func<int, Color> color, System.Func<int, string> detail)
    {
        picked = -1;
        stepText.text = title;
        detailText.text = "";
        // Hide the previous step's buttons right away (Destroy only happens at the end of the frame).
        foreach (Transform child in choiceRow)
        {
            if (child == choiceTemplate.transform) continue;
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        for (int i = 0; i < labels.Count; i++)
        {
            var button = Instantiate(choiceTemplate, choiceRow);
            button.gameObject.SetActive(true);
            button.GetComponent<Image>().color = color(i);
            button.GetComponentInChildren<Text>().text = labels[i];
            int index = i;
            button.onClick.AddListener(() => picked = index);
            var hover = button.gameObject.AddComponent<SetupHover>();
            hover.onHover = () => detailText.text = detail(index);
        }
        while (picked < 0) yield return null;
    }

    void GiveTokens(CharacterCard player, Element[] elements)
    {
        if (!player.tokenArea || !tokenPrefab) return;
        Canvas.ForceUpdateCanvases();
        float width = player.tokenArea.rect.width;
        for (int i = 0; i < elements.Length; i++)
        {
            var token = Instantiate(tokenPrefab, player.tokenArea);
            token.SetElement(elements[i]);
            float x = (i + 1) * width / (elements.Length + 1) - width * 0.5f;
            ((RectTransform)token.transform).anchoredPosition = token.ClampToParent(new Vector2(x, 0f));
        }
    }

    // Shows these weapon cards under the choices (null clears them).
    void ShowWeapons(List<ItemData> weapons)
    {
        foreach (Transform child in weaponRow)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        if (weapons == null) return;
        foreach (var weapon in weapons)
        {
            var card = Instantiate(itemCardPrefab, weaponRow);
            card.SetItem(weapon);
            card.draggable = false;
        }
    }

    static string WeaponNames(ClassData c)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < c.weapons.Count; i++)
        {
            if (i > 0) sb.Append("、");
            sb.Append(c.weapons[i] ? c.weapons[i].itemName : "?");
        }
        return sb.ToString();
    }

    static string WeaponList(ClassData c)
    {
        return "武器卡：" + WeaponNames(c);
    }
}
