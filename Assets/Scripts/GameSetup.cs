using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Character creation (its own scene). One player at a time:
// 0. Type a name (in the middle of the screen, no card yet).
// 1. Pick a species from the portraits (hover = full colour; click = its description and a confirm).
// 2. The character card appears on the left with that species. Pick a primary element
//    (its token comes when the game starts and the first main story card is dealt).
// 3. Pick a background (its starting items / 负伤 are given).
// 4. Pick a class, get its weapon cards, equip one (or none).
// The 随机角色 button (bottom right) makes the current player at random instead.
// All text follows the language setting, and switches right away when it changes.
// When every player is done, the choices go to Party and the game scene loads.
public class GameSetup : MonoBehaviour
{
    [System.Serializable]
    public class SpeciesOption
    {
        public Species species;
        public string displayName;
        public Sprite portrait;
        [TextArea(3, 10)] public string description;
        [TextArea(3, 10)] public string descriptionEnglish;
        public string Description => L.Current == Language.English && !string.IsNullOrEmpty(descriptionEnglish) ? descriptionEnglish : description;
    }

    public bool runOnStart = true;
    public int playerCount = 3;
    public string nextScene = "SampleScene";
    public List<SpeciesOption> speciesOptions = new List<SpeciesOption>();
    public List<BackgroundData> backgrounds = new List<BackgroundData>();
    public List<ClassData> classes = new List<ClassData>();
    public List<ItemData> pileCards = new List<ItemData>();   // The game's item pile, for backgrounds that take cards from it.
    public ElementToken tokenPrefab;

    [Header("UI")]
    public GameObject screen;            // The setup screen.
    public CharacterCard displayCard;    // The card that shows the player being made (left side).
    public RectTransform cardHolder;     // Where the card sits.
    // The screen's layouts are objects in the scene (edit them in the Scene window); the game only fills them in.
    public SetupLayout layoutNoCard;     // Name and age: centred, no character card.
    public SetupLayout layoutWithCard;   // Element, background, class…: with the card on the left.
    public SpeciesPage speciesPage;      // The species step.
    public Button randomCharacterButton; // 随机角色, bottom right.
    public Button choiceTemplate;        // Inactive; copied once per choice (the layout's grid sets the size).
    public RectTransform weaponRow;      // Shows the weapon cards a player just got (scale the row to size them).
    public ItemCard itemCardPrefab;
    public float cardScale = 0.9f;
    public float bigTokenSize = 130f;  // Size of the token shown with the 占卜 / 随机 result.

    static readonly Color PlainButton = new Color(0.93f, 0.89f, 0.8f);
    static readonly Color Ink = new Color(0.15f, 0.12f, 0.1f);

    int picked = -1;
    string[] lastInputs = new string[0];
    readonly List<ItemData> pile = new List<ItemData>();
    readonly List<ItemData> takenThisPlayer = new List<ItemData>();

    int languageVersion;
    Coroutine playerRoutine;
    bool playerDone;
    int currentPlayer;
    bool nameEntered;
    int currentAge = -1;   // -1: not entered (e.g. a random character).
    SetupLayout layout;   // The layout in use.
    Text stepText => layout.stepText;
    RectTransform choiceRow => layout.choiceRow;
    Text detailText => layout.detailText;
    GameObject bigToken;
    Button randomButton;

    void OnEnable() { L.Changed += OnLanguageChanged; }
    void OnDisable() { L.Changed -= OnLanguageChanged; }
    void OnLanguageChanged() { languageVersion++; }

    void Awake()
    {
        layout = layoutNoCard;
        if (speciesPage) speciesPage.gameObject.SetActive(false);
        if (randomCharacterButton) randomCharacterButton.gameObject.SetActive(false);
    }

    void Start()
    {
        if (runOnStart) StartCoroutine(RunSetup());
        else screen.SetActive(false);
    }

