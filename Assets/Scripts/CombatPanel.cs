using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

// Runs one combat: 3 rounds, following the GDD's steps:
// ① pick the player who fights (their card and equipped weapon go onto the field)
// ② "出战" uses the weapon: pay its tokens, roll, work out the attribute  ③ apply 克制 and add it to the total
// ④ any player can use one-time items (shown along the bottom), each settled on its own
// ⑤ "结束本轮" moves to the next round; after round 3 the total is compared with 全胜 / 胜利.
// Each item's rules come from ItemData.mechanic (see ItemMechanic). d6 rolls use the fortune stick box.
public class CombatPanel : MonoBehaviour
{
    public int rounds = 3;
    public ItemCard itemCardPrefab;

    [Header("UI")]
    public CombatCard cardView;            // The enemy's combat card.
    public Text roundText;
    public RectTransform pickRow;          // One button per player.
    public Button pickButtonTemplate;      // Inactive template.
    public RectTransform fieldCardHolder;  // The fighting player's card goes here.
    public RectTransform weaponHolder;     // Their equipped weapon card goes here.
    public Text fieldHint;
    public Button fightButton;
    public Button endRoundButton;          // Optional; made from the fight button if left empty.
    public RectTransform itemRow;          // One-time items of all players.
    public RectTransform itemEntryTemplate;// Inactive template: CardHolder, Owner (Text), UseButton.
    public Text itemDetailText;            // What the hovered item does.
    public Text totalText;
    public Text resultText;                // During combat: enemy element and what's needed to win.
    public Text logText;
    public Button closeButton;
    public Button elementChartButton;      // Small 五行生克 chart; click to enlarge.
    public GameObject elementChartZoom;    // The enlarged chart; click anywhere to close.
    public float fieldCardScale = 0.6f;
    public float weaponCardScale = 0.75f;
    public float itemCardScale = 0.4f;
    public float zoomCardScale = 1.6f;     // A clicked item card is shown this big.

    [Header("Result screen")]
    public GameObject resultView;          // Covers the window once the 3 rounds are done.
    public Text resultTitle;
    public Text resultTotal;
    public Text resultDescription;
    public Text resultReward;
    public Button continueButton;
    public ElementToken tokenPrefab;       // For reward and temporary tokens.

    CombatData combat;
    int round;
    int total;
    bool busy;
    bool fought;            // The round's fighter has used their weapon; items can still be used before ending the round.
    bool finished;
    bool retreated;
    bool drawItemAfter;     // 脚底抹油: draw an item card once combat is closed.
    bool lost;              // The 失败 text's 负伤 are drawn when 继续 is pressed.
    bool continuing;
    CharacterCard fighter;
    readonly List<CharacterCard> fighters = new List<CharacterCard>();         // Who fought, in order.
    readonly List<CharacterCard> stagePaid = new List<CharacterCard>();        // On-stage 负伤 already settled this round.
    readonly List<CharacterCard> protectedPlayers = new List<CharacterCard>(); // Used 失败保护.

    // What can change during a fight.
    Element enemyElement;
    Element? enemyElementNextRound;   // 迷魂香
    int greatNeed, winNeed;           // 全胜 / 胜利 thresholds after 香囊 / 替伤符
    readonly Dictionary<CharacterCard, Species> disguises = new Dictionary<CharacterCard, Species>();   // 易容面具

    // The last dice roll, for 贡品 (reroll).
    class LastRoll { public CharacterCard player; public string itemName; public List<int> sides = new List<int>(); public int flat; public Element? attribute; public int value, final; }
    LastRoll lastRoll;

    readonly List<string> log = new List<string>();
    readonly List<CharacterCard> players = new List<CharacterCard>();
    readonly List<Button> pickButtons = new List<Button>();
    readonly List<Button> useButtons = new List<Button>();
    readonly List<CharacterCard> useOwners = new List<CharacterCard>();
    readonly List<ItemData> useItems = new List<ItemData>();
    RectTransform choiceRow;   // Built on first use: buttons for in-combat choices (which token, which target…).
    GameObject cardZoom;       // Built on first use: an enlarged item card; click anywhere to close.
    int choice = -1;
    Transform fortuneBox;
    int fortuneBoxIndex;

    static readonly Color Tan = new Color(0.88f, 0.82f, 0.68f);
    static readonly Color Gold = new Color(0.95f, 0.75f, 0.35f);

    // Read by the tutorial to know where the fight is.
    public CharacterCard Fighter => fighter;
    public bool Fought => fought;
    public bool Finished => finished;
    public int Round => round;

    void Awake()
    {
        // Once a combat / social is chosen it has to be played out (leaving early would not give spent tokens back).
        if (closeButton) closeButton.gameObject.SetActive(false);
        if (continueButton) continueButton.onClick.AddListener(Continue);
        if (fightButton) fightButton.onClick.AddListener(() =>
        {
            if (!busy && fighter && !fought && round <= rounds && StageBlock(fighter) == null)
                StartCoroutine(Use(fighter, fighter.equippedSlot, true));
        });

        // "结束本轮" sits where the fight button is: after the fighter has attacked, anyone can still
        // use items, and the round only ends when this is pressed (GDD step ⑤).
        if (!endRoundButton && fightButton)
        {
            endRoundButton = Instantiate(fightButton, fightButton.transform.parent);
            endRoundButton.name = "EndRoundButton";
            endRoundButton.onClick.RemoveAllListeners();
            endRoundButton.GetComponent<Image>().color = Tan;
        }
        if (endRoundButton) endRoundButton.onClick.AddListener(EndRound);

        if (elementChartButton && elementChartZoom)
            elementChartButton.onClick.AddListener(() => { elementChartZoom.SetActive(true); elementChartZoom.transform.SetAsLastSibling(); });
        var zoomButton = elementChartZoom ? elementChartZoom.GetComponent<Button>() : null;
        if (zoomButton) zoomButton.onClick.AddListener(() => elementChartZoom.SetActive(false));
    }

