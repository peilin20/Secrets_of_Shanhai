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
    public float bigTokenSize = 130f;   // Size of the token shown with the 占卜 / 随机 result.

    static readonly Color PlainButton = new Color(0.93f, 0.89f, 0.8f);

    int picked = -1;
    string[] lastInputs = new string[0];

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
            player.injuries.Clear();

            // 0. The player's name (left empty: keeps the current one).
            yield return Choose(player.playerName + "：输入你的名字", new List<string> { "确定" },
                i => PlainButton, i => "", new[] { player.playerName }, false);
            if (lastInputs.Length > 0 && !string.IsNullOrWhiteSpace(lastInputs[0])) player.playerName = lastInputs[0].Trim();
            player.Refresh();

            // 1. Primary element: 2 tokens of it and 1 of a random other element.
            var elements = (Element[])System.Enum.GetValues(typeof(Element));
            var labels = new List<string>();
            foreach (var e in elements) labels.Add(CombatCard.ElementChar(e));
            // An empty cell so 占卜 and 随机 start on their own row, under the element buttons.
            while (labels.Count % 3 != 0) labels.Add(null);
            int divineIndex = labels.Count, randomIndex = labels.Count + 1;
            labels.Add("占卜");
            labels.Add("随机");
            Element primary = elements[0];
            string result = "";
            bool decided = false;
            while (!decided)
            {
                yield return Choose(player.playerName + "：选择主属性", labels,
                    i => i < elements.Length ? ElementToken.ColorFor(elements[i]) : PlainButton,
                    i => i == divineIndex ? "输入出生日期（公历），以日主五行为主属性"
                       : i == randomIndex ? "随机选一个五行" : "");
                if (picked < elements.Length)
                {
                    primary = elements[picked];
                    decided = true;
                }
                else if (picked == randomIndex)
                {
                    primary = elements[Random.Range(0, elements.Length)];
                    result = "你的主元素为：";
                    decided = true;
                }
                else
                {
                    // 占卜: ask for the birth date until it is a real date, or go back.
                    string prompt = player.playerName + "：输入出生日期（公历）";
                    while (true)
                    {
                        yield return Choose(prompt, new List<string> { "确定", "返回" },
                            i => PlainButton, i => "", new[] { "年", "月", "日" });
                        if (picked == 1) break;
                        if (Divination.TryParse(lastInputs[0], lastInputs[1], lastInputs[2], out var birth))
                        {
                            primary = Divination.DayMaster(birth);
                            result = "占卜结果为：";
                            decided = true;
                            break;
                        }
                        prompt = player.playerName + "：这不是一个有效的日期，请重新输入出生年、月、日";
                    }
                }
            }
            if (result != "")
            {
                // Two empty rows for the token, then 继续 under it.
                var bigToken = ShowBigToken(primary);
                yield return Choose(player.playerName + "：" + result + CombatCard.ElementChar(primary) + "元素",
                    new List<string> { null, null, null, null, null, null, "继续" }, i => PlainButton, i => "");
                Destroy(bigToken);
            }
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
            yield return GiveBackgroundStart(player, player.background);

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
    // A null label leaves that grid cell empty.
    // Each of `inputPlaceholders` adds a text box before the buttons; their texts end up in `lastInputs`.
    IEnumerator Choose(string title, List<string> labels, System.Func<int, Color> color, System.Func<int, string> detail,
        string[] inputPlaceholders = null, bool numbersOnly = true)
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

        var inputs = new List<InputField>();
        if (inputPlaceholders != null)
            foreach (var p in inputPlaceholders) inputs.Add(MakeInput(p, numbersOnly));
        if (inputs.Count > 0) inputs[0].ActivateInputField();

        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] == null)
            {
                var spacer = new GameObject("Spacer", typeof(RectTransform));
                spacer.transform.SetParent(choiceRow, false);
                continue;
            }
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
        lastInputs = new string[inputs.Count];
        for (int i = 0; i < inputs.Count; i++) lastInputs[i] = inputs[i].text;
    }

    // A large token of this element, centered over the first column of the choice grid's top two rows.
    GameObject ShowBigToken(Element e)
    {
        var go = new GameObject("BigToken", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(choiceRow.parent, false);
        rt.anchorMin = choiceRow.anchorMin;
        rt.anchorMax = choiceRow.anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.one * bigTokenSize;
        var grid = choiceRow.GetComponent<GridLayoutGroup>();
        Vector2 topLeft = choiceRow.anchoredPosition
            + new Vector2(-choiceRow.rect.width * choiceRow.pivot.x, choiceRow.rect.height * (1f - choiceRow.pivot.y));
        rt.anchoredPosition = topLeft + new Vector2(grid.cellSize.x * 0.5f, -(grid.cellSize.y + grid.spacing.y * 0.5f));
        var image = go.GetComponent<Image>();
        if (tokenPrefab) image.sprite = tokenPrefab.GetComponent<Image>().sprite;
        image.preserveAspect = true;
        image.color = ElementToken.ColorFor(e);
        image.raycastTarget = false;
        return go;
    }

    // A number box in the choice row, using the buttons' font (the grid gives it the button size).
    InputField MakeInput(string placeholder, bool numbersOnly)
    {
        var templateText = choiceTemplate.GetComponentInChildren<Text>();
        var go = new GameObject("BirthInput", typeof(RectTransform), typeof(Image), typeof(InputField));
        var rt = (RectTransform)go.transform;
        rt.SetParent(choiceRow, false);
        go.GetComponent<Image>().color = Color.white;

        Text MakeText(string name, Color c)
        {
            var t = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            var trt = (RectTransform)t.transform;
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10, 4);
            trt.offsetMax = new Vector2(-10, -4);
            t.font = templateText.font;
            t.fontSize = templateText.fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = c;
            t.supportRichText = false;
            return t;
        }

        var field = go.GetComponent<InputField>();
        var hint = MakeText("Placeholder", new Color(0.5f, 0.5f, 0.5f));
        hint.text = placeholder;
        field.placeholder = hint;
        field.textComponent = MakeText("Text", Color.black);
        field.contentType = numbersOnly ? InputField.ContentType.IntegerNumber : InputField.ContentType.Standard;
        if (!numbersOnly) field.characterLimit = 12;
        return field;
    }

    // What the background card gives at the start: its items, random 消耗品 from the item pile, a 负伤.
    // The new cards are shown with a "获得…" step.
    IEnumerator GiveBackgroundStart(CharacterCard player, BackgroundData bg)
    {
        if (!bg) yield break;
        var got = new List<ItemData>();
        foreach (var item in bg.startItems)
            if (item && player.TryAddItem(item)) got.Add(item);
        if (bg.pileItems.Count > 0)
        {
            var pile = FindFirstObjectByType<ItemDeck>();
            foreach (var wanted in bg.pileItems)
            {
                var item = pile ? pile.Take(wanted) : null;
                if (item && player.TryAddItem(item)) got.Add(item);
            }
        }
        if (bg.randomConsumables > 0)
        {
            var deck = FindFirstObjectByType<ItemDeck>();
            for (int i = 0; i < bg.randomConsumables && deck; i++)
            {
                var item = deck.TakeConsumable();
                if (item && player.TryAddItem(item)) got.Add(item);
            }
        }
        player.Refresh();
        if (got.Count > 0)
        {
            ShowWeapons(got);
            var names = new StringBuilder();
            for (int i = 0; i < got.Count; i++) names.Append(i == 0 ? "" : i == got.Count - 1 ? " 和 " : "、").Append(got[i].itemName);
            string kind = got.TrueForAll(x => x.kind == ItemKind.Consumable) ? "消耗品" : "道具";
            yield return Choose(player.playerName + "（" + bg.backgroundName + "）：获得" + kind + " " + names, new List<string> { "继续" },
                i => PlainButton, i => "");
            ShowWeapons(null);
        }
        if (bg.startInjury)
        {
            var injuries = InjuryDeck.Find();
            if (injuries) yield return injuries.Give(player, bg.startInjury, false, null);
            else { player.injuries.Add(bg.startInjury); player.Refresh(); }
            yield return Choose(player.playerName + "（" + bg.backgroundName + "）：开局携带" + bg.startInjury.Title + "：" + bg.startInjury.effect,
                new List<string> { "继续" }, i => PlainButton, i => "");
        }
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