    IEnumerator RunSetup()
    {
        screen.SetActive(true);
        screen.transform.SetAsLastSibling();
        BuildPile();
        Party.Clear();
        MakeRandomButton();

        var card = displayCard.transform;
        card.SetParent(cardHolder, false);
        ((RectTransform)card).anchoredPosition = Vector2.zero;
        card.localScale = Vector3.one * cardScale;

        for (int n = 0; n < playerCount; n++)
        {
            currentPlayer = n;
            nameEntered = false;
            currentAge = -1;
            displayCard.playerName = L.T("setup.defaultName", n + 1);
            playerDone = false;
            playerRoutine = StartCoroutine(CreatePlayer());
            while (!playerDone) yield return null;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
    }

    // A clean card for the current player (the name they typed stays).
    void ResetPlayer()
    {
        var player = displayCard;
        // Cards a background took from the pile for this player go back.
        foreach (var item in takenThisPlayer)
        {
            pile.Add(item);
            Party.takenFromPile.Remove(item);
        }
        takenThisPlayer.Clear();
        player.items.Clear();
        player.EnsureSlots();
        player.equippedSlot = -1;
        if (player.tokenArea) foreach (Transform t in player.tokenArea) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
        player.injuries.Clear();
        player.background = null;
        player.characterClass = null;
        player.elementChosen = false;
        player.Refresh();
        ClearStepUI();
        ShowCard(false);
    }

    // Removes whatever the interrupted step had on screen.
    void ClearStepUI()
    {
        if (speciesPage && speciesPage.gameObject.activeSelf)
        {
            speciesPage.gameObject.SetActive(false);
            layout.gameObject.SetActive(true);
        }
        if (bigToken) Destroy(bigToken);
        ShowWeapons(null);
        Clear(choiceRow, choiceTemplate.transform);
        detailText.text = "";
    }

    IEnumerator CreatePlayer()
    {
        var player = displayCard;
        ResetPlayer();

        // 0. The player's name (left empty: keeps the default).
        int number = currentPlayer + 1;
        yield return Choose(() => L.T("setup.enterName"), () => new List<string> { L.T("setup.ok") },
            i => PlainButton, i => "", () => new[] { L.T("setup.defaultName", number) }, false);
        player.playerName = lastInputs.Length > 0 && !string.IsNullOrWhiteSpace(lastInputs[0])
            ? lastInputs[0].Trim() : L.T("setup.defaultName", number);
        nameEntered = true;

        // 0b. Age (any whole number; 占卜 works out the birth year from it).
        string ageKey = "setup.enterAge";
        while (true)
        {
            yield return Choose(() => L.T(ageKey, player.playerName), () => new List<string> { L.T("setup.ok") },
                i => PlainButton, i => "", () => new[] { L.T("setup.age") });
            if (int.TryParse(lastInputs.Length > 0 ? lastInputs[0] : "", out int age) && age >= 0 && age < System.DateTime.Today.Year)
            {
                currentAge = age;
                break;
            }
            ageKey = "setup.badAge";
        }

        // 1. Species.
        yield return ChooseSpecies(player);
        player.Refresh();
        ShowCard(true);

        // 2. Primary element.
        var elements = (Element[])System.Enum.GetValues(typeof(Element));
        int divineIndex = 6, randomIndex = 7;   // 5 elements, an empty cell, then 占卜 and 随机 on their own row.
        System.Func<List<string>> elementLabels = () =>
        {
            var labels = new List<string>();
            foreach (var e in elements) labels.Add(L.ElementName(e));
            labels.Add(null);
            labels.Add(L.T("setup.divine"));
            labels.Add(L.T("setup.random"));
            return labels;
        };
        Element primary = elements[0];
        string resultKey = null;
        bool decided = false;
        while (!decided)
        {
            yield return Choose(() => L.T("setup.chooseElement", player.playerName), elementLabels,
                i => i < elements.Length ? ElementToken.ColorFor(elements[i]) : PlainButton,
                i => i == divineIndex ? L.T("setup.divineHint") : i == randomIndex ? L.T("setup.randomHint") : "");
            if (picked < elements.Length)
            {
                primary = elements[picked];
                decided = true;
            }
            else if (picked == randomIndex)
            {
                primary = elements[Random.Range(0, elements.Length)];
                resultKey = "setup.randomResult";
                decided = true;
            }
            else
            {
                // 占卜: ask for the birth date until it is a real date, or go back.
                string promptKey = "setup.birthPrompt";
                while (true)
                {
                    yield return Choose(() => L.T(promptKey, player.playerName), () => new List<string> { L.T("setup.ok"), L.T("setup.back") },
                        i => PlainButton, i => "", () => new[] { L.T("setup.month"), L.T("setup.day") });
                    if (picked == 1) break;
                    if (Divination.TryParseWithAge(currentAge, lastInputs[0], lastInputs[1], System.DateTime.Today, out var birth))
                    {
                        primary = Divination.DayMaster(birth);
                        resultKey = "setup.divineResult";
                        decided = true;
                        break;
                    }
                    promptKey = "setup.badDate";
                }
            }
        }
        if (resultKey != null)
        {
            // Two empty rows for the token, then 继续 under it.
            bigToken = ShowBigToken(primary);
            yield return Choose(() => L.T(resultKey, player.playerName, L.ElementName(primary)),
                () => new List<string> { null, null, null, null, null, null, L.T("setup.continue") }, i => PlainButton, i => "");
            Destroy(bigToken);
        }
        var tokens = GivePrimary(player, primary);

        // 3. Background.
        yield return Choose(() => L.T("setup.chooseBackground", player.playerName), BackgroundLabels,
            i => PlainButton, i => backgrounds[i].description);
        player.background = backgrounds[picked];
        player.Refresh();
        yield return GiveBackgroundStart(player, player.background, false);

        // 4. Class, and its weapon cards.
        yield return Choose(() => L.T("setup.chooseClass", player.playerName), ClassLabels,
            i => PlainButton, i => classes[i].description + "\n\n" + L.T("setup.weapons", WeaponNames(classes[i])));
        var chosen = classes[picked];
        var slots = GiveClass(player, chosen);

        // 5. Show the new weapon cards and let the player equip one (or none).
        ShowWeapons(chosen.weapons);
        yield return Choose(() => L.T("setup.gotWeapons", player.playerName, WeaponNames(chosen)),
            () =>
            {
                var labels = new List<string>();
                foreach (var weapon in chosen.weapons) labels.Add(L.T("setup.equip", weapon.itemName));
                labels.Add(L.T("setup.noEquip"));
                return labels;
            },
            i => PlainButton, i => "");
        if (picked < slots.Count) player.ToggleEquip(slots[picked]);
        ShowWeapons(null);

        SavePlayer(tokens);
    }

    // 随机角色: the rest of this player is picked at random, then shown with 继续.
    IEnumerator RandomPlayer()
    {
        var player = displayCard;
        ResetPlayer();
        var option = speciesOptions[Random.Range(0, speciesOptions.Count)];
        player.species = option.species;
        player.portrait = option.portrait;
        ShowCard(true);   // Tokens need the card active to be placed.
        var elements = (Element[])System.Enum.GetValues(typeof(Element));
        var tokens = GivePrimary(player, elements[Random.Range(0, elements.Length)]);
        player.background = backgrounds[Random.Range(0, backgrounds.Count)];
        yield return GiveBackgroundStart(player, player.background, true);
        var slots = GiveClass(player, classes[Random.Range(0, classes.Count)]);
        if (slots.Count > 0 && slots[0] >= 0) player.ToggleEquip(slots[0]);
        player.Refresh();
        yield return Choose(() => L.T("setup.randomDone", player.playerName), () => new List<string> { L.T("setup.continue") },
            i => PlainButton, i => "");
        SavePlayer(tokens);
    }

    void OnRandomClicked()
    {
        if (playerDone) return;
        if (playerRoutine != null) StopCoroutine(playerRoutine);
        if (!nameEntered) displayCard.playerName = L.T("setup.defaultName", currentPlayer + 1);
        playerRoutine = StartCoroutine(RandomPlayer());
    }

    // Sets the primary element. No tokens here: each player gets 1 primary token when a main story card
    // is turned, the first one as the game starts. (Returns the starting tokens, none.)
    List<Element> GivePrimary(CharacterCard player, Element primary)
    {
        player.primaryElement = primary;
        player.elementChosen = true;
        player.Refresh();
        return new List<Element>();
    }

    // The class and its weapon cards; returns the slots they went into.
    List<int> GiveClass(CharacterCard player, ClassData chosen)
    {
        player.characterClass = chosen;
        var slots = new List<int>();
        foreach (var weapon in chosen.weapons)
        {
            player.TryAddItem(weapon);
            slots.Add(player.items.IndexOf(weapon));
        }
        player.Refresh();
        return slots;
    }

    void SavePlayer(List<Element> tokens)
    {
        var player = displayCard;
        Party.players.Add(new PlayerSetup
        {
            name = player.playerName,
            age = currentAge,
            species = player.species,
            portrait = player.portrait,
            primaryElement = player.primaryElement,
            tokens = tokens,
            background = player.background,
            characterClass = player.characterClass,
            items = new List<ItemData>(player.items),
            equippedSlot = player.equippedSlot,
            injuries = new List<InjuryData>(player.injuries),
        });
        takenThisPlayer.Clear();
        playerDone = true;
    }

    List<string> BackgroundLabels()
    {
        var labels = new List<string>();
        foreach (var b in backgrounds) labels.Add(b.backgroundName);
        return labels;
    }

    List<string> ClassLabels()
    {
        var labels = new List<string>();
        foreach (var c in classes) labels.Add(c.className);
        return labels;
    }

    // The 随机角色 button in the bottom right corner (placed in the scene).
    void MakeRandomButton()
    {
        var button = randomCharacterButton;
        if (!button) return;
        button.gameObject.SetActive(true);
        var label = button.GetComponentInChildren<Text>();
        if (label)
        {
            label.text = L.T("setup.randomCharacter");
            var loc = label.GetComponent<LocalizedText>();
            if (!loc) loc = label.gameObject.AddComponent<LocalizedText>();
            loc.key = "setup.randomCharacter";
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnRandomClicked);
        randomButton = button;
    }

    // The card is hidden for the name and species steps, which use the centred layout; the rest use the layout
    // with the card. Both layouts are in the scene.
    void ShowCard(bool show)
    {
        cardHolder.gameObject.SetActive(show);
        var next = show ? layoutWithCard : layoutNoCard;
        if (layout != next)
        {
            Clear(layout.choiceRow, choiceTemplate.transform);
            layout.stepText.text = "";
            layout.detailText.text = "";
        }
        layoutNoCard.gameObject.SetActive(!show);
        layoutWithCard.gameObject.SetActive(show);
        layout = next;
    }

    // The four species as portraits: grey until the mouse is over one (its description shows under the row);
    // clicking one picks it (it stays in colour) and shows a confirm button. The page is laid out in the scene.
    IEnumerator ChooseSpecies(CharacterCard player)
    {
        ShowCard(false);
        Clear(choiceRow, choiceTemplate.transform);
        detailText.text = "";
        layout.gameObject.SetActive(false);
        var page = speciesPage;
        page.gameObject.SetActive(true);
        page.transform.SetAsLastSibling();

        bool confirmed = false;
        page.confirmButton.onClick.RemoveAllListeners();
        page.confirmButton.onClick.AddListener(() => confirmed = true);

        int selected = -1, hovered = -1;
        int count = Mathf.Min(page.entries.Count, speciesOptions.Count);
        for (int i = 0; i < page.entries.Count; i++)
        {
            var entry = page.entries[i];
            entry.button.gameObject.SetActive(i < count);
            if (i >= count) continue;
            int index = i;
            if (entry.portrait) entry.portrait.sprite = speciesOptions[i].portrait;
            if (entry.tint)
            {
                if (!entry.tint.target) entry.tint.target = entry.portrait;
                entry.tint.Selected = false;
                entry.tint.onEnter = () => hovered = index;
                entry.tint.onExit = () => { if (hovered == index) hovered = -1; };
            }
            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() =>
            {
                selected = index;
                for (int k = 0; k < count; k++) if (page.entries[k].tint) page.entries[k].tint.Selected = k == index;
            });
        }

        // Refresh the texts every frame: they follow the mouse, the pick and the language.
        while (!confirmed)
        {
            if (page.title) page.title.text = L.T("setup.chooseSpecies", player.playerName);
            if (page.subtitle) page.subtitle.text = L.T("setup.speciesSubtitle");
            for (int i = 0; i < count; i++)
                if (page.entries[i].nameText) page.entries[i].nameText.text = L.SpeciesName(speciesOptions[i].species);
            int shown = hovered >= 0 ? hovered : selected;
            if (page.description)
                page.description.text = shown >= 0
                    ? "<b>" + L.SpeciesName(speciesOptions[shown].species) + "</b>\n" + speciesOptions[shown].Description
                    : L.T("setup.speciesIntro");
            page.confirmButton.gameObject.SetActive(selected >= 0);
            if (selected >= 0 && page.confirmText)
                page.confirmText.text = L.T("setup.pickSpecies", L.SpeciesName(speciesOptions[selected].species));
            yield return null;
        }

        var choice = speciesOptions[selected];
        page.gameObject.SetActive(false);
        layout.gameObject.SetActive(true);
        Clear(choiceRow, choiceTemplate.transform);
        detailText.text = "";
        player.species = choice.species;
        player.portrait = choice.portrait;
    }

