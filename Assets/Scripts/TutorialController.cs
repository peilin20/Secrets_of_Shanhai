using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The tutorial scene. Runs a list of steps over the real game table: everything is dimmed except the part
// the step is about, and a speech bubble says what it is / what to do. A step either waits for 下一步,
// or for the player to click the lit-up thing. 跳过教程 (always shown) goes straight to the game.
// When the steps run out, the game scene loads with the same players.
public class TutorialController : MonoBehaviour
{
    public string gameScene = "SampleScene";
    public Canvas tableCanvas;           // The game table the steps point at.
    public EventData combatEvent;        // 战斗教程: the practice event (大黄狗妖).
    public int combatBonusTokens = 0;    // Extra primary tokens for the practice fight (0: follow the real rules).
    [Tooltip("For testing: 0 = from the start, 1 = jump to the combat part, 2 = jump to the social part.")]
    public int startPart = 0;

    class Step
    {
        public string key;                           // Bubble text (Localization key).
        public System.Func<RectTransform> target;    // What to light up (null: nothing, bubble in the middle).
        public System.Func<List<RectTransform>> targets;   // Or several things at once (lit as one area).
        public System.Func<Button> clickToContinue;  // If set, the player clicks this instead of 下一步.
        public System.Func<bool> doneWhen;           // If set, the step ends when this becomes true (no 下一步).
        public bool free;                            // Don't dim or block anything (for steps with pop-up choices).
        public System.Action onStart;                // Run when the step starts.
    }

