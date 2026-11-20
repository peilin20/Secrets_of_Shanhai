using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

// Runs one social encounter (社交对抗), following the 社交对抗规则 GDD. The team shares 好感度, starting at the card's 初始好感度.
// Each of the 3 rounds:
// ① pick who talks (负伤 that change 好感度 when they go on stage are settled before the judgment)
// ② they pick an element and bet any number (at least 1) of its tokens; bet tokens are spent
// ③ the NPC draws one element from its candidate pool (not put back) and the two are compared:
//    (社交对抗规则说明) same element = 心有灵犀 (bet ×3 in round 1, ×2 in rounds 2–3), 相生 either way = 相谈甚欢 (bet ×1),
//    anything else (相克) = 话不投机 (lose the bet)
// ④ anyone can use items ⑤ once 好感度 reaches an outcome the team may end there, or keep talking.
// 好感度 dropping to 0 is an immediate failure; after round 3 the team must have reached an outcome.
public class SocialPanel : MonoBehaviour
{
    public int rounds = 3;
    public ItemCard itemCardPrefab;
    public ElementToken tokenPrefab;       // For 祈雨符's temporary tokens.

    [Header("UI")]
    public SocialCard cardView;            // The NPC's social card.
    public Text roundText;
    public RectTransform pickRow;          // One button per player.
    public Button pickButtonTemplate;      // Inactive template.
    public RectTransform fieldCardHolder;  // The talking player's card goes here.
    public Text fieldHint;
    public Button speakButton;
    public Text poolLabel;
    public RectTransform poolRow;          // The NPC's candidate tokens; drawn ones are faded.
    public Image poolCircleTemplate;       // Inactive; copied per token.
    public Text drawText;                  // This round's NPC element and the result.
    public RectTransform itemRow;          // Items usable in social.
    public RectTransform itemEntryTemplate;// Inactive template: CardHolder, Owner (Text), UseButton.
    public Text itemDetailText;            // What the hovered item does.
    public Text favorText;
    public Text needText;                  // Each outcome and the 好感度 it needs.
    public Text logText;
    public Button closeButton;
    public Button elementChartButton;      // Small 五行生克 chart; click to enlarge.
    public GameObject elementChartZoom;    // The enlarged chart; click anywhere to close.
    public float fieldCardScale = 0.55f;
    public float itemCardScale = 0.4f;
    public float zoomCardScale = 1.6f;

    [Header("Result screen")]
    public GameObject resultView;
    public Text resultTitle;
    public Text resultTotal;
    public Text resultDescription;
    public Text resultReward;
    public Button continueButton;

    SocialData social;
    int round;
    int favor;
    int needLess;           // 香囊 / 替伤符 / the card's species bonus: every outcome needs this much less.
    bool busy;
    bool spoke;             // This round's talker has bet; items can still be used before moving on.
    bool extraTurn;         // 定身术: the talker talks again this round.
    bool finished;
    bool failed;
    bool bonusUsed;
    CharacterCard speaker;

    // Read by the tutorial to follow the encounter.
    public CharacterCard Speaker => speaker;
    public bool Spoke => spoke;
    public bool Finished => finished;
    public int Round => round;
    public RectTransform EndRow => endRow;

    readonly List<Element> pool = new List<Element>();
    readonly List<Element> drawn = new List<Element>();
    readonly List<Element> fullPool = new List<Element>();   // This encounter's whole pool (drawn ones included), for the display.
    readonly List<CharacterCard> speakers = new List<CharacterCard>();       // Everyone who talked in this event.
    readonly List<CharacterCard> onStageThisRound = new List<CharacterCard>();// On-stage 负伤 are paid once per appearance.
    readonly List<CharacterCard> protectedPlayers = new List<CharacterCard>();// Used 失败保护.
    bool continuing;
    readonly Dictionary<CharacterCard, Species> disguises = new Dictionary<CharacterCard, Species>();   // 易容面具
    readonly List<string> log = new List<string>();
    readonly List<CharacterCard> players = new List<CharacterCard>();
    readonly List<Button> pickButtons = new List<Button>();
    readonly List<Button> useButtons = new List<Button>();
    readonly List<CharacterCard> useOwners = new List<CharacterCard>();
    readonly List<ItemData> useItems = new List<ItemData>();
    readonly List<bool> useIsEquipment = new List<bool>();
    RectTransform choiceRow;   // Built on first use: buttons for choices (which element, how many…).
    RectTransform endRow;      // Built on first use: ⑤ buttons (next round / end with an outcome).
    Button bonusButton;        // Built on first use: the card's species bonus.
    GameObject cardZoom;       // Built on first use: an enlarged item card.
    int choice = -1;
    bool used;

    static readonly Color Tan = new Color(0.88f, 0.82f, 0.68f);
    static readonly Color Gold = new Color(0.95f, 0.75f, 0.35f);
    static readonly Color Blue = new Color(0.55f, 0.7f, 0.86f);

    void Awake()
    {
        // Once a combat / social is chosen it has to be played out (leaving early would not give spent tokens back).
        if (closeButton) closeButton.gameObject.SetActive(false);
        if (continueButton) continueButton.onClick.AddListener(Continue);
        if (speakButton) speakButton.onClick.AddListener(() =>
        {
            if (!busy && !finished && !spoke && speaker && CanSpeak(speaker) == null) StartCoroutine(Speak());
        });
        ElementChartArt.Setup(elementChartButton, elementChartZoom);   // The 五行 chart pictures (zh / en).
        if (elementChartButton && elementChartZoom)
            elementChartButton.onClick.AddListener(() => { elementChartZoom.SetActive(true); elementChartZoom.transform.SetAsLastSibling(); });
        var zoomButton = elementChartZoom ? elementChartZoom.GetComponent<Button>() : null;
        if (zoomButton) zoomButton.onClick.AddListener(() => elementChartZoom.SetActive(false));
    }