    static void Clear(Transform row, Transform keep)
    {
        foreach (Transform child in row)
        {
            if (child == keep) continue;
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    // The item pile as it will be at the start of the game, so backgrounds can take cards from it.
    void BuildPile()
    {
        pile.Clear();
        foreach (var card in pileCards)
            if (card) for (int c = 0; c < card.copiesInPile; c++) pile.Add(card);
        for (int i = pile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pile[i], pile[j]) = (pile[j], pile[i]);
        }
    }

    ItemData TakeFromPile(System.Predicate<ItemData> match)
    {
        for (int i = pile.Count - 1; i >= 0; i--)
        {
            if (!match(pile[i])) continue;
            var item = pile[i];
            pile.RemoveAt(i);
            Party.takenFromPile.Add(item);
            takenThisPlayer.Add(item);
            return item;
        }
        return null;
    }

    // Shows the choices as buttons and waits until one is clicked (the index ends up in `picked`).
    // A null label leaves that grid cell empty. Each of `inputPlaceholders` adds a text box before the buttons;
    // their texts end up in `lastInputs`. The title, labels and placeholders are re-read when the language changes.
    IEnumerator Choose(System.Func<string> title, System.Func<List<string>> labels, System.Func<int, Color> color, System.Func<int, string> detail,
        System.Func<string[]> inputPlaceholders = null, bool numbersOnly = true, System.Func<string> fixedDetail = null)
    {
        picked = -1;
        stepText.text = title();
        detailText.text = fixedDetail != null ? fixedDetail() : "";
        // Hide the previous step's buttons right away (Destroy only happens at the end of the frame).
        Clear(choiceRow, choiceTemplate.transform);

        var inputs = new List<InputField>();
        if (inputPlaceholders != null)
            foreach (var p in inputPlaceholders()) inputs.Add(MakeInput(p, numbersOnly));
        if (inputs.Count > 0) inputs[0].ActivateInputField();

        var list = labels();
        var buttonTexts = new Dictionary<int, Text>();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                var spacer = new GameObject("Spacer", typeof(RectTransform));
                spacer.transform.SetParent(choiceRow, false);
                continue;
            }
            var button = Instantiate(choiceTemplate, choiceRow);
            button.gameObject.SetActive(true);
            button.GetComponent<Image>().color = color(i);
            var text = button.GetComponentInChildren<Text>();
            text.text = list[i];
            buttonTexts[i] = text;
            int index = i;
            button.onClick.AddListener(() => picked = index);
            var hover = button.gameObject.AddComponent<SetupHover>();
            hover.onHover = () => { if (fixedDetail == null) detailText.text = detail(index); };
        }
        int seen = languageVersion;
        while (picked < 0)
        {
            if (seen != languageVersion)
            {
                seen = languageVersion;
                stepText.text = title();
                if (fixedDetail != null) detailText.text = fixedDetail();
                else detailText.text = "";
                list = labels();
                foreach (var kv in buttonTexts) kv.Value.text = list[kv.Key];
                if (inputPlaceholders != null)
                {
                    var hints = inputPlaceholders();
                    for (int i = 0; i < inputs.Count && i < hints.Length; i++) ((Text)inputs[i].placeholder).text = hints[i];
                }
            }
            yield return null;
        }
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