    static readonly Color Ink = new Color(0.15f, 0.12f, 0.1f);
    static readonly Color Paper = new Color(0.96f, 0.93f, 0.85f);
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.9f);
    const float Padding = 12f;

    RectTransform overlay;
    readonly Image[] shades = new Image[4];   // Top, bottom, left, right of the lit-up hole.
    readonly Image[] borders = new Image[4];   // Gold frame around the hole.
    RectTransform bubble;
    Text bubbleText;
    Button nextButton;
    Text nextLabel;
    Font font;
    bool advance;
    bool freeStep;   // The current step doesn't block: an unlit bubble goes to the top, out of the way.
    int languageVersion;

    void OnEnable() { L.Changed += OnLanguage; }
    void OnDisable() { L.Changed -= OnLanguage; }
    void OnLanguage() { languageVersion++; }

    IEnumerator Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        yield return null;   // Let the cards load the party and lay themselves out first.
        // As in the game, everyone starts with 1 primary token from the first main story card.
        // (The story card's own deal animation is off in this scene: the tutorial explains it instead.)
        var injuries = InjuryDeck.Find();
        if (injuries && injuries.tokenPrefab)
        {
            Canvas.ForceUpdateCanvases();
            // As many as the first main story card gives (主线牌 sheet: 每位玩家主属性token获取数量).
            var firstStory = FindFirstObjectByType<StoryCard>();
            int n = firstStory && firstStory.story ? firstStory.story.primaryTokens : 1;
            for (int i = 0; i < n; i++)
                yield return TokenFlight.GiveEach(FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude), p => p.primaryElement, injuries.tokenPrefab);
        }
        BuildOverlay();
        if (startPart <= 0)
            foreach (var step in Steps())
                yield return Run(step);
        if (startPart <= 1)
            foreach (var step in CombatSteps())
                yield return Run(step);
        foreach (var step in SocialSteps())
            yield return Run(step);
        Finish();
    }

    // --- The steps: 开局介绍 (more parts to come). ---

    List<Step> Steps()
    {
        var cards = new List<CharacterCard>(FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude));
        cards.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        var card = cards.Count > 0 ? cards[0] : null;
        RectTransform Rt(Component c) => c ? (RectTransform)c.transform : null;
        var story = FindFirstObjectByType<StoryCard>();
        var itemDeck = FindFirstObjectByType<ItemDeck>();
        var eventDeck = FindFirstObjectByType<EventDeck>();
        var fortune = FindFirstObjectByType<FortuneSticks>();
        RectTransform Find(string name) { var t = tableCanvas ? tableCanvas.transform.Find(name) : null; return t as RectTransform; }

        return new List<Step>
        {
            new Step { key = "tut.welcome" },
            new Step { key = "tut.cards", targets = () => cards.ConvertAll(c => (RectTransform)c.transform) },
            new Step { key = "tut.card", target = () => Rt(card) },
            new Step { key = "tut.element", target = () => card && card.elementBadge ? (RectTransform)card.elementBadge.transform : Rt(card) },
            new Step { key = "tut.tokens", target = () => card ? card.tokenArea : null },
            new Step { key = "tut.backpack", target = () => card && card.itemsButton ? (RectTransform)card.itemsButton.transform : Rt(card) },
            new Step { key = "tut.story", target = () => Rt(story) },
            new Step { key = "tut.eventSlots", targets = () => eventDeck ? new List<RectTransform>(eventDeck.slots) : new List<RectTransform>() },
            new Step { key = "tut.itemPile", target = () => Rt(itemDeck) },
            new Step { key = "tut.eventPile", target = () => Rt(eventDeck) },
            new Step { key = "tut.tokenLibrary", target = () => Find("TokenLibrary") },
            new Step { key = "tut.fortune", target = () => Rt(fortune), clickToContinue = () => fortune ? fortune.boxButton : null },
            new Step { key = "tut.introDone" },
        };
    }

    // --- 战斗教程: a practice fight (the event in combatEvent) ---

    List<Step> CombatSteps()
    {
        var eventDeck = FindFirstObjectByType<EventDeck>();
        var popup = FindFirstObjectByType<EventPopup>(FindObjectsInactive.Include);
        var combat = FindFirstObjectByType<CombatPanel>(FindObjectsInactive.Include);
        var story = FindFirstObjectByType<StoryCard>();
        RectTransform Rt(Component c) => c ? (RectTransform)c.transform : null;
        bool CombatOpen() => combat && combat.gameObject.activeInHierarchy;
        // A character's 背包 button (where 负伤 cards go).
        RectTransform Backpack()
        {
            var all = new List<CharacterCard>(FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude));
            all.Sort((x, y) => string.Compare(x.name, y.name, System.StringComparison.Ordinal));
            var c = all.Count > 0 ? all[0] : null;
            return c && c.itemsButton ? (RectTransform)c.itemsButton.transform : Rt(c);
        }

        RectTransform FirstEventCard()
        {
            if (!eventDeck) return null;
            foreach (var slot in eventDeck.slots) if (slot.childCount > 0) return (RectTransform)slot.GetChild(0);
            return null;
        }
        // The popup has one button per option, in order: light the event's (first) combat option.
        RectTransform CombatOption()
        {
            if (!popup || !combatEvent) return null;
            int index = combatEvent.options.FindIndex(o => o.kind == OptionKind.Combat);
            int n = 0;
            foreach (Transform child in popup.optionRow)
            {
                if (!child.gameObject.activeSelf || child == popup.optionButtonTemplate.transform) continue;
                if (n++ == index) return (RectTransform)child;
            }
            return null;
        }

        return new List<Step>
        {
            new Step
            {
                key = "tut.combatStart",
                onStart = () => PrepareCombat(eventDeck, story),
                target = FirstEventCard,
                doneWhen = () => popup && popup.gameObject.activeInHierarchy,
            },
            new Step { key = "tut.combatOption", target = CombatOption, doneWhen = CombatOpen },
            new Step { key = "tut.enemy", target = () => combat ? Rt(combat.cardView) : null },
            new Step { key = "tut.chart", target = () => combat && combat.elementChartButton ? Rt(combat.elementChartButton) : null },
            new Step { key = "tut.pick", target = () => combat ? combat.pickRow : null, doneWhen = () => combat && combat.Fighter },
            new Step { key = "tut.weapon", target = () => combat && combat.weaponHolder.childCount > 0 ? (RectTransform)combat.weaponHolder.GetChild(0) : (combat ? combat.weaponHolder : null) },
            new Step { key = "tut.fight", target = () => combat ? Rt(combat.fightButton) : null, free = true, doneWhen = () => combat && combat.Fought },
            new Step { key = "tut.total", target = () => combat ? Rt(combat.totalText) : null },
            new Step { key = "tut.items", target = () => combat ? combat.itemRow : null },
            new Step { key = "tut.endRound", target = () => combat ? Rt(combat.endRoundButton) : null, doneWhen = () => combat && combat.Round >= 2 },
            new Step { key = "tut.finishCombat", free = true, doneWhen = () => combat && combat.Finished && combat.resultView && combat.resultView.activeInHierarchy },
            new Step { key = "tut.result", target = () => combat && combat.resultView ? (RectTransform)combat.resultView.transform : null },
            new Step { key = "tut.continue", target = () => combat ? Rt(combat.continueButton) : null, free = true, doneWhen = () => !CombatOpen() },
            new Step { key = "tut.injury", target = Backpack },
            new Step { key = "tut.injuryHeal", target = Backpack },
            new Step { key = "tut.combatDone" },
        };
    }

    // --- 社交教程: the same event again, this time talking (its social option) ---

    List<Step> SocialSteps()
    {
        var eventDeck = FindFirstObjectByType<EventDeck>();
        var popup = FindFirstObjectByType<EventPopup>(FindObjectsInactive.Include);
        var social = FindFirstObjectByType<SocialPanel>(FindObjectsInactive.Include);
        var story = FindFirstObjectByType<StoryCard>();
        RectTransform Rt(Component c) => c ? (RectTransform)c.transform : null;
        bool SocialOpen() => social && social.gameObject.activeInHierarchy;

        RectTransform FirstEventCard()
        {
            if (!eventDeck) return null;
            foreach (var slot in eventDeck.slots) if (slot.childCount > 0) return (RectTransform)slot.GetChild(0);
            return null;
        }

        return new List<Step>
        {
            new Step
            {
                key = "tut.socialStart",
                onStart = () => PrepareSocial(eventDeck, story),
                target = FirstEventCard,
                doneWhen = () => (popup && popup.gameObject.activeInHierarchy) || SocialOpen(),
            },
            new Step { key = "tut.socialOption", target = () => OptionButton(popup, OptionKind.Social), doneWhen = SocialOpen },
            new Step { key = "tut.socialGoal" },
            new Step { key = "tut.npc", target = () => social ? Rt(social.cardView) : null },
            new Step { key = "tut.pool", target = () => social ? social.poolRow : null },
            new Step { key = "tut.favor", targets = () => social ? new List<RectTransform> { Rt(social.favorText), Rt(social.needText) } : new List<RectTransform>() },
            new Step { key = "tut.socialChart", target = () => social && social.elementChartButton ? Rt(social.elementChartButton) : null },
            new Step { key = "tut.socialPick", target = () => social ? social.pickRow : null, doneWhen = () => social && social.Speaker },
            new Step { key = "tut.speak", target = () => social ? Rt(social.speakButton) : null, free = true, doneWhen = () => social && (social.Spoke || social.Finished) },
            new Step { key = "tut.draw", target = () => social ? Rt(social.drawText) : null },
            new Step { key = "tut.socialItems", target = () => social ? social.itemRow : null },
            new Step { key = "tut.socialEnd", target = () => social ? social.EndRow : null, free = true, doneWhen = () => social && (social.Round >= 2 || social.Finished) },
            new Step { key = "tut.finishSocial", free = true, doneWhen = () => social && social.Finished && social.resultView && social.resultView.activeInHierarchy },
            new Step { key = "tut.socialResult", target = () => social && social.resultView ? (RectTransform)social.resultView.transform : null },
            new Step { key = "tut.socialContinue", target = () => social ? Rt(social.continueButton) : null, free = true, doneWhen = () => !SocialOpen() },
            new Step { key = "tut.socialDone" },
        };
    }

    // The popup has one button per option, in order: the event's first option of this kind.
    RectTransform OptionButton(EventPopup popup, OptionKind kind)
    {
        if (!popup || !combatEvent) return null;
        int index = combatEvent.options.FindIndex(o => o.kind == kind);
        int n = 0;
        foreach (Transform child in popup.optionRow)
        {
            if (!child.gameObject.activeSelf || child == popup.optionButtonTemplate.transform) continue;
            if (n++ == index) return (RectTransform)child;
        }
        return null;
    }

    // The practice talk: the same event goes on the table again. Talking needs at least 1 token; only if nobody
    // has any left (spent in the fight) does everyone get 1 primary token, so the practice can go on.
    void PrepareSocial(EventDeck eventDeck, StoryCard story)
    {
        if (story) story.holdStory = true;
        var cards = FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude);
        bool anyTokens = false;
        foreach (var p in cards) if (CombatPanel.TokensOf(p).Count > 0) anyTokens = true;
        var injuries = InjuryDeck.Find();
        if (!anyTokens && injuries && injuries.tokenPrefab)
            foreach (var p in cards) StartCoroutine(TokenFlight.Give(p, p.primaryElement, injuries.tokenPrefab));
        if (eventDeck && combatEvent) eventDeck.DrawSpecific(new List<EventData> { combatEvent });
    }

    // The practice fight: the event goes on the table, everyone equips a weapon if they have one
    // (plus combatBonusTokens extra primary tokens, none by default), and the story card stays put afterwards.
    void PrepareCombat(EventDeck eventDeck, StoryCard story)
    {
        if (story) story.holdStory = true;
        var injuries = InjuryDeck.Find();
        var prefab = injuries ? injuries.tokenPrefab : null;
        foreach (var p in FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude))
        {
            if (prefab) for (int i = 0; i < combatBonusTokens; i++) StartCoroutine(TokenFlight.Give(p, p.primaryElement, prefab));
            if (!p.Equipped)
                for (int i = 0; i < p.items.Count; i++)
                    if (p.items[i] && p.CanEquip(p.items[i])) { p.ToggleEquip(i); break; }
            p.Refresh();
        }
        if (eventDeck && combatEvent) eventDeck.DrawSpecific(new List<EventData> { combatEvent });
    }

    // --- Running a step ---

    IEnumerator Run(Step step)
    {
        advance = false;
        step.onStart?.Invoke();
        var targets = step.targets != null ? step.targets() : new List<RectTransform>();
        if (step.target != null) targets.Add(step.target());
        var click = step.clickToContinue != null ? step.clickToContinue() : null;
        nextButton.gameObject.SetActive(click == null && step.doneWhen == null);
        foreach (var shade in shades) shade.gameObject.SetActive(!step.free);
        freeStep = step.free;
        bubble.GetComponent<Image>().raycastTarget = !step.free;   // A free step's bubble lets clicks through.
        UnityEngine.Events.UnityAction onClick = () => advance = true;
        if (click) click.onClick.AddListener(onClick);

        int seen = -1;
        while (step.doneWhen != null ? !step.doneWhen() : !advance)
        {
            // Re-placed every frame: the table may still be moving (cards flying in, resizing).
            if (step.target != null) targets[targets.Count - 1] = step.target();   // e.g. a card that has just arrived.
            Light(targets);
            if (seen != languageVersion)
            {
                seen = languageVersion;
                bubbleText.text = L.T(step.key);
                nextLabel.text = L.T("tut.next");
                // The bubble grows to fit the text (text area: 24 top, 80 bottom for the button).
                float textWidth = bubble.sizeDelta.x - 56f;
                var settings = bubbleText.GetGenerationSettings(new Vector2(textWidth, 0f));
                float textHeight = bubbleText.cachedTextGeneratorForLayout.GetPreferredHeight(bubbleText.text, settings) / bubbleText.pixelsPerUnit;
                bool hasButton = nextButton.gameObject.activeSelf;
                bubble.sizeDelta = new Vector2(bubble.sizeDelta.x, Mathf.Max(hasButton ? 200f : 110f, textHeight + 24f + (hasButton ? 80f : 24f) + 12f));
                var textRt = (RectTransform)bubbleText.transform;
                textRt.offsetMin = new Vector2(28f, hasButton ? 80f : 24f);
            }
            yield return null;
        }
        if (click)
        {
            click.onClick.RemoveListener(onClick);
            yield return new WaitForSeconds(1.2f);   // Let the click's own result show (e.g. the stick coming out).
        }
    }

    void Finish()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameScene);
    }

    // --- Overlay ---

    void BuildOverlay()
    {
        var canvasGo = new GameObject("TutorialOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;   // Above the table, under the 设置 button.
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        overlay = (RectTransform)canvasGo.transform;

        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("Shade", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(overlay, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            shades[i] = go.GetComponent<Image>();
            shades[i].color = Dim;   // Also blocks clicks outside the hole.
        }

        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(overlay, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            borders[i] = go.GetComponent<Image>();
            borders[i].color = new Color(1f, 0.82f, 0.3f, 1f);
            borders[i].raycastTarget = false;
        }

        // Speech bubble: text + 下一步.
        var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(Outline));
        bubbleGo.transform.SetParent(overlay, false);
        bubble = (RectTransform)bubbleGo.transform;
        bubble.anchorMin = bubble.anchorMax = Vector2.zero;
        bubble.pivot = new Vector2(0.5f, 0.5f);
        bubble.sizeDelta = new Vector2(560f, 260f);
        bubbleGo.GetComponent<Image>().color = Paper;
        bubbleGo.GetComponent<Outline>().effectColor = Ink;
        bubbleText = MakeText(bubble, 28, TextAnchor.UpperLeft);
        var trt = (RectTransform)bubbleText.transform;
        trt.offsetMin = new Vector2(28f, 80f);
        trt.offsetMax = new Vector2(-28f, -24f);
        nextButton = MakeButton(bubble, new Vector2(1f, 0f), new Vector2(-20f, 18f), new Vector2(170f, 54f), out nextLabel);
        nextButton.onClick.AddListener(() => advance = true);

        // 跳过教程, bottom right, always there.
        var skip = MakeButton(overlay, new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(240f, 70f), out var skipLabel);
        skipLabel.text = L.T("tut.skip");
        skipLabel.gameObject.AddComponent<LocalizedText>().key = "tut.skip";
        skip.onClick.AddListener(Finish);
    }

    // Dims everything but the target (plus padding), outlines it, and puts the bubble next to it.
    void Light(List<RectTransform> targets)
    {
        Vector2 size = overlay.rect.size;
        Rect hole = new Rect(size.x * 0.5f, size.y * 0.5f, 0f, 0f);   // Nothing lit: dim all.
        bool any = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;
        var corners = new Vector3[4];
        foreach (var target in targets)
        {
            if (!target || !target.gameObject.activeInHierarchy) continue;
            target.GetWorldCorners(corners);
            Vector2 lo = ToOverlay(corners[0]), hi = ToOverlay(corners[2]);
            min = any ? Vector2.Min(min, lo) : lo;
            max = any ? Vector2.Max(max, hi) : hi;
            any = true;
        }
        if (any) hole = Rect.MinMaxRect(min.x - Padding, min.y - Padding, max.x + Padding, max.y + Padding);

        hole.xMin = Mathf.Clamp(hole.xMin, 0f, size.x);
        hole.xMax = Mathf.Clamp(hole.xMax, 0f, size.x);
        hole.yMin = Mathf.Clamp(hole.yMin, 0f, size.y);
        hole.yMax = Mathf.Clamp(hole.yMax, 0f, size.y);
        Place(shades[0], 0f, hole.yMax, size.x, size.y - hole.yMax);          // Top
        Place(shades[1], 0f, 0f, size.x, hole.yMin);                          // Bottom
        Place(shades[2], 0f, hole.yMin, hole.xMin, hole.height);              // Left
        Place(shades[3], hole.xMax, hole.yMin, size.x - hole.xMax, hole.height); // Right
        const float T = 5f;
        bool lit = hole.width > 0f;
        foreach (var border in borders) border.gameObject.SetActive(lit);
        Place(borders[0], hole.xMin - T, hole.yMax, hole.width + 2 * T, T);   // Top
        Place(borders[1], hole.xMin - T, hole.yMin - T, hole.width + 2 * T, T); // Bottom
        Place(borders[2], hole.xMin - T, hole.yMin, T, hole.height);          // Left
        Place(borders[3], hole.xMax, hole.yMin, T, hole.height);              // Right

        // Bubble: beside the hole on the side with the most room; in the middle if nothing is lit.
        Vector2 b = bubble.sizeDelta;
        Vector2 pos;
        // Nothing lit: in the middle, or for a free step bottom right (above 跳过教程), out of the way.
        if (hole.width <= 0f) pos = freeStep ? new Vector2(size.x - b.x * 0.5f - 20f, b.y * 0.5f + 110f) : size * 0.5f;
        else
        {
            float right = size.x - hole.xMax, left = hole.xMin, below = hole.yMin, above = size.y - hole.yMax;
            float best = Mathf.Max(right, left, below, above);
            if (best == right) pos = new Vector2(hole.xMax + 20f + b.x * 0.5f, hole.center.y);
            else if (best == left) pos = new Vector2(hole.xMin - 20f - b.x * 0.5f, hole.center.y);
            else if (best == below) pos = new Vector2(hole.center.x, hole.yMin - 20f - b.y * 0.5f);
            else pos = new Vector2(hole.center.x, hole.yMax + 20f + b.y * 0.5f);
        }
        pos.x = Mathf.Clamp(pos.x, b.x * 0.5f + 10f, size.x - b.x * 0.5f - 10f);
        pos.y = Mathf.Clamp(pos.y, b.y * 0.5f + 100f, size.y - b.y * 0.5f - 10f);   // Clear of the corner buttons.
        bubble.anchoredPosition = pos;
    }

    Vector2 ToOverlay(Vector3 world)
    {
        var cam = tableCanvas && tableCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? tableCanvas.worldCamera : null;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out var local);
        return local + overlay.rect.size * 0.5f;   // From the centre to the bottom-left corner.
    }

    static void Place(Image image, float x, float y, float w, float h)
    {
        var rt = image.rectTransform;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h));
    }

    Text MakeText(Transform parent, int size, TextAnchor align)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = Ink;
        t.raycastTarget = false;
        return t;
    }

    Button MakeButton(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, out Text label)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.82f, 0.76f, 0.62f);
        label = MakeText(go.transform, 26, TextAnchor.MiddleCenter);
        return go.GetComponent<Button>();
    }
}