    public void Open(CombatData data, string fallbackTitle)
    {
        combat = data;
        round = 1;
        total = 0;
        busy = fought = finished = retreated = drawItemAfter = lost = continuing = false;
        fighter = null;
        lastRoll = null;
        log.Clear();
        disguises.Clear();
        fighters.Clear();
        stagePaid.Clear();
        protectedPlayers.Clear();
        var injuries = InjuryDeck.Find();
        if (injuries) injuries.BeginEvent();
        enemyElementNextRound = null;
        enemyElement = data ? data.element : Element.Wood;
        greatNeed = data ? data.greatSuccessThreshold : 0;
        winNeed = data ? data.successThreshold : 0;
        if (resultView) resultView.SetActive(false);
        if (elementChartZoom) elementChartZoom.SetActive(false);
        if (cardZoom) cardZoom.SetActive(false);

        cardView.gameObject.SetActive(data != null);
        if (data) cardView.SetCombat(data);

        // Equipment can't change once combat starts.
        players.Clear();
        players.AddRange(FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude));
        players.Sort((a, b) => string.Compare(a.playerName, b.playerName, System.StringComparison.Ordinal));
        foreach (var p in players) p.equipmentLocked = true;

        BuildPickButtons();
        BuildItemRow();
        ShowFighter(null);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        // Keep the stick box visible above this window while combat is open.
        var sticks = FindFirstObjectByType<FortuneSticks>();
        if (sticks)
        {
            fortuneBox = sticks.transform;
            fortuneBoxIndex = fortuneBox.GetSiblingIndex();
            fortuneBox.SetAsLastSibling();
        }
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
            foreach (var t in TokensOf(p)) if (t.temporary) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
        }
        if (fortuneBox) fortuneBox.SetSiblingIndex(fortuneBoxIndex);
        gameObject.SetActive(false);
    }

    void EndRound()
    {
        if (busy || !fought || round > rounds) return;
        fought = false;
        lastRoll = null;
        stagePaid.Clear();
        round++;
        // 迷魂香 changes the enemy's element for the next round only.
        enemyElement = combat ? combat.element : Element.Wood;
        if (enemyElementNextRound.HasValue)
        {
            enemyElement = enemyElementNextRound.Value;
            enemyElementNextRound = null;
            log.Add("  本轮敌方属性变为" + CombatCard.ElementChar(enemyElement) + "（迷魂香）");
        }
        if (round > rounds) Finish();
        ShowFighter(null);
        UpdateTexts();
    }

    // --- Picking the round's fighter ---

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
            button.onClick.AddListener(() => { if (!busy && !fought && round <= rounds) ShowFighter(player); });
            pickButtons.Add(button);
        }
    }

    // Puts the player's card and equipped weapon on the field (null clears the field).
    void ShowFighter(CharacterCard player)
    {
        fighter = player;
        Clear(fieldCardHolder);
        Clear(weaponHolder);

        if (player)
        {
            // A display-only copy of the character card.
            var copy = Instantiate(player.gameObject, fieldCardHolder);
            foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(mb is Graphic) && !(mb is LayoutGroup) && !(mb is LayoutElement) && !(mb is AspectRatioFitter)
                    && !(mb is Mask) && !(mb is RectMask2D) && !(mb is BaseMeshEffect))
                    DestroyImmediate(mb);
            var group = copy.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var crt = (RectTransform)copy.transform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.localScale = Vector3.one * fieldCardScale;
            ItemsPopup.HookCopiedItemsButton(copy, player);

            var weapon = player.Equipped;
            if (weapon)
            {
                var card = Instantiate(itemCardPrefab, weaponHolder);
                card.SetItem(weapon);
                card.draggable = false;
                card.onClick = c => ShowCardZoom(c.item);
                Place(card.transform, weaponCardScale);
                fightButton.GetComponentInChildren<Text>().text = "用 " + weapon.itemName + " 出战（" + Summary(weapon) + "）";
            }
        }
        ShowFieldButtons();
    }

    // Shows the right hint and button for where the round is.
    void ShowFieldButtons()
    {
        if (choiceRow) choiceRow.gameObject.SetActive(false);
        bool hasWeapon = fighter && fighter.Equipped;
        string block = fighter && !fought ? StageBlock(fighter) : null;
        fightButton.gameObject.SetActive(round <= rounds && fighter && !fought && hasWeapon && block == null);
        if (endRoundButton)
        {
            endRoundButton.gameObject.SetActive(round <= rounds && fought);
            endRoundButton.GetComponentInChildren<Text>().text = round < rounds ? "⑤ 结束本轮" : "⑤ 结束第 3 轮并结算";
        }
        if (round > rounds) fieldHint.text = "";
        else if (!fighter) fieldHint.text = "① 选择本轮上场的玩家";
        else if (fought) fieldHint.text = "④ 还可以使用道具，然后结束本轮";
        else if (block != null) fieldHint.text = block + "，请换人";
        else if (hasWeapon) fieldHint.text = "② " + fighter.playerName + " 上场，用 " + fighter.Equipped.itemName + " 出战（出战前可以换人）";
        else fieldHint.text = fighter.playerName + " 没有装备武器，本轮无法出战";
        RefreshInteractable();
    }

    // --- One-time items, usable by anyone at any time ---

    void BuildItemRow()
    {
        Clear(itemRow, itemEntryTemplate.transform);
        useButtons.Clear();
        useOwners.Clear();
        useItems.Clear();
        if (itemDetailText) itemDetailText.text = "鼠标移到道具上查看效果";
        foreach (var p in players)
        {
            for (int i = 0; i < p.items.Count; i++)
            {
                var item = p.items[i];
                if (!item || item.kind != ItemKind.Consumable) continue;
                var entry = Instantiate(itemEntryTemplate, itemRow);
                entry.gameObject.SetActive(true);
                var card = Instantiate(itemCardPrefab, entry.Find("CardHolder"));
                card.SetItem(item);
                card.draggable = false;
                card.onClick = c => ShowCardZoom(c.item);
                Place(card.transform, itemCardScale);
                entry.Find("Owner").GetComponent<Text>().text = p.playerName;
                var player = p;
                int slot = i;
                // Hovering an item shows what it does, and why it can't be used right now.
                var hover = entry.gameObject.AddComponent<SetupHover>();
                hover.onHover = () =>
                {
                    if (!itemDetailText) return;
                    string why = WhyNotUsable(player, item);
                    itemDetailText.text = item.itemName + "：" + ItemSummary(item) + (why != null ? "   【现在不能用：" + why + "】" : "");
                };
                var use = entry.Find("UseButton").GetComponent<Button>();
                use.onClick.AddListener(() => { if (!busy && round <= rounds && WhyNotUsable(player, item) == null) StartCoroutine(Use(player, slot, false)); });
                useButtons.Add(use);
                useOwners.Add(p);
                useItems.Add(item);
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

    // Shows a big copy of the item card over the window; a click anywhere closes it.
    void ShowCardZoom(ItemData item)
    {
        if (!item) return;
        if (!cardZoom)
        {
            cardZoom = new GameObject("CardZoom", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)cardZoom.transform;
            rt.SetParent(fightButton.transform.parent, false);
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
        Clear(cardZoom.transform, cardZoom.transform.Find("Hint"));
        var card = Instantiate(itemCardPrefab, cardZoom.transform);
        card.SetItem(item);
        card.draggable = false;
        card.onClick = c => cardZoom.SetActive(false);
        Place(card.transform, zoomCardScale);
        ((RectTransform)card.transform).anchoredPosition = new Vector2(0f, 20f);
        cardZoom.SetActive(true);
        cardZoom.transform.SetAsLastSibling();
    }

    // Null if the player may fight now, otherwise why not (负伤 like "无法在战斗中第一个出场").
    string StageBlock(CharacterCard p)
    {
        var deck = InjuryDeck.Find();
        return deck ? deck.StageBlock(p, true, fighters.Count == 0) : null;
    }

    // The fighter's 负伤 that change the combat value ("每次上场战斗值-1", "如果第一个出场，战斗值+3")
    // are settled once per round on stage.
    void OnStage(CharacterCard p)
    {
        bool first = fighters.Count == 0;
        if (!fighters.Contains(p)) fighters.Add(p);
        var deck = InjuryDeck.Find();
        if (!deck) return;
        deck.WentOnStage(p, round);
        if (stagePaid.Contains(p)) return;
        stagePaid.Add(p);
        total += deck.CombatOnStage(p, first, log, (v, e) => ApplyCounter(v, e, out _));
    }

    // Null if the item can be used right now, otherwise the reason it can't.
    string WhyNotUsable(CharacterCard player, ItemData item)
    {
        switch (item.mechanic)
        {
            case ItemMechanic.SocialOnly:
            case ItemMechanic.Sugar:
            case ItemMechanic.Rattle: return "只能在社交中使用";
            case ItemMechanic.Salve:
            case ItemMechanic.Panacea: return "在背包里点击这张卡使用";
            case ItemMechanic.NoEffect: return "没有使用效果";
            case ItemMechanic.FailureGuard: return "战斗失败时在结算画面使用";
            case ItemMechanic.ExtraTurn: return player == fighter && fought ? null : "本轮上场的玩家出战后才能使用";
            case ItemMechanic.Reroll:
                if (player == fighter) return "只能由未上场的玩家使用";
                return lastRoll != null ? null : "要在一次掷骰后立刻使用";
            case ItemMechanic.RainCharm: return Count(player, Element.Metal) > 0 ? null : "没有金 token";
            case ItemMechanic.Burst:
                foreach (Element e in System.Enum.GetValues(typeof(Element))) if (Count(player, e) == 0) return "需要五种属性的 token 各一枚";
                return null;
        }
        return null;
    }

    // --- Using an item ---

    // A weapon use is the round's fight; a one-time item is used up. Returns early (nothing spent)
    // when the item can't be used, e.g. not enough tokens.
    IEnumerator Use(CharacterCard player, int slot, bool isRoundFight)
    {
        var item = slot >= 0 && slot < player.items.Count ? player.items[slot] : null;
        if (!item) yield break;
        busy = true;
        HideFieldButtons();
        if (item.mechanic != ItemMechanic.Reroll) lastRoll = null;

        string who = isRoundFight ? "第" + round + "轮 " + player.playerName : "  道具 " + player.playerName;
        string src = who + " 使用 " + item.itemName;
        used = true;

        switch (item.mechanic)
        {
            case ItemMechanic.TokensTimesThree: yield return UseTokensTimesThree(player, src); break;
            case ItemMechanic.FiveElementCharm: yield return UseFiveElementCharm(player, src); break;
            case ItemMechanic.Escape: yield return UseEscape(player, src); break;
            case ItemMechanic.Retreat: Retreat(src + "：全队安全撤离", false); break;
            case ItemMechanic.IncenseBurner: yield return UseIncenseBurner(player, src); break;
            case ItemMechanic.BloodMist: yield return UseBloodMist(player, src); break;
            case ItemMechanic.Bewitching: yield return UseBewitching(player, src); break;
            case ItemMechanic.InjuryTransfer: yield return UseInjuryTransfer(player, src); break;
            case ItemMechanic.Sachet: yield return UseSachet(player, src); break;
            case ItemMechanic.ExtraTurn:
                fought = false;
                log.Add(src + "：获得额外回合，可以再出战一次");
                break;
            case ItemMechanic.RainCharm: yield return UseRainCharm(player, src); break;
            case ItemMechanic.SpiceTea: yield return UseSpiceTea(player, src); break;
            case ItemMechanic.Reroll: yield return UseReroll(src); break;
            case ItemMechanic.Disguise: yield return UseDisguise(player, src); break;
            case ItemMechanic.Burst: yield return UseBurst(player, src); break;
            case ItemMechanic.SocialOnly:
            case ItemMechanic.FailureGuard: used = false; break;
            default: yield return UseAuto(player, item, src); break;   // Auto, Pursuit (its own attack is plain)
        }

        if (used)
        {
            if (isRoundFight)
            {
                fought = true;
                OnStage(player);
            }
            else
            {
                player.items[slot] = null;   // One-time items are gone after use.
                player.Refresh();
            }
        }

        busy = false;
        if (!isRoundFight) BuildItemRow();
        if (fighter) ShowFighter(fighter); else ShowFieldButtons();
        UpdateTexts();
    }

    bool used;   // Set false by an item that turned out not usable (nothing was spent).

    void Fail(string message)
    {
        log.Add(message);
        used = false;
    }

    // Simple items: pay tokens, roll, attribute, 克制.
    IEnumerator UseAuto(CharacterCard player, ItemData item, string src)
    {
        var plan = ParsePlan(item);
        if (plan.manual) { log.Add(src + "：效果请按卡面结算"); yield break; }

        int primaryHave = Count(player, player.primaryElement);
        if (primaryHave < plan.costPrimary || TokensOf(player).Count < plan.costPrimary + plan.costAny)
        {
            Fail(src + "：token 不足，无法使用");
            yield break;
        }
        Element? spent = null;
        for (int i = 0; i < plan.costPrimary; i++) { Spend(player, player.primaryElement); spent = player.primaryElement; }
        for (int i = 0; i < plan.costAny; i++)
        {
            yield return ChooseToken(player, player.playerName + "：选择要消耗的 token", false);
            Spend(player, (Element)choice);
            spent = (Element)choice;
        }

        var roll = new LastRoll { player = player, itemName = item.itemName };
        var parts = new List<string>();
        yield return RollValue(plan.dice, roll, parts);

        Element? attribute = null;
        string note = "";
        if (plan.attr == "主") attribute = player.primaryElement;
        else if (plan.attr == "token") attribute = spent;
        else if (plan.attr == "rolled")
        {
            int r = 0;
            yield return RollDie(6, x => r = x);
            foreach (Match m in Regex.Matches(plan.rolledText, @"(\d)\s*=\s*([^，,。\s]+)"))
                if (int.Parse(m.Groups[1].Value) == r)
                    attribute = m.Groups[2].Value.Contains("主") ? player.primaryElement : ParseElement(m.Groups[2].Value);
            note = "（属性掷出 " + r + "）";
        }
        else attribute = ParseElement(plan.attr);

        string paid = plan.costPrimary + plan.costAny > 0 ? "（消耗" + (plan.costPrimary + plan.costAny) + "枚token）" : "";
        roll.attribute = attribute;
        yield return Hit(player, src + paid, roll.value, attribute, string.Join(" + ", parts.ToArray()) + note, roll);
    }

    // 破阵刀: every token spent is +3; the attribute is the element spent most (ties: random).
    IEnumerator UseTokensTimesThree(CharacterCard player, string src)
    {
        if (TokensOf(player).Count == 0) { Fail(src + "：没有 token，无法使用"); yield break; }
        var spent = new Dictionary<Element, int>();
        int n = 0;
        while (TokensOf(player).Count > 0)
        {
            yield return ChooseToken(player, player.playerName + "：选择要消耗的 token（已消耗 " + n + " 枚，每枚 +3）", n > 0);
            if (choice < 0) break;
            var e = (Element)choice;
            Spend(player, e);
            spent[e] = spent.TryGetValue(e, out int c) ? c + 1 : 1;
            n++;
        }
        int best = 0;
        var top = new List<Element>();
        foreach (var pair in spent)
        {
            if (pair.Value > best) { best = pair.Value; top.Clear(); }
            if (pair.Value == best) top.Add(pair.Key);
        }
        var attr = top[Random.Range(0, top.Count)];
        yield return Hit(player, src + "（消耗" + n + "枚token）", 3 * n, attr, n + "×3", null);
    }

    // 五行符: each token (at most one per element) is a separate +2 of that element.
    IEnumerator UseFiveElementCharm(CharacterCard player, string src)
    {
        if (TokensOf(player).Count == 0) { Fail(src + "：没有 token，无法使用"); yield break; }
        var usedElements = new List<Element>();
        while (true)
        {
            var options = new List<Element>();
            foreach (Element e in System.Enum.GetValues(typeof(Element)))
                if (!usedElements.Contains(e) && Count(player, e) > 0) options.Add(e);
            if (options.Count == 0) break;
            yield return ChooseElement(player.playerName + "：选择要消耗的 token（每种属性最多 1 枚，每枚 +2）", options, usedElements.Count > 0);
            if (choice < 0) break;
            var el = (Element)choice;
            Spend(player, el);
            usedElements.Add(el);
            yield return Hit(player, src + "（" + CombatCard.ElementChar(el) + " token）", 2, el, "2", null);
        }
    }

    // 脚底抹油: spend any 3 tokens, the team retreats; if 2+ of them counter the enemy, draw an item card.
    IEnumerator UseEscape(CharacterCard player, string src)
    {
        if (TokensOf(player).Count < 3) { Fail(src + "：需要 3 枚 token"); yield break; }
        int countering = 0;
        for (int i = 0; i < 3; i++)
        {
            yield return ChooseToken(player, player.playerName + "：选择要消耗的 token（" + (i + 1) + " / 3）", false);
            var e = (Element)choice;
            Spend(player, e);
            if (ElementRules.Counters(e, enemyElement)) countering++;
        }
        bool draw = countering >= 2;
        Retreat(src + "：全队安全撤退" + (draw ? "（" + countering + " 枚克制敌方，抽取 1 张道具）" : ""), draw);
    }

    void Retreat(string message, bool drawItem)
    {
        log.Add(message);
        var injuries = InjuryDeck.Find();
        if (injuries) injuries.OnRetreat(players, log);   // "每次逃跑失去3主属性token"
        retreated = true;
        drawItemAfter = drawItem;
        finished = true;
        ShowResult("撤退", "全队安全撤离了战斗。", drawItem ? "关闭后抽取 1 张道具卡" : "", false);
    }

    // 合香炉: spend 3 tokens of one element for +6 of it; if that element counters the enemy,
    // another 5 (this extra 5 isn't doubled by 克制).
    IEnumerator UseIncenseBurner(CharacterCard player, string src)
    {
        var options = new List<Element>();
        foreach (Element e in System.Enum.GetValues(typeof(Element))) if (Count(player, e) >= 3) options.Add(e);
        if (options.Count == 0) { Fail(src + "：需要 3 枚相同属性的 token"); yield break; }
        yield return ChooseElement(player.playerName + "：选择要消耗 3 枚的属性", options, false);
        var el = (Element)choice;
        for (int i = 0; i < 3; i++) Spend(player, el);
        yield return Hit(player, src + "（消耗3枚" + CombatCard.ElementChar(el) + "）", 6, el, "6", null);
        if (ElementRules.Counters(el, enemyElement))
            yield return Hit(player, src + "（克制敌方，额外伤害）", 5, el, "5", null, false);
    }

    // 血雾: draw an injury card, +5 of its element.
    IEnumerator UseBloodMist(CharacterCard player, string src)
    {
        InjuryData injury = null;
        yield return DrawInjury(player, x => injury = x);
        if (!injury) { Fail(src + "：没有负伤牌可抽"); yield break; }
        yield return Hit(player, src + "（抽到" + CombatCard.ElementChar(injury.element) + "负伤）", 5, injury.element, "5", null);
    }

    // 迷魂香: draw an injury card; next round the enemy's element becomes its element.
    IEnumerator UseBewitching(CharacterCard player, string src)
    {
        InjuryData injury = null;
        yield return DrawInjury(player, x => injury = x);
        if (!injury) { Fail(src + "：没有负伤牌可抽"); yield break; }
        yield return Hit(player, src + "（抽到" + CombatCard.ElementChar(injury.element) + "负伤）", 2, injury.element, "2", null);
        enemyElementNextRound = injury.element;
        log.Add("  下回合敌方属性变为" + CombatCard.ElementChar(injury.element));
    }

    // 替伤符: per token spent, move one injury card of that element to a teammate or the enemy
    // (to the enemy: 全胜 / 胜利 need 2d4 less each time).
    IEnumerator UseInjuryTransfer(CharacterCard player, string src)
    {
        int moved = 0;
        while (true)
        {
            var movable = new List<int>();
            for (int i = 0; i < player.injuries.Count; i++)
                if (player.injuries[i].special ? TokensOf(player).Count > 0 : Count(player, player.injuries[i].element) > 0) movable.Add(i);
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
            labels.Add("敌人"); colors.Add(new Color(0.86f, 0.55f, 0.45f));
            yield return Choose("把" + injury.Title + "转移给谁？", labels, colors);
            int targetChoice = choice;

            if (injury.special)
            {
                yield return ChooseToken(player, player.playerName + "：消耗哪一枚 token 转移" + injury.Title + "？", false, null);
                Spend(player, (Element)choice);
            }
            else Spend(player, injury.element);
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
                int a = 0, b = 0;
                yield return RollDie(4, x => a = x);
                yield return RollDie(4, x => b = x);
                LowerThresholds(a + b);
                log.Add(src + "：把" + injury.Title + "转移给敌人，2d4 = " + a + "+" + b
                        + "，全胜 / 胜利条件各 -" + (a + b));
            }
            player.Refresh();
        }
        if (moved == 0) Fail(src + "：没有能转移的负伤牌（需要负伤牌和同属性 token）");
    }

    // 香囊: 全胜 / 胜利 need 1d4 less; if the user (or their disguise) is the enemy's species, the full 4.
    IEnumerator UseSachet(CharacterCard player, string src)
    {
        var species = disguises.TryGetValue(player, out var d) ? d : player.species;
        bool same = combat && combat.hasSpecies && combat.species == species;
        int r = 4;
        if (!same) yield return RollDie(4, x => r = x);
        LowerThresholds(r);
        log.Add(src + "：胜利和全胜条件 -" + r + (same ? "（与目标种族相同，取最大值）" : "（1d4）"));
    }

    // 祈雨符: each 金 token spent gives 1d4 temporary 水 tokens (this event only).
    IEnumerator UseRainCharm(CharacterCard player, string src)
    {
        int have = Count(player, Element.Metal);
        var labels = new List<string>();
        var colors = new List<Color>();
        for (int i = 1; i <= have; i++) { labels.Add("消耗 " + i + " 枚金"); colors.Add(ElementToken.ColorFor(Element.Metal)); }
        yield return Choose(player.playerName + "：消耗几枚金 token？（每枚换 1d4 枚临时水 token）", labels, colors);
        int n = choice + 1;
        int gained = 0;
        var rolls = new List<string>();
        for (int i = 0; i < n; i++)
        {
            Spend(player, Element.Metal);
            int r = 0;
            yield return RollDie(4, x => r = x);
            rolls.Add(r.ToString());
            for (int k = 0; k < r; k++) gained += AddToken(player, Element.Water, true);   // 负伤 can block 水 tokens.
        }
        log.Add(src + "：消耗" + n + "枚金，获得" + gained + "枚临时水token（" + string.Join("+", rolls.ToArray()) + "）");
    }

    // 香料茶: 2d4 temporary 水 tokens (this event only).
    IEnumerator UseSpiceTea(CharacterCard player, string src)
    {
        int a = 0, b = 0;
        yield return RollDie(4, x => a = x);
        yield return RollDie(4, x => b = x);
        int gained = 0;
        for (int k = 0; k < a + b; k++) gained += AddToken(player, Element.Water, true);   // 负伤 can block 水 tokens.
        log.Add(src + "：获得" + gained + "枚临时水token（" + a + "+" + b + "）");
    }

    // 贡品: roll the last roll's dice again.
    IEnumerator UseReroll(string src)
    {
        var last = lastRoll;
        if (last == null) { Fail(src + "：没有可以重掷的骰子"); yield break; }
        int value = last.flat;
        var parts = new List<string>();
        foreach (var sides in last.sides)
        {
            int r = 0;
            yield return RollDie(sides, x => r = x);
            value += r;
            parts.Add("d" + sides + "→" + r);
        }
        int final = ApplyCounter(value, last.attribute, out string counterNote);
        total += final - last.final;
        log.Add(src + "：重掷 " + last.player.playerName + " 的 " + last.itemName + "，" + string.Join(" + ", parts.ToArray())
                + " = " + value + "，" + counterNote + " → " + last.final + " 改为 " + final);
        last.value = value;
        last.final = final;
        lastRoll = last;   // Another 贡品 can reroll again.
    }

    // 易容面具: for this event, the enemy takes the user for a species they pick.
    IEnumerator UseDisguise(CharacterCard player, string src)
    {
        var all = (Species[])System.Enum.GetValues(typeof(Species));
        var labels = new List<string>();
        var colors = new List<Color>();
        foreach (var s in all) { labels.Add(SpeciesName(s)); colors.Add(Tan); }
        yield return Choose(player.playerName + "：让对方把你误认为哪个种族？", labels, colors);
        disguises[player] = all[choice];
        log.Add(src + "：本次事件中对方把 " + player.playerName + " 误认为" + SpeciesName(all[choice]));
    }

    // 单次高强度输出: one token of each element, 10 of your primary element.
    IEnumerator UseBurst(CharacterCard player, string src)
    {
        foreach (Element e in System.Enum.GetValues(typeof(Element))) Spend(player, e);
        yield return Hit(player, src + "（消耗五种属性token各一枚）", 10, player.primaryElement, "10", null);
    }

    // --- Settling a hit ---

    // Adds one hit to the total: applies 克制 against the enemy's current element (unless `counter` is false),
    // logs it, and lets 追击刃 holders follow up when the hit was countered.
    IEnumerator Hit(CharacterCard player, string source, int value, Element? attribute, string detail, LastRoll roll, bool counter = true)
    {
        string counterNote = "不计克制";
        int final = counter ? ApplyCounter(value, attribute, out counterNote) : value;
        total += final;
        log.Add(source + "：" + detail + " = " + value + "，" + (attribute.HasValue ? CombatCard.ElementChar(attribute.Value) + "属性" : "无属性")
                + "，" + counterNote + " → +" + final);
        if (roll != null && roll.sides.Count > 0) { roll.final = final; lastRoll = roll; }
        UpdateTexts();

        if (counter && attribute.HasValue && combat && ElementRules.Counters(enemyElement, attribute.Value))
            yield return OfferPursuit(player);
    }

    // The 克制 relationship is bold in the log when the attack counters the enemy.
    int ApplyCounter(int value, Element? attribute, out string note)
    {
        note = "×1";
        if (!attribute.HasValue || !combat) return value;
        string ours = CombatCard.ElementChar(attribute.Value), theirs = CombatCard.ElementChar(enemyElement);
        if (ElementRules.Counters(attribute.Value, enemyElement)) { note = "<b>×2（" + ours + "克" + theirs + "）</b>"; return value * 2; }
        if (ElementRules.Counters(enemyElement, attribute.Value)) { note = "×½（" + theirs + "克" + ours + "）"; return Mathf.FloorToInt(value / 2f); }
        return value;
    }

    // 追击刃: when a teammate's hit was countered by the enemy, each other player with 追击刃 equipped
    // may spend any 1 token to hit for 2 of that token's element. No limit on how often.
    IEnumerator OfferPursuit(CharacterCard hitter)
    {
        foreach (var q in players)
        {
            if (q == hitter || !q.Equipped || q.Equipped.mechanic != ItemMechanic.Pursuit || TokensOf(q).Count == 0) continue;
            yield return ChooseToken(q, q.playerName + " 的追击刃：队友的攻击被克制了，消耗 1 枚 token 追击？", true, "不追击");
            if (choice < 0) continue;
            var e = (Element)choice;
            Spend(q, e);
            yield return Hit(q, "  追击 " + q.playerName + " 追击刃（消耗1枚" + CombatCard.ElementChar(e) + "）", 2, e, "2", null);
        }
    }

    IEnumerator RollValue(string dice, LastRoll roll, List<string> parts)
    {
        foreach (Match m in Regex.Matches(dice ?? "", @"([+-]?)\s*(\d*)\s*[dD]\s*(\d+)|([+-]?)\s*(\d+)"))
        {
            if (m.Groups[3].Success)
            {
                int count = m.Groups[2].Value == "" ? 1 : int.Parse(m.Groups[2].Value);
                int sides = int.Parse(m.Groups[3].Value);
                for (int i = 0; i < count; i++)
                {
                    int r = 0;
                    yield return RollDie(sides, x => r = x);
                    roll.value += r;
                    roll.sides.Add(sides);
                    parts.Add("d" + sides + "→" + r);
                }
            }
            else
            {
                int flat = int.Parse(m.Groups[5].Value) * (m.Groups[4].Value == "-" ? -1 : 1);
                roll.value += flat;
                roll.flat += flat;
                parts.Add(flat.ToString());
            }
        }
    }

    IEnumerator RollDie(int sides, System.Action<int> result)
    {
        var sticks = FindFirstObjectByType<FortuneSticks>();
        if (sides == 6 && sticks)
        {
            while (sticks.IsRolling) yield return null;
            bool done = false;
            sticks.Roll(r => { result(r); done = true; });
            while (!done) yield return null;
        }
        else result(Random.Range(1, sides + 1));
    }

    void LowerThresholds(int amount)
    {
        greatNeed = Mathf.Max(0, greatNeed - amount);
        winNeed = Mathf.Max(0, winNeed - amount);
    }

    // Draws a 负伤 card for the player (its "抽到时" effect and a full backpack are handled by the deck).
    IEnumerator DrawInjury(CharacterCard player, System.Action<InjuryData> result)
    {
        var deck = InjuryDeck.Find();
        var injury = deck ? deck.Draw() : null;
        if (injury) yield return deck.Give(player, injury, true, log);
        result(injury);
    }

    // --- In-combat choices ---

    // Shows one button per label where the fight button sits and waits for a click (index in `choice`).
    IEnumerator Choose(string hint, List<string> labels, List<Color> colors)
    {
        if (!choiceRow)
        {
            var go = new GameObject("ChoiceRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            choiceRow = (RectTransform)go.transform;
            choiceRow.SetParent(fightButton.transform.parent, false);
            var frt = (RectTransform)fightButton.transform;
            choiceRow.anchorMin = choiceRow.anchorMax = frt.anchorMin;
            choiceRow.anchoredPosition = frt.anchoredPosition;
            choiceRow.sizeDelta = new Vector2(760, 60);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false; layout.childControlHeight = false;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
        }
        HideFieldButtons();
        Clear(choiceRow);
        choiceRow.gameObject.SetActive(true);
        choiceRow.SetAsLastSibling();
        fieldHint.text = hint;
        choice = -1;
        float width = Mathf.Min(170f, (760f - 8f * (labels.Count - 1)) / Mathf.Max(1, labels.Count));
        for (int i = 0; i < labels.Count; i++)
        {
            var b = Instantiate(pickButtonTemplate, choiceRow);
            b.gameObject.SetActive(true);
            b.interactable = true;
            ((RectTransform)b.transform).sizeDelta = new Vector2(width, 56);
            b.GetComponent<Image>().color = colors[i];
            b.GetComponentInChildren<Text>().text = labels[i];
            int index = i;
            b.onClick.AddListener(() => choice = index);
        }
        while (choice < 0) yield return null;
        choiceRow.gameObject.SetActive(false);
    }

    // Pick one of the player's tokens. `choice` ends up as the Element, or -1 for the extra "done/skip" button.
    IEnumerator ChooseToken(CharacterCard player, string hint, bool allowDone, string doneLabel = "完成")
    {
        var options = new List<Element>();
        foreach (Element e in System.Enum.GetValues(typeof(Element))) if (Count(player, e) > 0) options.Add(e);
        var labels = new List<string>();
        var colors = new List<Color>();
        foreach (var e in options) { labels.Add(CombatCard.ElementChar(e) + " ×" + Count(player, e)); colors.Add(ElementToken.ColorFor(e)); }
        if (allowDone) { labels.Add(doneLabel); colors.Add(Tan); }
        yield return Choose(hint, labels, colors);
        choice = choice < options.Count ? (int)options[choice] : -1;
    }

    // Pick one element from a list. `choice` ends up as the Element, or -1 for "完成".
    IEnumerator ChooseElement(string hint, List<Element> options, bool allowDone)
    {
        var labels = new List<string>();
        var colors = new List<Color>();
        foreach (var e in options) { labels.Add(CombatCard.ElementChar(e)); colors.Add(ElementToken.ColorFor(e)); }
        if (allowDone) { labels.Add("完成"); colors.Add(Tan); }
        yield return Choose(hint, labels, colors);
        choice = choice < options.Count ? (int)options[choice] : -1;
    }

    void HideFieldButtons()
    {
        fightButton.gameObject.SetActive(false);
        if (endRoundButton) endRoundButton.gameObject.SetActive(false);
        RefreshInteractable();
    }

    // --- Tokens ---

    public static List<ElementToken> TokensOf(CharacterCard player)
    {
        var list = new List<ElementToken>();
        if (player.tokenArea)
            foreach (var t in player.tokenArea.GetComponentsInChildren<ElementToken>())
                if (t.gameObject.activeSelf) list.Add(t);
        return list;
    }

    public static int Count(CharacterCard player, Element element)
    {
        int n = 0;
        foreach (var t in TokensOf(player)) if (t.element == element) n++;
        return n;
    }

    // Spends a token of this element; temporary ones go first.
    public static void Spend(CharacterCard player, Element element)
    {
        ElementToken pick = null;
        foreach (var t in TokensOf(player))
            if (t.element == element && (pick == null || t.temporary)) pick = t;
        if (!pick) return;
        pick.gameObject.SetActive(false);
        Destroy(pick.gameObject);
    }

    int AddToken(CharacterCard player, Element element, bool temporary)
    {
        return player.GainToken(element, tokenPrefab, temporary);
    }

    // --- End of combat ---

    void Finish()
    {
        finished = true;
        string title, description;
        bool won;
        if (!combat || (combat.greatSuccessThreshold <= 0 && combat.successThreshold <= 0))
        {
            title = "战斗结束"; description = "这张战斗卡还没有设置门槛。"; won = false;
        }
        else if (total >= greatNeed)
        {
            title = "全胜！"; description = combat.greatSuccessReward; won = true;
        }
        else if (total >= winNeed)
        {
            title = "胜利！"; description = combat.successReward; won = true;
        }
        else
        {
            title = "失败"; description = combat.failureResult; won = false;
        }
        string reward = "";
        if (won)
        {
            // 武士: "当你参与的战斗胜利时，获得1主属性token" — every 武士 who went on stage in this combat.
            foreach (var p in fighters)
            {
                if (!p || !p.characterClass || p.characterClass.className != "武士") continue;
                int got = AddToken(p, p.primaryElement, false);
                reward += (reward.Length > 0 ? "\n" : "") + p.playerName + "（武士）获得 " + got + " 枚" + CombatCard.ElementChar(p.primaryElement) + " token";
            }
            // 负伤 that heal after winning a fight. (Primary-element tokens come with each new main story card, not here.)
            var injuries = InjuryDeck.Find();
            if (injuries) foreach (var line in injuries.OnWin(true)) reward += (reward.Length > 0 ? "\n" : "") + line;
        }
        lost = !won && combat;
        ShowResult(title, description, reward, lost);
        if (won) StartCoroutine(GiveCardRewards(description));
    }

    // Tokens the card's reward text promises ("获1土token"…) are handed out on the result screen.
    IEnumerator GiveCardRewards(string text)
    {
        var lines = new List<string>();
        var injuries = InjuryDeck.Find();
        yield return RewardRules.Give(text, players, injuries ? injuries.OnStage : (IReadOnlyList<CharacterCard>)fighters, tokenPrefab, lines);
        foreach (var line in lines) resultReward.text += "\n" + line;
    }

    // The clean result screen: outcome, total, text, reward, and (on a loss) 失败保护 buttons.
    void ShowResult(string title, string description, string reward, bool offerFailureGuard)
    {
        if (!resultView) return;
        resultTitle.text = title;
        resultTotal.text = "战斗总值：" + total;
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
                    var b = Instantiate(pickButtonTemplate, rrt);
                    b.gameObject.SetActive(true);
                    b.interactable = true;
                    ((RectTransform)b.transform).sizeDelta = new Vector2(330, 56);
                    b.GetComponent<Image>().color = Gold;
                    b.GetComponentInChildren<Text>().text = p.playerName + " 使用失败保护";
                    var player = p;
                    int slot = i;
                    b.onClick.AddListener(() =>
                    {
                        if (continuing) return;
                        player.items[slot] = null;
                        player.Refresh();
                        if (!protectedPlayers.Contains(player)) protectedPlayers.Add(player);
                        resultReward.text += (resultReward.text.Length > 0 ? "\n" : "") + player.playerName + " 用失败保护抵挡了失败效果";
                        Destroy(b.gameObject);
                    });
                }
            }
        }
        resultView.SetActive(true);
        resultView.transform.SetAsLastSibling();
    }

    void GiveEveryonePrimaryToken()
    {
        foreach (var p in players) AddToken(p, p.primaryElement, false);
    }

    // After the result: on a loss, the 失败 text's 负伤 are drawn first; then 负伤 that heal at the end of
    // an event are settled. Then close combat and take the finished event card off the table.
    // Once every drawn event is dealt with, turn to the next main story card.
    void Continue()
    {
        if (!continuing) StartCoroutine(ContinueRoutine());
    }

    IEnumerator ContinueRoutine()
    {
        continuing = true;
        var injuries = InjuryDeck.Find();
        if (injuries)
        {
            if (lost) yield return injuries.ApplyFailure(combat.failureResult, players, protectedPlayers, combat.special);
            yield return injuries.EndEvent(players);
        }
        bool draw = drawItemAfter;
        Close();
        var events = FindFirstObjectByType<EventPopup>(FindObjectsInactive.Include);
        if (events) events.ResolveCurrentEvent();
        if (draw)
        {
            var items = FindFirstObjectByType<ItemDeck>();
            if (items) items.Draw();   // 脚底抹油's reward: then click a player to give them the card.
        }
        var story = FindFirstObjectByType<StoryCard>();
        if (story) story.AdvanceWhenEventsDone();
    }

    void RefreshInteractable()
    {
        bool open = !busy && !finished && round <= rounds;
        for (int i = 0; i < pickButtons.Count; i++)
        {
            pickButtons[i].interactable = open && !fought;   // The fighter can't change after attacking.
            pickButtons[i].GetComponent<Image>().color = players[i] == fighter ? Gold : Tan;
        }
        fightButton.interactable = open && !fought && fighter && fighter.Equipped && StageBlock(fighter) == null;
        if (endRoundButton) endRoundButton.interactable = open && fought;
        for (int i = 0; i < useButtons.Count; i++)
            useButtons[i].interactable = open && WhyNotUsable(useOwners[i], useItems[i]) == null;
    }

    void UpdateTexts()
    {
        roundText.text = round <= rounds ? "第 " + round + " / " + rounds + " 轮" : "战斗结束";
        totalText.text = "战斗总值：" + total;
        if (combat && !finished)
            resultText.text = "敌方属性：" + CombatCard.ElementChar(enemyElement)
                + (enemyElement != combat.element ? "（本轮被改变）" : "")
                + (enemyElementNextRound.HasValue ? "，下回合变为" + CombatCard.ElementChar(enemyElementNextRound.Value) : "")
                + "\n全胜需 ≥" + greatNeed + "   胜利需 ≥" + winNeed;
        else if (!combat) resultText.text = "还没有关联战斗卡。";
        var sb = new StringBuilder();
        foreach (var line in log) sb.AppendLine(line);
        logText.text = sb.ToString();
    }

    // --- Reading simple effects from the card text ---

    // Supports "+1d6主属性战斗值", "+4木属性战斗值", "+1d4token属性战斗值", "加1土属性战斗值", "2主属性战斗值",
    // "战斗值加成：+4 / 战斗值属性：通过投掷1d6决定。1=金，…", and token costs like
    // "消耗角色主属性token1枚" / "消耗1主属性token" / "消耗任意1枚token" / "消耗1token".
    class Plan
    {
        public bool manual;
        public int costPrimary, costAny;
        public string dice = "";
        public string attr = "";        // "主", "token", an element char, "rolled", or "" for none
        public string rolledText = "";
    }

    static Plan ParsePlan(ItemData item)
    {
        var plan = new Plan();
        if (!item) { plan.manual = true; return plan; }

        // Older cards: separate "战斗值加成" / "战斗值属性" lines.
        string bonus = Bonus(item);
        if (!string.IsNullOrEmpty(bonus))
        {
            plan.dice = bonus;
            string a = AttributeText(item);
            if (Regex.IsMatch(a ?? "", @"\d\s*=")) { plan.attr = "rolled"; plan.rolledText = a; }
            else { var e = ParseElement(a); plan.attr = e.HasValue ? CombatCard.ElementChar(e.Value) : ""; }
            return plan;
        }

        var sb = new StringBuilder();
        foreach (var e in item.effects) sb.Append(e.value).Append('。');
        string text = sb.ToString().Replace("​", "");

        var m = Regex.Match(text, @"消耗(?:角色)?主属性\s*token\s*(\d+)\s*枚");
        if (!m.Success) m = Regex.Match(text, @"消耗(\d+)\s*主属性\s*token");
        if (m.Success) plan.costPrimary = int.Parse(m.Groups[1].Value);
        m = Regex.Match(text, @"消耗任意(\d+)\s*枚\s*token");
        if (!m.Success) m = Regex.Match(text, @"消耗(\d+)\s*token");
        if (m.Success) plan.costAny = int.Parse(m.Groups[1].Value);

        m = Regex.Match(text, @"战斗值加成[:：]\s*([+-]?\s*\d*\s*[dD]?\s*\d+)");
        if (m.Success)
        {
            plan.dice = m.Groups[1].Value;
            var a = Regex.Match(text, @"战斗值属性[:：]([^。]*。[^。]*)");
            if (a.Success && Regex.IsMatch(a.Groups[1].Value, @"\d\s*=")) { plan.attr = "rolled"; plan.rolledText = a.Groups[1].Value; }
            return plan;
        }
        m = Regex.Match(text, @"(?:^|[+加，。,])\s*(\d*\s*[dD]\s*\d+|\d+)\s*(主|token|木|火|土|金|水)?\s*属性战斗值");
        if (!m.Success) { plan.manual = true; return plan; }
        plan.dice = m.Groups[1].Value;
        plan.attr = m.Groups[2].Value;
        return plan;
    }

    // Short text for the fight button.
    static string Summary(ItemData item)
    {
        switch (item.mechanic)
        {
            case ItemMechanic.TokensTimesThree: return "每消耗1枚token +3";
            case ItemMechanic.FiveElementCharm: return "每种属性token +2";
            case ItemMechanic.Escape: return "消耗3枚token撤退";
            case ItemMechanic.IncenseBurner: return "消耗3枚同属性token +6";
            case ItemMechanic.BloodMist: return "抽负伤牌 +5";
            case ItemMechanic.Bewitching: return "抽负伤牌，下回合改敌方属性";
            case ItemMechanic.InjuryTransfer: return "转移负伤牌";
            case ItemMechanic.Disguise: return "选择伪装的种族";
        }
        var plan = ParsePlan(item);
        if (plan.manual) return "按卡面结算";
        string attr = plan.attr == "主" ? "主属性" : plan.attr == "token" ? "token属性" : plan.attr == "rolled" ? "掷骰定属性"
            : plan.attr == "" ? "无属性" : plan.attr + "属性";
        string cost = (plan.costPrimary > 0 ? "，耗" + plan.costPrimary + "主属性token" : "")
            + (plan.costAny > 0 ? "，耗" + plan.costAny + "任意token" : "");
        return plan.dice.Replace("+", "").Trim() + " " + attr + cost;
    }

    public static string SpeciesName(Species s)
    {
        switch (s)
        {
            case Species.Taoyi: return "饕裔";
            case Species.Fox: return "九尾";
            case Species.Baize: return "白泽";
            case Species.Human: return "人";
        }
        return s.ToString();
    }

    // --- Helpers ---

    public static void Place(Transform card, float scale)
    {
        var rt = (RectTransform)card;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one * scale;
    }

    public static void Clear(Transform parent, Transform keep = null)
    {
        foreach (Transform child in parent)
        {
            if (child == keep) continue;
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public static string ItemSummary(ItemData item)
    {
        var sb = new StringBuilder(item.description.Replace("\n", " "));
        foreach (var e in item.effects) sb.Append("  ").Append(e.label).Append("：").Append(e.value.Replace("\n", " "));
        return sb.ToString();
    }

    // Item effects can use Chinese or English labels.
    static string Bonus(ItemData item)
    {
        string v = EffectValue(item, "战斗值加成");
        return string.IsNullOrEmpty(v) ? EffectValue(item, "Combat Bonus") : v;
    }

    static string AttributeText(ItemData item)
    {
        string v = EffectValue(item, "战斗值属性");
        return string.IsNullOrEmpty(v) ? EffectValue(item, "Combat Attribute") : v;
    }

    // "火" / "Fire" → Fire; "无属性" / "None" / anything else → no attribute.
    static Element? ParseElement(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        text = text.Trim();
        foreach (Element e in System.Enum.GetValues(typeof(Element)))
            if (text.StartsWith(CombatCard.ElementChar(e)) || text.StartsWith(e.ToString(), System.StringComparison.OrdinalIgnoreCase))
                return e;
        return null;
    }

    static string EffectValue(ItemData item, string label)
    {
        if (!item) return "";
        foreach (var e in item.effects)
            if (e.label != null && e.label.Trim().Equals(label, System.StringComparison.OrdinalIgnoreCase))
                return e.value;
        return "";
    }
}