    public void Open(SocialData data, string fallbackTitle)
    {
        social = data;
        round = 1;
        favor = data ? data.startingFavor : 0;
        needLess = 0;
        busy = spoke = extraTurn = finished = failed = bonusUsed = false;
        speaker = null;
        viewed = null;
        pendingReward = null;
        pool.Clear();
        if (data) pool.AddRange(data.pool);
        // Pool shapes like 3-1 / 1-1-1-1 where the card names fewer elements than the shape needs:
        // the rest are random elements not already in the pool, picked for this encounter.
        if (data && data.randomOthers > 0)
        {
            var others = new List<Element>();
            foreach (Element e in System.Enum.GetValues(typeof(Element))) if (!pool.Contains(e)) others.Add(e);
            for (int i = 0; i < data.randomOthers && others.Count > 0; i++)
            {
                int k = Random.Range(0, others.Count);
                pool.Add(others[k]);
                others.RemoveAt(k);
            }
        }
        fullPool.Clear();
        fullPool.AddRange(pool);
        drawn.Clear();
        speakers.Clear();
        onStageThisRound.Clear();
        protectedPlayers.Clear();
        continuing = false;
        disguises.Clear();
        log.Clear();
        var injuries = InjuryDeck.Find();
        if (injuries) injuries.BeginEvent();
        if (resultView) resultView.SetActive(false);
        if (elementChartZoom) elementChartZoom.SetActive(false);
        if (cardZoom) cardZoom.SetActive(false);

        cardView.gameObject.SetActive(data != null);
        if (data) cardView.SetSocial(data);

        // Equipment can't change once the talk starts.
        players.Clear();
        players.AddRange(FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude));
        players.Sort((a, b) => string.Compare(a.playerName, b.playerName, System.StringComparison.Ordinal));
        foreach (var p in players) p.equipmentLocked = true;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        BuildPickButtons();
        BuildItemRow();
        ShowSpeaker(null);
        UpdateTexts();
    }

    public void Close()
    {
        StopAllCoroutines();
        continuing = false;
        foreach (var p in players)
        {
            p.equipmentLocked = false;
            // Temporary tokens (祈雨符) only last for this event.
            foreach (var t in CombatPanel.TokensOf(p)) if (t.temporary) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
        }
        gameObject.SetActive(false);
    }

    // --- ① Picking who talks ---

    void BuildPickButtons()
    {
        foreach (var b in pickButtons) if (b) Destroy(b.gameObject);
        pickButtons.Clear();
        foreach (var p in players)
        {
            var button = Instantiate(pickButtonTemplate, pickRow);
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<Text>().text = p.playerName;
            var player = p;
            button.onClick.AddListener(() => { if (!busy && !spoke && !extraTurn && !finished) ShowSpeaker(player); });
            pickButtons.Add(button);
        }
    }

    // Null if the player can talk now, otherwise why not.
    string CanSpeak(CharacterCard p)
    {
        var injuries = InjuryDeck.Find();
        string block = injuries ? injuries.StageBlock(p, false, speakers.Count == 0) : null;
        if (block != null) return L.T("combat.hintBlocked", block);
        if (CombatPanel.TokensOf(p).Count == 0) return L.T("social.noTokens", p.playerName);
        return null;
    }

    bool AnyoneCanSpeak()
    {
        foreach (var p in players) if (CanSpeak(p) == null) return true;
        return false;
    }

    // Puts the player's card on the field (null clears it).
    void ShowSpeaker(CharacterCard player)
    {
        speaker = player;
        if (player && player != viewed) ViewBackpack(player);   // The backpack row follows who's picked.
        CombatPanel.Clear(fieldCardHolder);
        if (player)
        {
            // A display-only copy of the character card.
            var copy = Instantiate(player.gameObject, fieldCardHolder);
            foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(mb is Graphic) && !(mb is LayoutGroup) && !(mb is LayoutElement) && !(mb is AspectRatioFitter)
                    && !(mb is Mask) && !(mb is RectMask2D) && !(mb is BaseMeshEffect))
                    DestroyImmediate(mb);
            copy.AddComponent<CanvasGroup>().blocksRaycasts = false;
            var crt = (RectTransform)copy.transform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.localScale = Vector3.one * fieldCardScale;
            ItemsPopup.HookCopiedItemsButton(copy, player);
        }
        ShowFieldButtons();
    }

    // Shows the right hint and buttons for where the round is.
    void ShowFieldButtons()
    {
        if (choiceRow) choiceRow.gameObject.SetActive(false);
        if (endRow) { CombatPanel.Clear(endRow); endRow.gameObject.SetActive(false); }
        speakButton.gameObject.SetActive(false);
        if (finished) { fieldHint.text = ""; RefreshInteractable(); return; }
        if (!social) { fieldHint.text = L.T("social.noCardOption"); RefreshInteractable(); return; }

        if (!spoke)
        {
            if (!AnyoneCanSpeak()) { Lose(L.T("social.nobodyCanBet")); return; }
            if (!speaker) fieldHint.text = L.T("social.hintPick");
            else if (CanSpeak(speaker) != null) fieldHint.text = CanSpeak(speaker);
            else
            {
                fieldHint.text = extraTurn ? L.T("social.hintAgain", speaker.playerName)
                    : L.T("social.hintSpeak", speaker.playerName);
                speakButton.gameObject.SetActive(true);
                speakButton.GetComponentInChildren<Text>().text = L.T("social.speakButton");
            }
            // Affinity already enough for an outcome (e.g. after an item): the team may end here and take it,
            // or keep talking. Ending isn't automatic.
            var labels = new List<string>();
            var colors = new List<Color>();
            var actions = new List<System.Action>();
            foreach (var outcome in social.outcomes)
            {
                if (favor < Need(outcome)) continue;
                var o = outcome;
                labels.Add(L.T("social.endWith", o.Label)); colors.Add(Gold); actions.Add(() => EndWith(o));
            }
            if (actions.Count > 0)
            {
                // The speak button joins the row, so both choices sit side by side.
                if (speakButton.gameObject.activeSelf)
                {
                    speakButton.gameObject.SetActive(false);
                    labels.Insert(0, L.T("social.speakButton")); colors.Insert(0, Tan);
                    actions.Insert(0, () => { if (!spoke && speaker && CanSpeak(speaker) == null) StartCoroutine(Speak()); });
                }
                // Short, so it fits on one line.
                if (!speaker) fieldHint.text = L.T("social.hintPickOrEnd");
                else if (CanSpeak(speaker) == null) fieldHint.text = L.T("social.hintSpeakOrEnd", speaker.playerName);
                ShowEndRow(labels, colors, actions);
            }
        }
        else
        {
            // ⑤ Keep talking, or end with an outcome the team has reached.
            var labels = new List<string>();
            var colors = new List<Color>();
            var actions = new List<System.Action>();
            if (round < rounds && pool.Count > 0)
            {
                labels.Add(L.T("social.nextRound")); colors.Add(Tan); actions.Add(NextRound);
            }
            foreach (var outcome in social.outcomes)
            {
                if (favor < Need(outcome)) continue;
                var o = outcome;
                labels.Add(L.T("social.endWith", o.Label)); colors.Add(Gold); actions.Add(() => EndWith(o));
            }
            if (actions.Count == 0)
            {
                labels.Add(L.T("social.endFail")); colors.Add(new Color(0.86f, 0.55f, 0.45f));
                actions.Add(() => Lose(L.T("social.outOfRounds")));
            }
            fieldHint.text = L.T(round < rounds ? "social.hintAfter" : "social.hintAfterLast");
            ShowEndRow(labels, colors, actions);
        }
        RefreshInteractable();
    }

    void NextRound()
    {
        if (busy || !spoke || finished) return;
        round++;
        spoke = extraTurn = false;
        onStageThisRound.Clear();
        ShowSpeaker(null);
        UpdateTexts();
    }

    // 心有灵犀's rate: ×3 in round 1, ×2 in rounds 2 and 3 (收益系数).
    static int SameElementRate(int round) => round == 1 ? 3 : 2;

    // --- ②③ Talking: bet tokens, the NPC draws ---

    IEnumerator Speak()
    {
        busy = true;
        HideFieldButtons();
        var p = speaker;

        var options = new List<Element>();
        var labels = new List<string>();
        var colors = new List<Color>();
        foreach (Element e in System.Enum.GetValues(typeof(Element)))
        {
            if (CombatPanel.Count(p, e) == 0) continue;
            options.Add(e);
            labels.Add(L.ElementName(e) + " ×" + CombatPanel.Count(p, e));
            colors.Add(ElementToken.ColorFor(e));
        }
        labels.Add(L.T("common.cancel")); colors.Add(Tan);
        yield return Choose(L.T("social.askElement", p.playerName), labels, colors);
        if (choice >= options.Count) { busy = false; ShowFieldButtons(); yield break; }
        var element = options[choice];

        int have = CombatPanel.Count(p, element);
        labels = new List<string>();
        colors = new List<Color>();
        for (int i = 1; i <= have; i++) { labels.Add(L.T("social.bet", i)); colors.Add(ElementToken.ColorFor(element)); }
        labels.Add(L.T("common.cancel")); colors.Add(Tan);
        yield return Choose(L.T("social.askBet", p.playerName, L.ElementName(element)), labels, colors);
        if (choice >= have) { busy = false; ShowFieldButtons(); yield break; }
        int bet = choice + 1;
        if (!speakers.Contains(p)) speakers.Add(p);
        var deck = InjuryDeck.Find();
        if (deck) deck.WentOnStage(p, round);

        int favorOnStage = favor;   // For 九尾: "在好感度为1时上场".
        // 负伤 that change 好感度 when this character goes on stage are settled before the judgment.
        if (!onStageThisRound.Contains(p))
        {
            onStageThisRound.Add(p);
            ApplyOnStage(p);
            UpdateTexts();
            if (favor <= 0) { busy = false; Lose(L.T("social.zero")); yield break; }
        }
        for (int i = 0; i < bet; i++) CombatPanel.Spend(p, element);

        // The NPC draws from its pool, not putting it back.
        drawText.text = L.T("social.thinking");
        yield return new WaitForSeconds(0.6f);
        var npc = pool[Random.Range(0, pool.Count)];
        pool.Remove(npc);
        drawn.Add(npc);

        int counted = bet + FireBonus(p, element);
        string ours = L.ElementName(element), theirs = L.ElementName(npc);
        string relation, result;
        int change;
        if (element == npc)
        {
            int m = SameElementRate(round);
            // 九尾 (种族&职业): "在社交中，如果在好感度为1时上场，本轮心有灵犀加成倍率为3".
            bool foxTalent = p.species == Species.Fox && favorOnStage == 1 && m < 3;
            if (foxTalent) m = 3;
            change = counted * m;
            relation = "<b>" + L.T("social.bothSame", ours) + "</b>";
            result = L.T("social.inTune") + " ×" + m + (foxTalent ? L.T("social.foxTalent") : "");
        }
        else if (ElementRules.Generates(element, npc) || ElementRules.Generates(npc, element))
        {
            change = counted;
            relation = "<b>" + (ElementRules.Generates(element, npc) ? L.T("social.generates", ours, theirs) : L.T("social.generates", theirs, ours)) + "</b>";
            result = L.T("social.gettingAlong") + " ×1";
        }
        else
        {
            change = -counted;
            relation = "<b>" + (ElementRules.Counters(element, npc) ? L.T("combat.counters", ours, theirs) : L.T("combat.counters", theirs, ours)) + "</b>";
            result = L.T("social.atOdds");
        }
        favor += change;
        string signed = (change >= 0 ? "+" : "") + change;
        log.Add(L.T("social.logBet", round, p.playerName, bet, ours) + (counted != bet ? L.T("social.countedAs", counted) : "")
                + L.T("social.logDraw", theirs, relation, result, signed));
        drawText.text = L.T("social.drawn", theirs) + "\n" + relation + "\n" + L.T("social.drawResult", result, signed);

        spoke = true;
        extraTurn = false;
        busy = false;
        UpdateTexts();
        if (favor <= 0) { Lose(L.T("social.zero")); yield break; }
        BuildItemRow();
        ShowFieldButtons();
    }

    // 负伤 like "每次上场团队好感度-1" / "每次好感度-2（在开始本轮社交前结算）".
    void ApplyOnStage(CharacterCard p)
    {
        foreach (var injury in p.injuries)
        {
            if (!injury) continue;
            var m = Regex.Match(injury.effect, @"好感度\s*-\s*(\d+)");
            if (!m.Success) continue;
            int n = int.Parse(m.Groups[1].Value);
            favor -= n;
            log.Add("  " + L.T("social.logInjury", p.playerName, L.ElementName(injury.element), n));
        }
    }

    // 负伤 like "在社交中使用火token威胁时额外计入3枚".
    static int FireBonus(CharacterCard p, Element element)
    {
        if (element != Element.Fire) return 0;
        int extra = 0;
        foreach (var injury in p.injuries)
        {
            if (!injury) continue;
            var m = Regex.Match(injury.effect, @"火token[^，。]*额外计入\s*(\d+)\s*枚");
            if (m.Success) extra += int.Parse(m.Groups[1].Value);
        }
        return extra;
    }

    int Need(SocialOutcome outcome)
    {
        int need = outcome.favorNeeded;
        // 特殊 like "如果白泽在场，大成功难度下降为7": the best outcome needs that much at most.
        if (social && social.outcomes.Count > 0 && outcome == social.outcomes[0] && !string.IsNullOrEmpty(social.special))
        {
            var m = Regex.Match(social.special, @"如果(\S{1,3}?)在场，大成功难度下降为\s*(\d+)");
            if (m.Success && players.Exists(p => CombatPanel.SpeciesName(p.species) == m.Groups[1].Value))
                need = Mathf.Min(need, int.Parse(m.Groups[2].Value));
        }
        return Mathf.Max(0, need - needLess);
    }

    // 特殊 like "如果九尾上场且取得胜利，额外获得1主属性token；取得大胜利时，额外获得2主属性token。"
    void SpeciesWinBonus(SocialOutcome outcome, List<string> notes)
    {
        if (!social || string.IsNullOrEmpty(social.special)) return;
        var m = Regex.Match(social.special, @"如果(\S{1,3}?)上场且取得胜利，额外获得\s*(\d+)\s*主属性\s*token[^\d]*取得大胜利时，额外获得\s*(\d+)\s*主属性\s*token");
        if (!m.Success) return;
        int n = int.Parse(outcome == social.outcomes[0] ? m.Groups[3].Value : m.Groups[2].Value);
        foreach (var p in speakers)
        {
            if (CombatPanel.SpeciesName(p.species) != m.Groups[1].Value) continue;
            int got = 0;
            for (int i = 0; i < n; i++) got += AddToken(p, p.primaryElement, false);
            notes.Add(L.T("social.speciesToken", p.playerName, L.SpeciesName(p.species), got, L.ElementName(p.primaryElement)));
        }
    }

    // --- ④ Items ---

    CharacterCard viewed;      // Whose backpack the bottom row shows.
    RectTransform backpackSwitch;

    void ViewBackpack(CharacterCard p)
    {
        viewed = p;
        BuildItemRow();
    }

    // The bottom row: one player's backpack. Consumables have a 使用 button; an equipped 替伤符 / 脚底抹油 too
    // (for whoever is talking, as before). Other equipment and 负伤 cards are shown so the players can see them.
    void BuildItemRow()
    {
        CombatPanel.Clear(itemRow, itemEntryTemplate.transform);
        useButtons.Clear();
        useOwners.Clear();
        useItems.Clear();
        useIsEquipment.Clear();
        if (itemDetailText) itemDetailText.text = L.T("items.hoverZoom");
        if (!viewed || !players.Contains(viewed)) viewed = speaker ? speaker : (players.Count > 0 ? players[0] : null);
        backpackSwitch = BackpackView.NameButtons(backpackSwitch, itemRow, pickButtonTemplate, players, viewed, ViewBackpack);
        var p = viewed;
        int shown = 0;
        if (p)
        {
            for (int i = 0; i < p.items.Count; i++)
            {
                var item = p.items[i];
                if (!item) continue;
                bool equipment = item.kind != ItemKind.Consumable;
                bool usable = !equipment || (i == p.equippedSlot && (item.mechanic == ItemMechanic.InjuryTransfer || item.mechanic == ItemMechanic.Escape));
                var entry = Instantiate(itemEntryTemplate, itemRow);
                entry.gameObject.SetActive(true);
                shown++;
                var card = Instantiate(itemCardPrefab, entry.Find("CardHolder"));
                card.SetItem(item);
                card.draggable = false;
                card.onClick = c => ShowCardZoom(c.item);
                CombatPanel.Place(card.transform, itemCardScale);
                entry.Find("Owner").GetComponent<Text>().text = L.T(equipment ? (i == p.equippedSlot ? "items.equipped" : "items.equipment") : "items.consumable");
                var player = p;
                int slot = i;
                var hover = entry.gameObject.AddComponent<SetupHover>();
                hover.onHover = () =>
                {
                    if (!itemDetailText) return;
                    string why = usable ? WhyNotUsable(player, item, equipment) : null;
                    itemDetailText.text = L.T("items.nameDetail", item.Name, CombatPanel.ItemSummary(item)) + (why != null ? "   " + L.T("items.cantUse", why) : "");
                };
                var use = entry.Find("UseButton").GetComponent<Button>();
                if (!usable) { use.gameObject.SetActive(false); continue; }
                use.onClick.AddListener(() =>
                {
                    if (!busy && !finished && WhyNotUsable(player, item, equipment) == null) StartCoroutine(Use(player, slot, equipment));
                });
                useButtons.Add(use);
                useOwners.Add(p);
                useItems.Add(item);
                useIsEquipment.Add(equipment);
            }
            foreach (var injury in p.injuries)
            {
                if (!injury) continue;
                BackpackView.AddInjury(itemRow, itemEntryTemplate, itemCardPrefab, itemCardScale, injury, itemDetailText);
                shown++;
            }
        }
        if (shown == 0)
        {
            var entry = Instantiate(itemEntryTemplate, itemRow);
            entry.gameObject.SetActive(true);
            entry.Find("Owner").GetComponent<Text>().text = L.T("items.emptyBackpack");
            entry.Find("UseButton").gameObject.SetActive(false);
        }
        RefreshInteractable();
    }

    // Null if the item can be used right now, otherwise the reason it can't.
    string WhyNotUsable(CharacterCard player, ItemData item, bool equipment)
    {
        if (equipment)
        {
            if (player != speaker) return L.T("why.speakerEquip");
            if (item.mechanic == ItemMechanic.Escape) return CombatPanel.TokensOf(player).Count >= 3 ? null : L.T("why.need3");
            foreach (var injury in player.injuries)
                if (injury && (injury.special ? CombatPanel.TokensOf(player).Count > 0 : CombatPanel.Count(player, injury.element) > 0)) return null;
            return L.T("why.noTransfer");
        }
        switch (item.mechanic)
        {
            case ItemMechanic.SocialOnly:
            case ItemMechanic.Sachet:
            case ItemMechanic.Disguise:
            case ItemMechanic.Retreat:
                return null;
            case ItemMechanic.RainCharm: return CombatPanel.Count(player, Element.Metal) > 0 ? null : L.T("why.noMetal");
            case ItemMechanic.SpiceTea: return null;
            case ItemMechanic.Sugar: return player == speaker ? null : L.T("why.speakerOnly");
            case ItemMechanic.Rattle: return L.T("why.rattle");
            case ItemMechanic.Salve:
            case ItemMechanic.Pouch:
            case ItemMechanic.Panacea: return L.T("why.useFromBackpack");
            case ItemMechanic.Money: return social && social.outcomes.Count > 0 ? null : L.T("why.noEffect");
            case ItemMechanic.NoEffect: return L.T("why.noEffect");
            case ItemMechanic.ExtraTurn:
                if (player != speaker || !spoke) return L.T("why.afterSpeak");
                return pool.Count > rounds - round ? null : L.T("why.poolTooSmall");
            case ItemMechanic.FailureGuard: return L.T("why.socialGuard");
        }
        return L.T("why.combatOnly");
    }

    IEnumerator Use(CharacterCard player, int slot, bool equipment)
    {
        var item = slot >= 0 && slot < player.items.Count ? player.items[slot] : null;
        if (!item) yield break;
        busy = true;
        HideFieldButtons();
        used = true;
        string src = L.T("combat.uses", "  " + L.T("combat.srcItem", player.playerName), item.Name);

        switch (item.mechanic)
        {
            case ItemMechanic.SocialOnly:
                favor += 5;
                log.Add(L.T("social.logPlus5", src));
                break;
            case ItemMechanic.Sachet: yield return UseSachet(player, src); break;
            case ItemMechanic.Disguise: yield return UseDisguise(player, src); break;
            case ItemMechanic.RainCharm: yield return UseRainCharm(player, src); break;
            case ItemMechanic.SpiceTea: yield return UseSpiceTea(player, src); break;
            case ItemMechanic.Sugar: yield return UseSugar(player, src); break;
            case ItemMechanic.Retreat: Retreat(L.T("combat.logRetreatSafe", src)); break;
            case ItemMechanic.ExtraTurn:
                spoke = false;
                extraTurn = true;
                log.Add(L.T("social.logExtraTurn", src, player.playerName));
                break;
            case ItemMechanic.InjuryTransfer: yield return UseInjuryTransfer(player, src); break;
            case ItemMechanic.Escape: yield return UseEscape(player, src); break;
            case ItemMechanic.Money:
            {
                // 一些钱: affinity rises to what 成功 (the last, plainest outcome) needs; never lowers it.
                var success = social.outcomes[social.outcomes.Count - 1];
                int before = favor;
                favor = Mathf.Max(favor, Need(success));
                log.Add(L.T("social.logMoney", src, success.Label, favor - before));
                break;
            }
            default: used = false; break;
        }

        if (used && !equipment)
        {
            player.items[slot] = null;   // One-time items are gone after use.
            player.Refresh();
        }
        busy = false;
        UpdateTexts();
        if (finished) yield break;
        BuildItemRow();
        ShowFieldButtons();
    }

    // 香囊: every outcome needs 1d4 less; if the user (or their disguise) is the NPC's species, the full 4.
    IEnumerator UseSachet(CharacterCard player, string src)
    {
        var species = disguises.TryGetValue(player, out var d) ? d : player.species;
        bool same = social && social.hasSpecies && social.species == species;
        int r = 4;
        if (!same) yield return RollDie(4, x => r = x);
        needLess += r;
        log.Add(L.T("social.logSachet", src, r) + L.T(same ? "combat.sachetSame" : "combat.sachetRoll"));
    }

    // Every die goes through 承天问卜 (the stick box in the middle of a dimmed screen; the player clicks it).
    IEnumerator RollDie(int sides, System.Action<int> result)
    {
        yield return FortuneSticks.Ask(sides, result);
    }

    // 易容面具: for this event, the NPC takes the user for a species they pick.
    IEnumerator UseDisguise(CharacterCard player, string src)
    {
        var all = (Species[])System.Enum.GetValues(typeof(Species));
        var labels = new List<string>();
        var colors = new List<Color>();
        foreach (var s in all) { labels.Add(L.SpeciesName(s)); colors.Add(Tan); }
        yield return Choose(L.T("combat.askDisguise", player.playerName), labels, colors);
        disguises[player] = all[choice];
        log.Add(L.T("combat.logDisguise", src, player.playerName, L.SpeciesName(all[choice])));
    }

    // 祈雨符: each 金 token spent gives 1d4 temporary 水 tokens (this event only).
    IEnumerator UseRainCharm(CharacterCard player, string src)
    {
        int have = CombatPanel.Count(player, Element.Metal);
        var labels = new List<string>();
        var colors = new List<Color>();
        for (int i = 1; i <= have; i++) { labels.Add(L.T("combat.spendMetal", i)); colors.Add(ElementToken.ColorFor(Element.Metal)); }
        yield return Choose(L.T("combat.askMetal", player.playerName), labels, colors);
        int n = choice + 1;
        int gained = 0;
        var rolls = new List<string>();
        for (int i = 0; i < n; i++)
        {
            CombatPanel.Spend(player, Element.Metal);
            int r = 0;
            yield return RollDie(4, x => r = x);
            rolls.Add(r.ToString());
            for (int k = 0; k < r; k++) gained += AddToken(player, Element.Water, true);   // 负伤 can block 水 tokens.
        }
        log.Add(L.T("combat.logRain", src, n, gained, string.Join("+", rolls.ToArray())));
    }

    // 香料茶: 2d4 temporary 水 tokens (this event only).
    IEnumerator UseSpiceTea(CharacterCard player, string src)
    {
        int a = 0, b = 0;
        yield return RollDie(4, x => a = x);
        yield return RollDie(4, x => b = x);
        int gained = 0;
        for (int k = 0; k < a + b; k++) gained += AddToken(player, Element.Water, true);   // 负伤 can block 水 tokens.
        log.Add(L.T("combat.logTea", src, gained, a, b));
    }

    // 糖: counts as 1 token of an element the speaker picks (a temporary token, this event only).
    IEnumerator UseSugar(CharacterCard player, string src)
    {
        var all = (Element[])System.Enum.GetValues(typeof(Element));
        var labels = new List<string>();
        var colors = new List<Color>();
        foreach (var e in all) { labels.Add(L.ElementName(e)); colors.Add(ElementToken.ColorFor(e)); }
        yield return Choose(L.T("social.askSugar", player.playerName), labels, colors);
        var element = all[choice];
        int gained = AddToken(player, element, true);
        log.Add(L.T("social.logSugar", src, L.ElementName(element)) + (gained == 0 ? L.T("social.blocked") : ""));
    }

    // 替伤符: per token spent, move one 负伤 card of that element to a teammate or the NPC
    // (to the NPC: every outcome needs 2d4 less each time).
    IEnumerator UseInjuryTransfer(CharacterCard player, string src)
    {
        int moved = 0;
        while (true)
        {
            var movable = new List<int>();
            for (int i = 0; i < player.injuries.Count; i++)
                if (player.injuries[i] && (player.injuries[i].special ? CombatPanel.TokensOf(player).Count > 0
                    : CombatPanel.Count(player, player.injuries[i].element) > 0)) movable.Add(i);
            if (movable.Count == 0) break;

            var cards = new List<InjuryData>();
            foreach (var i in movable) cards.Add(player.injuries[i]);
            yield return InjuryDeck.AskInjury(L.T("combat.askTransfer", player.playerName),
                cards, moved > 0 ? L.T("common.done") : null, x => choice = x);
            if (choice >= movable.Count) break;
            var injury = player.injuries[movable[choice]];

            var targets = new List<CharacterCard>();
            var labels = new List<string>();
            var colors = new List<Color>();
            foreach (var p in players) if (p != player) { targets.Add(p); labels.Add(p.playerName); colors.Add(Tan); }
            labels.Add("NPC"); colors.Add(Blue);
            yield return Choose(L.T("combat.askTransferTo", injury.Title), labels, colors);
            int targetChoice = choice;

            if (injury.special)
            {
                // Any one token pays for the 特殊负伤.
                var have = new List<Element>();
                foreach (Element e in System.Enum.GetValues(typeof(Element))) if (CombatPanel.Count(player, e) > 0) have.Add(e);
                labels = new List<string>();
                colors = new List<Color>();
                foreach (var e in have) { labels.Add(L.ElementName(e) + " ×" + CombatPanel.Count(player, e)); colors.Add(ElementToken.ColorFor(e)); }
                yield return Choose(L.T("combat.askTransferToken", player.playerName, injury.Title), labels, colors);
                CombatPanel.Spend(player, have[choice]);
            }
            else CombatPanel.Spend(player, injury.element);
            choice = targetChoice;
            player.injuries.Remove(injury);
            moved++;
            if (choice < targets.Count)
            {
                var target = targets[choice];
                log.Add(L.T("combat.logTransfer", src, injury.Title, target.playerName));
                var deck = InjuryDeck.Find();
                if (deck) yield return deck.Give(target, injury, false, log);   // Their backpack may need room.
                else { target.injuries.Add(injury); target.Refresh(); }
            }
            else
            {
                int a = 0, b = 0;
                yield return RollDie(4, x => a = x);
                yield return RollDie(4, x => b = x);
                needLess += a + b;
                log.Add(L.T("social.logTransferNpc", src, injury.Title, a, b, a + b));
            }
            player.Refresh();
            UpdateTexts();
        }
        if (moved == 0) used = false;
    }

    // 脚底抹油: spend any 3 tokens, the team leaves the talk safely.
    IEnumerator UseEscape(CharacterCard player, string src)
    {
        var labels = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var options = new List<Element>();
            labels = new List<string>();
            var colors = new List<Color>();
            foreach (Element e in System.Enum.GetValues(typeof(Element)))
            {
                if (CombatPanel.Count(player, e) == 0) continue;
                options.Add(e);
                labels.Add(L.ElementName(e) + " ×" + CombatPanel.Count(player, e));
                colors.Add(ElementToken.ColorFor(e));
            }
            yield return Choose(L.T("combat.askToken3", player.playerName, i + 1), labels, colors);
            CombatPanel.Spend(player, options[choice]);
        }
        Retreat(L.T("combat.logEscape", src));
    }

    // The card's own bonus, e.g. a 九尾 lets 大妈 touch their tail: every outcome needs less.
    void UseSpeciesBonus()
    {
        if (busy || finished || bonusUsed || !social) return;
        bonusUsed = true;
        needLess += social.bonusAmount;
        log.Add("  " + L.T("social.logBonus", social.BonusButton, social.bonusAmount));
        UpdateTexts();
        ShowFieldButtons();
    }

    bool TeamHasBonusSpecies()
    {
        if (!social || !social.hasSpeciesBonus) return false;
        foreach (var p in players) if (p.species == social.bonusSpecies) return true;
        return false;
    }

    // --- Choices ---

    // Shows one button per label where the speak button sits and waits for a click (index in `choice`).
    IEnumerator Choose(string hint, List<string> labels, List<Color> colors)
    {
        if (!choiceRow) choiceRow = MakeRow("ChoiceRow");
        HideFieldButtons();
        CombatPanel.Clear(choiceRow);
        choiceRow.gameObject.SetActive(true);
        choiceRow.SetAsLastSibling();
        fieldHint.text = hint;
        choice = -1;
        float width = Mathf.Min(170f, (760f - 8f * (labels.Count - 1)) / Mathf.Max(1, labels.Count));
        for (int i = 0; i < labels.Count; i++)
        {
            int index = i;
            MakeButton(choiceRow, labels[i], colors[i], width).onClick.AddListener(() => choice = index);
        }
        while (choice < 0) yield return null;
        choiceRow.gameObject.SetActive(false);
    }

    // The ⑤ buttons; unlike Choose, they don't block, so items stay usable.
    void ShowEndRow(List<string> labels, List<Color> colors, List<System.Action> actions)
    {
        if (!endRow) endRow = MakeRow("EndRow");
        CombatPanel.Clear(endRow);
        endRow.gameObject.SetActive(true);
        endRow.SetAsLastSibling();
        float width = Mathf.Min(260f, (760f - 8f * (labels.Count - 1)) / Mathf.Max(1, labels.Count));
        for (int i = 0; i < labels.Count; i++)
        {
            var action = actions[i];
            MakeButton(endRow, labels[i], colors[i], width).onClick.AddListener(() => { if (!busy && !finished) action(); });
        }
    }

    RectTransform MakeRow(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var row = (RectTransform)go.transform;
        row.SetParent(speakButton.transform.parent, false);
        var srt = (RectTransform)speakButton.transform;
        row.anchorMin = row.anchorMax = srt.anchorMin;
        row.anchoredPosition = srt.anchoredPosition;
        row.sizeDelta = new Vector2(760, 60);
        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8; layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false; layout.childControlHeight = false;
        layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
        return row;
    }

    Button MakeButton(RectTransform parent, string label, Color color, float width)
    {
        var b = Instantiate(pickButtonTemplate, parent);
        b.gameObject.SetActive(true);
        b.interactable = true;
        ((RectTransform)b.transform).sizeDelta = new Vector2(width, 56);
        b.GetComponent<Image>().color = color;
        b.GetComponentInChildren<Text>().text = label;
        return b;
    }

    void HideFieldButtons()
    {
        speakButton.gameObject.SetActive(false);
        if (endRow) endRow.gameObject.SetActive(false);
        RefreshInteractable();
    }

    // Shows a big copy of the item card over the window; a click anywhere closes it.
    void ShowCardZoom(ItemData item)
    {
        if (!item) return;
        if (!cardZoom)
        {
            cardZoom = new GameObject("CardZoom", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)cardZoom.transform;
            rt.SetParent(speakButton.transform.parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var dim = cardZoom.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.8f);
            var button = cardZoom.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => cardZoom.SetActive(false));

            var hint = new GameObject("Hint", typeof(RectTransform), typeof(Text));
            var hrt = (RectTransform)hint.transform;
            hrt.SetParent(rt, false);
            hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = new Vector2(0f, -460f);
            hrt.sizeDelta = new Vector2(700f, 40f);
            var text = hint.GetComponent<Text>();
            text.font = fieldHint.font;
            text.fontSize = 22;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.text = L.T("zoom.close");
        }
        CombatPanel.Clear(cardZoom.transform, cardZoom.transform.Find("Hint"));
        var card = Instantiate(itemCardPrefab, cardZoom.transform);
        card.SetItem(item);
        card.draggable = false;
        card.onClick = c => cardZoom.SetActive(false);
        CombatPanel.Place(card.transform, zoomCardScale);
        ((RectTransform)card.transform).anchoredPosition = new Vector2(0f, 20f);
        cardZoom.SetActive(true);
        cardZoom.transform.SetAsLastSibling();
    }

    int AddToken(CharacterCard player, Element element, bool temporary)
    {
        return player.GainToken(element, tokenPrefab, temporary);
    }

    // --- End of the talk ---

    void EndWith(SocialOutcome outcome)
    {
        if (finished) return;
        finished = true;
        HideFieldButtons();
        StartCoroutine(EndWithRoutine(outcome));
    }

    IEnumerator EndWithRoutine(SocialOutcome outcome)
    {
        log.Add(L.T("social.logEnd", outcome.Label));
        UpdateTexts();
        // 负伤 like "上场并赢得一次社交事件后，可弃掉这张牌" settle now; the reward's tokens ("每人2木token"…) are
        // handed out when 继续 is pressed, after the players have seen the result.
        var notes = new List<string>();
        var injuries = InjuryDeck.Find();
        SpeciesWinBonus(outcome, notes);
        if (injuries) notes.AddRange(injuries.OnWin(false));
        pendingReward = outcome.reward;
        ShowResult(outcome.Label, OutcomeList(outcome), string.Join("\n", notes.ToArray()), false);
        yield break;
    }

    string pendingReward;

    // Every outcome on the card, the one reached marked (null: the failure).
    string OutcomeList(SocialOutcome reached)
    {
        var lines = new List<string>();
        foreach (var o in social.outcomes)
            lines.Add(CombatPanel.OutcomeLine(o == reached, L.T("social.outcomeLine", o.Label, Need(o)), o.Reward));
        lines.Add(CombatPanel.OutcomeLine(reached == null, L.T("combat.fail"), social.FailureResult));
        return string.Join("\n\n", lines.ToArray());
    }

    void Lose(string reason)
    {
        if (finished) return;
        finished = true;
        failed = true;
        log.Add(L.T("social.logLost", reason));
        UpdateTexts();
        string next = social && social.failureCombat ? "\n" + L.T("social.thenCombat", social.failureCombat.Title) : "";
        ShowResult(L.T("combat.fail"), social ? OutcomeList(null) : "", reason + next, social != null);
    }

    void Retreat(string message)
    {
        log.Add(message);
        var injuries = InjuryDeck.Find();
        if (injuries) injuries.OnRetreat(players, log);   // "每次逃跑失去3主属性token"
        finished = true;
        UpdateTexts();
        ShowResult(L.T("combat.retreatTitle"), L.T("social.retreatBody"), "", false);
    }

    // The result screen: outcome, 好感度, text, notes, and (on a loss) 失败保护 buttons.
    void ShowResult(string title, string description, string reward, bool offerFailureGuard)
    {
        HideFieldButtons();
        if (choiceRow) choiceRow.gameObject.SetActive(false);
        if (!resultView) return;
        resultTitle.text = title;
        resultTotal.text = L.T("social.affinity", favor);
        CombatPanel.LayoutResult(resultTotal, resultDescription, resultReward);
        resultDescription.text = description;
        resultReward.text = reward;

        var guardRow = resultView.transform.Find("FailureGuards");
        if (guardRow) Destroy(guardRow.gameObject);
        if (offerFailureGuard)
        {
            var row = new GameObject("FailureGuards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rrt = (RectTransform)row.transform;
            rrt.SetParent(resultView.transform, false);
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.5f);
            rrt.anchoredPosition = new Vector2(0, -170);
            rrt.sizeDelta = new Vector2(1100, 60);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false; layout.childControlHeight = false;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            foreach (var p in players)
            {
                for (int i = 0; i < p.items.Count; i++)
                {
                    if (!p.items[i] || p.items[i].mechanic != ItemMechanic.FailureGuard) continue;
                    var player = p;
                    int slot = i;
                    var b = MakeButton(rrt, L.T("combat.useGuard", p.playerName), Gold, 330);
                    b.onClick.AddListener(() =>
                    {
                        if (continuing) return;
                        if (!protectedPlayers.Contains(player)) protectedPlayers.Add(player);
                        player.items[slot] = null;
                        player.Refresh();
                        resultReward.text += (resultReward.text.Length > 0 ? "\n" : "") + L.T("combat.guarded", player.playerName);
                        Destroy(b.gameObject);
                    });
                }
            }
        }
        resultView.SetActive(true);
        resultView.transform.SetAsLastSibling();
        RefreshInteractable();
    }

    // After the result: on a loss, the 失败 text's 负伤 are drawn, and 负伤 that heal at the end of an event
    // are settled. Then close the talk and take the event card off the table (once every drawn event is
    // dealt with, the next main story card turns). A failed talk with a 失败 combat goes into that combat instead;
    // the event goes on there.
    void Continue()
    {
        if (!continuing) StartCoroutine(ContinueRoutine());
    }

    IEnumerator ContinueRoutine()
    {
        continuing = true;
        if (!string.IsNullOrEmpty(pendingReward))
        {
            var notes = new List<string>();
            yield return RewardRules.Give(pendingReward, players, speakers, tokenPrefab, notes);
        }
        pendingReward = null;
        bool toCombat = failed && social && social.failureCombat;
        var next = social ? social.failureCombat : null;
        var injuries = InjuryDeck.Find();
        if (injuries && !toCombat)
        {
            if (failed && social) yield return injuries.ApplyFailure(social.failureResult, players, protectedPlayers, social.special);
            yield return injuries.EndEvent(players);
        }
        Close();
        if (toCombat)
        {
            var combat = FindFirstObjectByType<CombatPanel>(FindObjectsInactive.Include);
            if (combat) { combat.Open(next, next.Title); yield break; }
        }
        var events = FindFirstObjectByType<EventPopup>(FindObjectsInactive.Include);
        if (events) events.ResolveCurrentEvent();
        var story = FindFirstObjectByType<StoryCard>();
        if (story) story.AdvanceWhenEventsDone();
    }

    // --- Texts ---

    void RefreshInteractable()
    {
        bool open = !busy && !finished;
        for (int i = 0; i < pickButtons.Count; i++)
        {
            pickButtons[i].interactable = open && !spoke && !extraTurn;   // The talker can't change after betting.
            pickButtons[i].GetComponent<Image>().color = players[i] == speaker ? Gold : Tan;
        }
        speakButton.interactable = open && !spoke && speaker && CanSpeak(speaker) == null;
        for (int i = 0; i < useButtons.Count; i++)
            useButtons[i].interactable = open && WhyNotUsable(useOwners[i], useItems[i], useIsEquipment[i]) == null;
        if (bonusButton) bonusButton.interactable = open;
    }

    void UpdateTexts()
    {
        roundText.text = finished ? L.T("social.over") : L.T("combat.round", round, rounds);
        favorText.text = L.T("social.affinity", favor);

        var sb = new StringBuilder();
        if (social)
            foreach (var outcome in social.outcomes)
                sb.Append(L.T("social.needLine", outcome.Label, Need(outcome)))
                  .Append(favor >= Need(outcome) ? L.T("social.reached") : "").Append('\n');
        if (needLess > 0) sb.Append(L.T("social.allLowered", needLess));
        needText.text = social ? sb.ToString().TrimEnd('\n') : L.T("social.noCard");

        // The species bonus button sits under the needs.
        bool showBonus = !finished && !bonusUsed && TeamHasBonusSpecies();
        if (showBonus && !bonusButton)
        {
            bonusButton = MakeButton((RectTransform)needText.transform.parent, "", Gold, 430);
            var brt = (RectTransform)bonusButton.transform;
            var nrt = (RectTransform)needText.transform;
            brt.anchorMin = brt.anchorMax = nrt.anchorMin;
            brt.anchoredPosition = nrt.anchoredPosition + new Vector2(0f, -nrt.sizeDelta.y * 0.5f - 30f);
            brt.sizeDelta = new Vector2(nrt.sizeDelta.x, 44f);
            bonusButton.onClick.AddListener(UseSpeciesBonus);
        }
        if (bonusButton)
        {
            bonusButton.gameObject.SetActive(showBonus);
            if (showBonus) bonusButton.GetComponentInChildren<Text>().text = L.T("social.bonusLabel", social.BonusButton, social.bonusAmount);
        }

        // The NPC's pool: drawn tokens stay visible, faded.
        if (poolRow && poolCircleTemplate)
        {
            CombatPanel.Clear(poolRow, poolCircleTemplate.transform);
            if (social)
            {
                var drawnLeft = new List<Element>(drawn);
                var all = new List<Element>(fullPool);
                all.Sort();
                foreach (var e in all)
                {
                    var circle = Instantiate(poolCircleTemplate, poolRow);
                    circle.gameObject.SetActive(true);
                    var color = ElementToken.ColorFor(e);
                    if (drawnLeft.Remove(e)) color.a = 0.25f;
                    circle.color = color;
                    circle.GetComponentInChildren<Text>().text = CombatCard.ElementChar(e);
                }
            }
        }
        if (poolLabel) poolLabel.text = L.T("social.pool", pool.Count);
        if (drawText && !spoke && !busy && !finished)
            drawText.text = L.T("social.rates", round, SameElementRate(round));

        var logSb = new StringBuilder();
        foreach (var line in log) logSb.AppendLine(line);
        logText.text = logSb.ToString();
    }
}