    // What the background card gives at the start: its items, cards from the item pile, a 负伤.
    // Unless `silent`, the new cards are shown with a "获得…" step.
    IEnumerator GiveBackgroundStart(CharacterCard player, BackgroundData bg, bool silent)
    {
        if (!bg) yield break;
        var got = new List<ItemData>();
        foreach (var item in bg.startItems)
            if (item && player.TryAddItem(item)) got.Add(item);
        foreach (var wanted in bg.pileItems)
        {
            var item = TakeFromPile(x => x == wanted);
            if (item && player.TryAddItem(item)) got.Add(item);
        }
        for (int i = 0; i < bg.randomConsumables; i++)
        {
            var item = TakeFromPile(x => x && x.kind == ItemKind.Consumable);
            if (item && player.TryAddItem(item)) got.Add(item);
        }
        player.Refresh();
        if (got.Count > 0 && !silent)
        {
            ShowWeapons(got);
            bool consumables = got.TrueForAll(x => x.kind == ItemKind.Consumable);
            yield return Choose(() =>
                {
                    var names = new StringBuilder();
                    for (int i = 0; i < got.Count; i++)
                        names.Append(i == 0 ? "" : i == got.Count - 1 ? L.T("setup.and") : L.T("setup.listSep")).Append(got[i].itemName);
                    return L.T(consumables ? "setup.gotConsumables" : "setup.gotItems", player.playerName, bg.backgroundName, names.ToString());
                },
                () => new List<string> { L.T("setup.continue") }, i => PlainButton, i => "");
            ShowWeapons(null);
        }
        if (bg.startInjury)
        {
            var injuries = InjuryDeck.Find();
            if (injuries) yield return injuries.Give(player, bg.startInjury, false, null);
            else { player.injuries.Add(bg.startInjury); player.Refresh(); }
            if (!silent)
                yield return Choose(() => L.T("setup.startInjury", player.playerName, bg.backgroundName, bg.startInjury.Title, bg.startInjury.effect),
                    () => new List<string> { L.T("setup.continue") }, i => PlainButton, i => "");
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
            if (i > 0) sb.Append(L.T("setup.listSep"));
            sb.Append(c.weapons[i] ? c.weapons[i].itemName : "?");
        }
        return sb.ToString();
    }

}
