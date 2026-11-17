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
//    same element = 大成功 (bet ×5 in round 1, ×2 later), 相生 = 成功 (bet ×1, ×½ in round 3), otherwise 失败 (lose the bet)
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
            ItemsPopup.AddBackpackTab(button, p);
            button.onClick.AddListener(() => { if (!busy && !spoke && !extraTurn && !finished) ShowSpeaker(player); });
            pickButtons.Add(button);
        }
    }

    // Null if the player can talk now, otherwise why not.
    string CanSpeak(CharacterCard p)
    {
        var injuries = InjuryDeck.Find();
        string block = injuries ? injuries.StageBlock(p, false, speakers.Count == 0) : null;
        if (block != null) return block + "，请换人";
        if (CombatPanel.TokensOf(p).Count == 0) return p.playerName + " 没有 token，不能下注，请换人";
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
        if (!social) { fieldHint.text = "这个选项还没有关联社交卡。"; RefreshInteractable(); return; }

        if (!spoke)
        {
            if (!AnyoneCanSpeak()) { Lose("没有玩家还能下注（每轮至少下注 1 枚 token）"); return; }
            if (!speaker) fieldHint.text = "① 选择本轮说话的玩家";
            else if (CanSpeak(speaker) != null) fieldHint.text = CanSpeak(speaker);
            else
            {
                fieldHint.text = extraTurn ? "② " + speaker.playerName + " 再说一次（定身术）"
                    : "② " + speaker.playerName + " 说话：选一个属性，下注至少 1 枚（说话前可以换人）";
                speakButton.gameObject.SetActive(true);
                speakButton.GetComponentInChildren<Text>().text = "② 选择属性并下注";
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
                labels.Add("⑤ 继续下一轮"); colors.Add(Tan); actions.Add(NextRound);
            }
            foreach (var outcome in social.outcomes)
            {
                if (favor < Need(outcome)) continue;
                var o = outcome;
                labels.Add("结束：" + o.label); colors.Add(Gold); actions.Add(() => EndWith(o));
            }
            if (actions.Count == 0)
            {
                labels.Add("结束对抗（好感度不够，失败）"); colors.Add(new Color(0.86f, 0.55f, 0.45f));
                actions.Add(() => Lose("3 轮结束，好感度没有达到任何选项"));
            }
            fieldHint.text = "④ 还可以使用道具   ⑤ " + (round < rounds ? "继续下一轮，或在好感度够时结束对抗" : "最后一轮结束，选择结果");
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
            labels.Add(CombatCard.ElementChar(e) + " ×" + CombatPanel.Count(p, e));
            colors.Add(ElementToken.ColorFor(e));
        }
        labels.Add("取消"); colors.Add(Tan);
        yield return Choose(p.playerName + "：用哪个属性说话？", labels, colors);
        if (choice >= options.Count) { busy = false; ShowFieldButtons(); yield break; }
        var element = options[choice];

        int have = CombatPanel.Count(p, element);
        labels = new List<string>();
        colors = new List<Color>();
        for (int i = 1; i <= have; i++) { labels.Add("下注 " + i); colors.Add(ElementToken.ColorFor(element)); }
        labels.Add("取消"); colors.Add(Tan);
        yield return Choose(p.playerName + "：下注几枚" + CombatCard.ElementChar(element) + "？（下得越多，输赢越大）", labels, colors);
        if (choice >= have) { busy = false; ShowFieldButtons(); yield break; }
        int bet = choice + 1;
        if (!speakers.Contains(p)) speakers.Add(p);
        var deck = InjuryDeck.Find();
        if (deck) deck.WentOnStage(p, round);

        // 负伤 that change 好感度 when this character goes on stage are settled before the judgment.
        if (!onStageThisRound.Contains(p))
        {
            onStageThisRound.Add(p);
            ApplyOnStage(p);
            UpdateTexts();
            if (favor <= 0) { busy = false; Lose("好感度降到 0"); yield break; }
        }
        for (int i = 0; i < bet; i++) CombatPanel.Spend(p, element);

        // The NPC draws from its pool, not putting it back.
        drawText.text = "NPC 正在想……";
        yield return new WaitForSeconds(0.6f);
        var npc = pool[Random.Range(0, pool.Count)];
        pool.Remove(npc);
        drawn.Add(npc);

        int counted = bet + FireBonus(p, element);
        string ours = CombatCard.ElementChar(element), theirs = CombatCard.ElementChar(npc);
        string relation, result;
        int change;
        if (element == npc)
        {
            int m = round == 1 ? 5 : 2;
            change = counted * m;
            relation = "<b>心有灵犀（都是" + ours + "）</b>";
            result = "大成功 ×" + m;
        }
        else if (ElementRules.Generates(element, npc) || ElementRules.Generates(npc, element))
        {
            bool half = round >= rounds;
            change = half ? counted / 2 : counted;
            relation = "<b>" + (ElementRules.Generates(element, npc) ? ours + "生" + theirs : theirs + "生" + ours) + "</b>";
            result = half ? "成功 ×½" : "成功 ×1";
        }
        else
        {
            change = -counted;
            relation = "<b>" + (ElementRules.Counters(element, npc) ? ours + "克" + theirs : theirs + "克" + ours) + "</b>";
            result = "失败";
        }
        favor += change;
        log.Add("第" + round + "轮 " + p.playerName + " 下注 " + bet + " 枚" + ours + (counted != bet ? "（负伤：算作 " + counted + " 枚）" : "")
                + "，NPC 抽到" + theirs + "：" + relation + "，" + result + " → 好感度 " + (change >= 0 ? "+" : "") + change);
        drawText.text = "NPC 抽到：" + theirs + "\n" + relation + "\n" + result + "，好感度 " + (change >= 0 ? "+" : "") + change;

        spoke = true;
        extraTurn = false;
        busy = false;
        UpdateTexts();
        if (favor <= 0) { Lose("好感度降到 0"); yield break; }
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
            log.Add("  " + p.playerName + " 上场：" + CombatCard.ElementChar(injury.element) + "负伤，好感度 -" + n);
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
            notes.Add(p.playerName + "（" + m.Groups[1].Value + "）额外获得 " + got + " 枚" + CombatCard.ElementChar(p.primaryElement) + " token");
        }
    }

    // --- ④ Items ---

    void BuildItemRow()
    {
        CombatPanel.Clear(itemRow, itemEntryTemplate.transform);
        useButtons.Clear();
        useOwners.Clear();
        useItems.Clear();
        useIsEquipment.Clear();
        if (itemDetailText) itemDetailText.text = "鼠标移到道具上查看效果，点击卡片放大";
        foreach (var p in players)
        {
            for (int i = 0; i < p.items.Count; i++)
            {
                var item = p.items[i];
                if (!item) continue;
                // Equipped 替伤符 / 脚底抹油 also work in social, for whoever is talking.
                bool equipment = item.kind != ItemKind.Consumable;
                if (equipment && (i != p.equippedSlot || (item.mechanic != ItemMechanic.InjuryTransfer && item.mechanic != ItemMechanic.Escape)))
                    continue;
                var entry = Instantiate(itemEntryTemplate, itemRow);
                entry.gameObject.SetActive(true);
                var card = Instantiate(itemCardPrefab, entry.Find("CardHolder"));
                card.SetItem(item);
                card.draggable = false;
                card.onClick = c => ShowCardZoom(c.item);
                CombatPanel.Place(card.transform, itemCardScale);
                entry.Find("Owner").GetComponent<Text>().text = p.playerName + (equipment ? "（装备）" : "");
                var player = p;
                int slot = i;
                var hover = entry.gameObject.AddComponent<SetupHover>();
                hover.onHover = () =>
                {
                    if (!itemDetailText) return;
                    string why = WhyNotUsable(player, item, equipment);
                    itemDetailText.text = item.itemName + "：" + CombatPanel.ItemSummary(item) + (why != null ? "   【现在不能用：" + why + "】" : "");
                };
                var use = entry.Find("UseButton").GetComponent<Button>();
                use.onClick.AddListener(() =>
                {
                    if (!busy && !finished && WhyNotUsable(player, item, equipment) == null) StartCoroutine(Use(player, slot, equipment));
                });
                useButtons.Add(use);
                useOwners.Add(p);
                useItems.Add(item);
                useIsEquipment.Add(equipment);
            }
        }
        if (useButtons.Count == 0)
        {
            var entry = Instantiate(itemEntryTemplate, itemRow);
            entry.gameObject.SetActive(true);
            entry.Find("Owner").GetComponent<Text>().text = "（没有道具）";
            entry.Find("UseButton").gameObject.SetActive(false);
        }
        RefreshInteractable();
    }

    // Null if the item can be used right now, otherwise the reason it can't.
    string WhyNotUsable(CharacterCard player, ItemData item, bool equipment)
    {
        if (equipment)
        {
            if (player != speaker) return "装备只有本轮说话的玩家能用";
            if (item.mechanic == ItemMechanic.Escape) return CombatPanel.TokensOf(player).Count >= 3 ? null : "需要 3 枚 token";
            foreach (var injury in player.injuries)
                if (injury && (injury.special ? CombatPanel.TokensOf(player).Count > 0 : CombatPanel.Count(player, injury.element) > 0)) return null;
            return "没有能转移的负伤牌（需要负伤牌和同属性 token）";
        }
        switch (item.mechanic)
        {
            case ItemMechanic.SocialOnly:
            case ItemMechanic.Sachet:
            case ItemMechanic.Disguise:
            case ItemMechanic.Retreat:
                return null;
            case ItemMechanic.RainCharm: return CombatPanel.Count(player, Element.Metal) > 0 ? null : "没有金 token";
            case ItemMechanic.SpiceTea: return null;
            case ItemMechanic.Sugar: return player == speaker ? null : "只有本轮说话的玩家能用";
            case ItemMechanic.Rattle: return "还没有设定哪些社交对象是孩子或调皮的精怪";
            case ItemMechanic.Salve:
            case ItemMechanic.Panacea: return "在背包里点击这张卡使用";
            case ItemMechanic.NoEffect: return "没有使用效果";
            case ItemMechanic.ExtraTurn:
                if (player != speaker || !spoke) return "本轮说话的玩家说完后才能使用";
                return pool.Count > rounds - round ? null : "NPC 的候选池不够再说一次";
            case ItemMechanic.FailureGuard: return "社交失败时在结算画面使用";
        }
        return "只能在战斗中使用";
    }

    IEnumerator Use(CharacterCard player, int slot, bool equipment)
    {
        var item = slot >= 0 && slot < player.items.Count ? player.items[slot] : null;
        if (!item) yield break;
        busy = true;
        HideFieldButtons();
        used = true;
        string src = "  道具 " + player.playerName + " 使用 " + item.itemName;

        switch (item.mechanic)
        {
            case ItemMechanic.SocialOnly:
                favor += 5;
                log.Add(src + "：好感度 +5");
                break;
            case ItemMechanic.Sachet: yield return UseSachet(player, src); break;
            case ItemMechanic.Disguise: yield return UseDisguise(player, src); break;
            case ItemMechanic.RainCharm: yield return UseRainCharm(player, src); break;
            case ItemMechanic.SpiceTea: UseSpiceTea(player, src); break;
            case ItemMechanic.Sugar: yield return UseSugar(player, src); break;
            case ItemMechanic.Retreat: Retreat(src + "：全队安全撤离"); break;
            case ItemMechanic.ExtraTurn:
                spoke = false;
                extraTurn = true;
                log.Add(src + "：额外获得 1 回合，" + player.playerName + " 可以再说一次");
                break;
            case ItemMechanic.InjuryTransfer: yield return UseInjuryTransfer(player, src); break;
            case ItemMechanic.Escape: yield return UseEscape(player, src); break;
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
        int r = same ? 4 : Random.Range(1, 5);
        needLess += r;
        log.Add(src + "：所有选项所需好感度 -" + r + (same ? "（与目标种族相同，取最大值）" : "（1d4）"));
        yield break;
    }

    // 易容面具: for this event, the NPC takes the user for a species they pick.
    IEnumerator UseDisguise(CharacterCard player, string src)
    {
        var all = (Species[])System.Enum.GetValues(typeof(Species));
        var labels = new List<string>();
        var colors = new List<Color>();
        foreach (var s in all) { labels.Add(CombatPanel.SpeciesName(s)); colors.Add(Tan); }
        yield return Choose(player.playerName + "：让对方把你误认为哪个种族？", labels, colors);
        disguises[player] = all[choice];
        log.Add(src + "：本次事件中对方把 " + player.playerName + " 误认为" + CombatPanel.SpeciesName(all[choice]));
    }

    // 祈雨符: each 金 token spent gives 1d4 temporary 水 tokens (this event only).
    IEnumerator UseRainCharm(CharacterCard player, string src)
    {
        int have = CombatPanel.Count(player, Element.Metal);
        var labels = new List<string>();
        var colors = new List<Color>();
        for (int i = 1; i <= have; i++) { labels.Add("消耗 " + i + " 枚金"); colors.Add(ElementToken.ColorFor(Element.Metal)); }
        yield return Choose(player.playerName + "：消耗几枚金 token？（每枚换 1d4 枚临时水 token）", labels, colors);
        int n = choice + 1;
        int gained = 0;
        var rolls = new List<string>();
        for (int i = 0; i < n; i++)
        {
            CombatPanel.Spend(player, Element.Metal);
            int r = Random.Range(1, 5);
            rolls.Add(r.ToString());
            for (int k = 0; k < r; k++) gained += AddToken(player, Element.Water, true);   // 负伤 can block 水 tokens.
        }
        log.Add(src + "：消耗" + n + "枚金，获得" + gained + "枚临时水token（" + string.Join("+", rolls.ToArray()) + "）");
    }

    // 香料茶: 2d4 temporary 水 tokens (this event only).
    void UseSpiceTea(CharacterCard player, string src)
    {
        int a = Random.Range(1, 5), b = Random.Range(1, 5);
        int gained = 0;
        for (int k = 0; k < a + b; k++) gained += AddToken(player, Element.Water, true);   // 负伤 can block 水 tokens.
        log.Add(src + "：获得" + gained + "枚临时水token（" + a + "+" + b + "）");
    }

    // 糖: counts as 1 token of an element the speaker picks (a temporary token, this event only).
    IEnumerator UseSugar(CharacterCard player, string src)
    {
        var all = (Element[])System.Enum.GetValues(typeof(Element));
        var labels = new List<string>();
        var colors = new List<Color>();
        foreach (var e in all) { labels.Add(CombatCard.ElementChar(e)); colors.Add(ElementToken.ColorFor(e)); }
        yield return Choose(player.playerName + "：把糖当作哪种属性的 token？", labels, colors);
        var element = all[choice];
        int gained = AddToken(player, element, true);
        log.Add(src + "：当作 1 枚" + CombatCard.ElementChar(element) + " token" + (gained == 0 ? "（被负伤挡住了）" : ""));
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
            yield return InjuryDeck.AskInjury(player.playerName + "：选择要转移的负伤牌（每张消耗 1 枚同属性 token，特殊负伤消耗任意 1 枚）",
                cards, moved > 0 ? "完成" : null, x => choice = x);
            if (choice >= movable.Count) break;
            var injury = player.injuries[movable[choice]];

            var targets = new List<CharacterCard>();
            var labels = new List<string>();
            var colors = new List<Color>();
            foreach (var p in players) if (p != player) { targets.Add(p); labels.Add(p.playerName); colors.Add(Tan); }
            labels.Add("NPC"); colors.Add(Blue);
            yield return Choose("把" + injury.Title + "转移给谁？", labels, colors);
            int targetChoice = choice;

            if (injury.special)
            {
                // Any one token pays for the 特殊负伤.
                var have = new List<Element>();
                foreach (Element e in System.Enum.GetValues(typeof(Element))) if (CombatPanel.Count(player, e) > 0) have.Add(e);
                labels = new List<string>();
                colors = new List<Color>();
                foreach (var e in have) { labels.Add(CombatCard.ElementChar(e) + " ×" + CombatPanel.Count(player, e)); colors.Add(ElementToken.ColorFor(e)); }
                yield return Choose(player.playerName + "：消耗哪一枚 token 转移" + injury.Title + "？", labels, colors);
                CombatPanel.Spend(player, have[choice]);
            }
            else CombatPanel.Spend(player, injury.element);
            choice = targetChoice;
            player.injuries.Remove(injury);
            moved++;
            if (choice < targets.Count)
            {
                var target = targets[choice];
                log.Add(src + "：把" + injury.Title + "转移给 " + target.playerName);
                var deck = InjuryDeck.Find();
                if (deck) yield return deck.Give(target, injury, false, log);   // Their backpack may need room.
                else { target.injuries.Add(injury); target.Refresh(); }
            }
            else
            {
                int a = Random.Range(1, 5), b = Random.Range(1, 5);
                needLess += a + b;
                log.Add(src + "：把" + injury.Title + "转移给 NPC，2d4 = " + a + "+" + b
                        + "，所有选项所需好感度 -" + (a + b));
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
                labels.Add(CombatCard.ElementChar(e) + " ×" + CombatPanel.Count(player, e));
                colors.Add(ElementToken.ColorFor(e));
            }
            yield return Choose(player.playerName + "：选择要消耗的 token（" + (i + 1) + " / 3）", labels, colors);
            CombatPanel.Spend(player, options[choice]);
        }
        Retreat(src + "：全队安全撤退");
    }

    // The card's own bonus, e.g. a 九尾 lets 大妈 touch their tail: every outcome needs less.
    void UseSpeciesBonus()
    {
        if (busy || finished || bonusUsed || !social) return;
        bonusUsed = true;
        needLess += social.bonusAmount;
        log.Add("  特殊：" + social.bonusButton + "，所有选项所需好感度 -" + social.bonusAmount);
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
            text.text = "点击任意处关闭";
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
        log.Add("结束对抗：" + outcome.label);
        UpdateTexts();
        // Tokens the reward text promises ("每人2木token"…), then 负伤 like "上场并赢得一次社交事件后，可弃掉这张牌".
        var notes = new List<string>();
        var injuries = InjuryDeck.Find();
        yield return RewardRules.Give(outcome.reward, players, speakers, tokenPrefab, notes);
        SpeciesWinBonus(outcome, notes);
        if (injuries) notes.AddRange(injuries.OnWin(false));
        ShowResult(outcome.label, outcome.reward, string.Join("\n", notes.ToArray()), false);
    }

    void Lose(string reason)
    {
        if (finished) return;
        finished = true;
        failed = true;
        log.Add(reason + "，对抗失败");
        UpdateTexts();
        string next = social && social.failureCombat ? "\n继续后进入战斗【" + social.failureCombat.title + "】" : "";
        ShowResult("失败", social ? social.failureResult : "", reason + next, social != null);
    }

    void Retreat(string message)
    {
        log.Add(message);
        var injuries = InjuryDeck.Find();
        if (injuries) injuries.OnRetreat(players, log);   // "每次逃跑失去3主属性token"
        finished = true;
        UpdateTexts();
        ShowResult("撤退", "全队安全撤离了对话。", "", false);
    }

    // The result screen: outcome, 好感度, text, notes, and (on a loss) 失败保护 buttons.
    void ShowResult(string title, string description, string reward, bool offerFailureGuard)
    {
        HideFieldButtons();
        if (choiceRow) choiceRow.gameObject.SetActive(false);
        if (!resultView) return;
        resultTitle.text = title;
        resultTotal.text = "好感度：" + favor;
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
                    var b = MakeButton(rrt, p.playerName + " 使用失败保护", Gold, 330);
                    b.onClick.AddListener(() =>
                    {
                        if (continuing) return;
                        if (!protectedPlayers.Contains(player)) protectedPlayers.Add(player);
                        player.items[slot] = null;
                        player.Refresh();
                        resultReward.text += (resultReward.text.Length > 0 ? "\n" : "") + player.playerName + " 用失败保护抵挡了失败效果";
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
            if (combat) { combat.Open(next, next.title); yield break; }
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
        roundText.text = finished ? "对抗结束" : "第 " + round + " / " + rounds + " 轮";
        favorText.text = "好感度：" + favor;

        var sb = new StringBuilder();
        if (social)
            foreach (var outcome in social.outcomes)
                sb.Append(outcome.label).Append("：需 ≥").Append(Need(outcome))
                  .Append(favor >= Need(outcome) ? "【已达成】" : "").Append('\n');
        if (needLess > 0) sb.Append("（所有选项已 -").Append(needLess).Append("）");
        needText.text = social ? sb.ToString().TrimEnd('\n') : "还没有关联社交卡。";

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
            if (showBonus) bonusButton.GetComponentInChildren<Text>().text = social.bonusButton + "（所有选项 -" + social.bonusAmount + "）";
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
        if (poolLabel) poolLabel.text = "NPC 候选池（剩 " + pool.Count + " 枚，不放回）";
        if (drawText && !spoke && !busy && !finished)
            drawText.text = "第 " + round + " 轮倍率\n同属性 ×" + (round == 1 ? 5 : 2) + "　相生 ×" + (round >= rounds ? "½" : "1") + "\n其他：扣除下注";

        var logSb = new StringBuilder();
        foreach (var line in log) logSb.AppendLine(line);
        logText.text = logSb.ToString();
    }
}
