using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// The 负伤 (injury) pile, and the rules written on the injury cards (负伤规则 + the 负伤 sheet).
// Injuries share the 6 backpack slots with items. What a card says about when it heals or what it does
// is read from its effect text here: when drawn, going on stage, winning, retreating, a new main story card,
// the end of an event, or paying tokens to get rid of it.
public class InjuryDeck : MonoBehaviour
{
    public List<InjuryData> cards = new List<InjuryData>();
    public ElementToken tokenPrefab;   // For tokens a healed injury gives.

    readonly List<InjuryData> pile = new List<InjuryData>();

    // Events are the combat / social encounters. "下一次事件" and "一次事件完全不上场" count these.
    int eventNumber;
    bool inEvent;
    readonly List<CharacterCard> onStage = new List<CharacterCard>();   // Who went on stage this event, in order.

    public static InjuryDeck Find() => FindFirstObjectByType<InjuryDeck>();

    public IReadOnlyList<CharacterCard> OnStage => onStage;

    void Start()
    {
        Refill();
    }

    void Refill()
    {
        pile.Clear();
        foreach (var card in cards)
            if (card) for (int c = 0; c < card.copiesInPile; c++) pile.Add(card);
        for (int i = pile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pile[i], pile[j]) = (pile[j], pile[i]);
        }
    }

    // Draws the top card, or the top card of one element (e.g. "抽取一张水属性负伤卡").
    public InjuryData Draw(Element? element = null)
    {
        if (pile.Count == 0) Refill();
        for (int i = pile.Count - 1; i >= 0; i--)
        {
            if (element.HasValue && pile[i].element != element.Value) continue;
            var card = pile[i];
            pile.RemoveAt(i);
            return card;
        }
        // None of that element left in the pile: take one from the full set.
        var all = cards.FindAll(c => c && (!element.HasValue || c.element == element.Value));
        return all.Count > 0 ? all[Random.Range(0, all.Count)] : null;
    }

    // How an injury card looks on a choice button: its element's colour, with the element and the effect written on it.
    public static string CardLabel(InjuryData x)
        => x.special ? "<b>" + x.Title + "</b>（特殊）\n" + x.effect : "<b>" + CombatCard.ElementChar(x.element) + "属性负伤</b>\n" + x.effect;
    public static Color CardColor(InjuryData x)
        => x.special ? new Color(0.6f, 0.56f, 0.52f) : ElementToken.ColorFor(x.element);

    // Asks the player to pick one of these injury cards (plus an optional extra button such as 取消 / 完成).
    // `result` gets the index; the extra button is index cards.Count.
    public static IEnumerator AskInjury(string question, List<InjuryData> cards, string extra, System.Action<int> result)
    {
        var labels = new List<string>();
        var colors = new List<Color>();
        foreach (var x in cards) { labels.Add(CardLabel(x)); colors.Add(CardColor(x)); }
        if (!string.IsNullOrEmpty(extra)) { labels.Add(extra); colors.Add(new Color(0.88f, 0.82f, 0.68f)); }
        yield return ChoicePrompt.AskCards(question, labels, colors, result);
    }

    // --- Getting an injury ---

    // Puts an injury into the player's backpack. `drawn`: it came from the pile, so "抽到时" effects happen.
    // A full backpack makes the player throw away an item of their choice to make room.
    public IEnumerator Give(CharacterCard p, InjuryData injury, bool drawn, List<string> log)
    {
        if (!p || !injury) yield break;
        // e.g. "抽到时，失去1件道具。弃掉这张牌。如没有道具则无事发生"
        if (drawn && injury.effect.Contains("抽到时") && injury.effect.Contains("失去1件道具"))
        {
            if (p.ItemCount > 0) yield return LoseItem(p, p.playerName + " 抽到" + injury.Title + "：" + injury.effect + "\n选择要失去的道具", log);
            Note(log, "  " + p.playerName + " 的" + injury.Title + "：失去 1 件道具后弃掉");
            yield break;
        }
        if (!p.HasFreeItemSlot && p.ItemCount > 0)
            yield return LoseItem(p, p.playerName + " 的背包满了（道具和负伤共 " + CharacterCard.ItemSlots + " 格）。\n要放下" + injury.Title + "，先丢掉一件道具", log);
        p.injuries.Add(injury);
        if (injury.effect.Contains("无法在下一次事件中上场")) p.sitOutEvent = eventNumber + 1;
        p.Refresh();
    }

    IEnumerator LoseItem(CharacterCard p, string question, List<string> log)
    {
        var slots = new List<int>();
        var labels = new List<string>();
        for (int i = 0; i < p.items.Count; i++)
            if (p.items[i]) { slots.Add(i); labels.Add(p.items[i].itemName); }
        if (slots.Count == 0) yield break;
        int pick = 0;
        yield return ChoicePrompt.Ask(question, labels, x => pick = x);
        int slot = slots[pick];
        var lost = p.items[slot];
        p.items[slot] = null;
        if (p.equippedSlot == slot) p.equippedSlot = -1;
        p.Refresh();
        Note(log, "  " + p.playerName + " 丢掉了 " + lost.itemName);
    }

    // Removes the injury; any tokens its card promises when it heals ("…并获得2金token") are given.
    void Heal(CharacterCard p, InjuryData injury, string why, List<string> lines)
    {
        p.injuries.Remove(injury);
        string gained = "";
        var m = Regex.Match(injury.effect, @"获得\s*(\d+)\s*(金|木|水|火|土)\s*token");
        if (m.Success)
        {
            var e = ElementOf(m.Groups[2].Value);
            int n = 0;
            for (int i = 0; i < int.Parse(m.Groups[1].Value); i++) n += p.GainToken(e, tokenPrefab);
            gained = "，获得 " + n + " 枚" + m.Groups[2].Value + " token";
        }
        p.Refresh();
        lines.Add(p.playerName + " 的" + injury.Title + "弃掉了（" + why + "）" + gained);
    }

    // --- Events ---

    // A combat / social encounter starts (reopening one that's still going doesn't count again).
    public void BeginEvent()
    {
        if (inEvent) return;
        inEvent = true;
        eventNumber++;
        onStage.Clear();
        byRound.Clear();
    }

    // `round` (1, 2, 3…) records who went on stage in which round, for "第三轮上场角色…".
    public void WentOnStage(CharacterCard p, int round = 0)
    {
        if (!onStage.Contains(p)) onStage.Add(p);
        if (round > 0 && !byRound.ContainsKey(round)) byRound[round] = p;
    }

    readonly Dictionary<int, CharacterCard> byRound = new Dictionary<int, CharacterCard>();

    // Null if the player may go on stage now, otherwise why not. `first`: nobody has gone on stage yet.
    public string StageBlock(CharacterCard p, bool combat, bool first)
    {
        foreach (var injury in p.injuries)
        {
            if (!injury) continue;
            if (inEvent && p.sitOutEvent == eventNumber && injury.effect.Contains("无法在下一次事件中上场"))
                return p.playerName + " 有" + injury.Title + "：这次事件不能上场";
            if (first && combat && injury.effect.Contains("无法在战斗中第一个出场"))
                return p.playerName + " 有" + injury.Title + "：不能在战斗中第一个出场";
            if (first && !combat && injury.effect.Contains("无法在社交事件中第一个出场"))
                return p.playerName + " 有" + injury.Title + "：不能在社交中第一个出场";
        }
        return null;
    }

    // 负伤 that change the combat value when this player goes on stage:
    // "每次上场战斗值-1", "在战斗中，如果第一个出场，水属性战斗值+1". An element-ed bonus goes through
    // `counter` (克制 ×2 / ×½ against the enemy).
    public int CombatOnStage(CharacterCard p, bool first, List<string> log, System.Func<int, Element, int> counter = null)
    {
        int change = 0;
        foreach (var injury in p.injuries)
        {
            if (!injury) continue;
            var m = Regex.Match(injury.effect, @"每次上场()战斗值\s*([+-])\s*(\d+)");
            if (!m.Success && first) m = Regex.Match(injury.effect, @"战斗中，如果第一个出场，(?:(金|木|水|火|土)属性)?战斗值\s*([+-])\s*(\d+)");
            if (!m.Success) continue;
            int n = int.Parse(m.Groups[3].Value) * (m.Groups[2].Value == "-" ? -1 : 1);
            string what = "战斗值";
            if (m.Groups[1].Value != "")
            {
                var e = ElementOf(m.Groups[1].Value);
                what = m.Groups[1].Value + "属性战斗值";
                if (counter != null && n > 0) n = counter(n, e);
            }
            change += n;
            Note(log, "  " + p.playerName + " 上场：" + injury.Title + "，" + what + " " + (n >= 0 ? "+" : "") + n);
        }
        return change;
    }

    // "每次逃跑失去3主属性token".
    public void OnRetreat(List<CharacterCard> players, List<string> log)
    {
        foreach (var p in players)
        {
            foreach (var injury in p.injuries)
            {
                if (!injury) continue;
                var m = Regex.Match(injury.effect, @"每次逃跑失去\s*(\d+)\s*主属性\s*token");
                if (!m.Success) continue;
                int lost = 0;
                for (int i = 0; i < int.Parse(m.Groups[1].Value); i++)
                    if (CombatPanel.Count(p, p.primaryElement) > 0) { CombatPanel.Spend(p, p.primaryElement); lost++; }
                Note(log, "  " + p.playerName + " 的" + injury.Title + "：逃跑失去 " + lost + " 枚主属性 token");
            }
        }
    }

    // After a win: "上场并赢得一次社交事件后，可弃掉这张牌" / "…赢得一次战斗事件后，可弃掉这张牌，并抽取一张道具牌".
    public List<string> OnWin(bool combat)
    {
        var lines = new List<string>();
        string kind = combat ? "赢得一次战斗事件后" : "赢得一次社交事件后";
        foreach (var p in onStage)
        {
            foreach (var injury in p.injuries.ToArray())
            {
                if (!injury || !injury.effect.Contains(kind)) continue;
                Heal(p, injury, "赢了这次" + (combat ? "战斗" : "社交"), lines);
                if (injury.effect.Contains("抽取一张道具牌"))
                {
                    var items = FindFirstObjectByType<ItemDeck>();
                    if (items && items.DrawFor(p)) lines[lines.Count - 1] += "，并抽取一张道具牌";
                }
            }
        }
        return lines;
    }

    // The 失败 text says who draws which 负伤: "所有上场玩家抽取一张水属性负伤卡", "一名玩家抽取一张“负伤”牌",
    // "第一位上场的玩家…", "最后一位上场的玩家…", "后两个上场的玩家…", "选择一名主属性不为火的角色…获得一张“烧伤”牌".
    // Players protected by 失败保护 are skipped. Ends with a summary of who got what.
    // A text with several 负伤 clauses ("第一轮上场角色抽取1木负伤牌，第三轮上场角色抽取1土负伤牌") settles each one.
    // `special`: the card's 特殊 text, which may let the player swap the element they draw
    // ("你也可以选择抽取一张水负伤牌而不是木负伤牌").
    public IEnumerator ApplyFailure(string text, List<CharacterCard> players, List<CharacterCard> protectedPlayers, string special = null)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("负伤")) yield break;
        swapFrom = swapTo = null;
        var swap = Regex.Match(text + " " + special, @"抽取?一张\s*(金|木|水|火|土)\s*(?:属性)?\s*负伤(?:牌|卡)?\s*而不是\s*(金|木|水|火|土)\s*(?:属性)?\s*负伤");
        if (swap.Success) { swapTo = ElementOf(swap.Groups[1].Value); swapFrom = ElementOf(swap.Groups[2].Value); }
        var clauses = new List<string>();
        foreach (var part in Regex.Split(Regex.Replace(text, @"（[^）]*）", ""), @"[，,；;。]"))
            if (part.Contains("负伤")) clauses.Add(part);
        if (clauses.Count == 0) clauses.Add(text);
        var lines = new List<string>();
        foreach (var clause in clauses) yield return ApplyFailureClause(clause, text, players, protectedPlayers, lines);
        if (protectedPlayers.Count > 0)
            foreach (var p in protectedPlayers) lines.Add(p.playerName + " 用失败保护躲过了负伤");
        yield return Tell("失败结算", lines);
    }

    Element? swapFrom, swapTo;   // This failure lets a player draw swapTo instead of swapFrom.

    // The player picks which element's 负伤 to draw; the buttons are in the elements' colours.
    IEnumerator AskSwap(CharacterCard p, System.Action<Element> result)
    {
        var options = new List<Element> { swapFrom.Value, swapTo.Value };
        var labels = new List<string>();
        var colors = new List<Color>();
        foreach (var e in options)
        {
            labels.Add("<b>" + CombatCard.ElementChar(e) + "属性负伤</b>\n从" + CombatCard.ElementChar(e) + "负伤牌里随机抽一张");
            colors.Add(ElementToken.ColorFor(e));
        }
        int pick = 0;
        yield return ChoicePrompt.AskCards(p.playerName + "：抽哪种负伤牌？", labels, colors, x => pick = x);
        result(options[pick]);
    }

    // `fullText` is the whole 失败 text, for who-can-be-picked limits written outside the clause ("【请选择一名主属性不为火的角色】").
    IEnumerator ApplyFailureClause(string text, string fullText, List<CharacterCard> players, List<CharacterCard> protectedPlayers, List<string> lines)
    {
        var element = InjuryElement(text);
        var free = players.FindAll(p => !protectedPlayers.Contains(p));
        var stage = onStage.FindAll(p => free.Contains(p));
        var targets = new List<CharacterCard>();
        var roundMatch = Regex.Match(text, @"第\s*(一|二|三|1|2|3)\s*轮\s*(出场|上场)");
        if (text.Contains("所有上场")) targets.AddRange(stage);
        else if (text.Contains("所有角色") || text.Contains("每位角色") || text.Contains("所有玩家")) targets.AddRange(free);
        else if (roundMatch.Success)
        {
            // Nobody went on stage that round: nothing to draw.
            int r = "一二三".IndexOf(roundMatch.Groups[1].Value) + 1;
            if (r == 0) r = int.Parse(roundMatch.Groups[1].Value);
            if (byRound.TryGetValue(r, out var who) && free.Contains(who)) targets.Add(who);
        }
        else if (text.Contains("第一位上场") || text.Contains("第一名上场") || text.Contains("第一位出场") || text.Contains("第一名出场")) { if (onStage.Count > 0 && free.Contains(onStage[0])) targets.Add(onStage[0]); }
        else if (text.Contains("最后一位上场")) { if (onStage.Count > 0 && free.Contains(onStage[onStage.Count - 1])) targets.Add(onStage[onStage.Count - 1]); }
        else if (text.Contains("后两个上场"))
        {
            for (int i = Mathf.Max(0, onStage.Count - 2); i < onStage.Count; i++)
                if (free.Contains(onStage[i])) targets.Add(onStage[i]);
        }
        else
        {
            // One player, picked by the team (some cards limit who can be picked).
            var m = Regex.Match(fullText, @"主属性不为\s*(金|木|水|火|土)");
            if (m.Success) free = free.FindAll(p => p.primaryElement != ElementOf(m.Groups[1].Value));
            if (free.Count == 0) yield break;
            var names = new List<string>();
            foreach (var p in free) names.Add(p.playerName);
            int pick = 0;
            yield return ChoicePrompt.Ask("失败：选择一名玩家抽取" + (element.HasValue ? "一张" + CombatCard.ElementChar(element.Value) + "负伤" : "一张负伤牌")
                                          + (m.Success ? "\n（主属性不能是" + m.Groups[1].Value + "）" : ""), names, x => pick = x);
            targets.Add(free[pick]);
        }

        // "抽取2土负伤牌": each target draws that many.
        var countMatch = Regex.Match(text, @"抽取\s*(\d+)\s*(?:张|枚)?\s*(?:金|木|水|火|土)?\s*(?:属性)?\s*负伤");
        int count = countMatch.Success ? Mathf.Max(1, int.Parse(countMatch.Groups[1].Value)) : 1;
        foreach (var p in targets)
        {
            for (int k = 0; k < count; k++)
            {
                var drawElement = element;
                if (element.HasValue && swapFrom.HasValue && element.Value == swapFrom.Value)
                {
                    var chosen = element.Value;
                    yield return AskSwap(p, x => chosen = x);
                    drawElement = chosen;
                }
                var injury = Draw(drawElement);
                if (!injury) continue;
                var log = new List<string>();
                yield return Give(p, injury, true, log);
                lines.Add(p.playerName + " 抽到" + injury.Title + "：" + injury.effect);
                lines.AddRange(log);
            }
        }
    }

    // "水属性负伤", "金负伤", or a kind of wound ("烧伤"…) from the 负伤规则 table.
    static Element? InjuryElement(string text)
    {
        var m = Regex.Match(text, @"(金|木|水|火|土)\s*(属性)?\s*负伤");
        if (m.Success) return ElementOf(m.Groups[1].Value);
        if (text.Contains("烧伤")) return Element.Fire;
        if (text.Contains("划伤") || text.Contains("穿刺")) return Element.Metal;
        if (text.Contains("感染") || text.Contains("中毒")) return Element.Wood;
        if (text.Contains("砸伤") || text.Contains("骨折")) return Element.Earth;
        return null;
    }

    // After an event: "在一次事件完全不上场后，弃掉这张牌并获得2木token", and "无法在下一次事件中上场。在那之后弃掉这张牌".
    public IEnumerator EndEvent(List<CharacterCard> players)
    {
        if (!inEvent) yield break;
        inEvent = false;
        var lines = new List<string>();
        foreach (var p in players)
        {
            foreach (var injury in p.injuries.ToArray())
            {
                if (!injury) continue;
                if (injury.effect.Contains("一次事件完全不上场后") && !onStage.Contains(p))
                    Heal(p, injury, "这次事件没有上场", lines);
                else if (injury.effect.Contains("无法在下一次事件中上场") && p.sitOutEvent == eventNumber)
                    Heal(p, injury, "已经休息了一次事件", lines);
            }
        }
        yield return Tell("事件结束", lines);
    }

    // A new main story card: "抵达下一主线牌时，弃掉这张牌并获得2金token" and the like.
    public IEnumerator OnNewStory()
    {
        var lines = new List<string>();
        foreach (var p in FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude))
            foreach (var injury in p.injuries.ToArray())
                if (injury && injury.effect.Contains("抵达下一主线")) Heal(p, injury, "抵达新的主线牌", lines);
        yield return Tell("新的主线牌", lines);
    }

    // Clicking an injury card in the backpack: pay tokens to get rid of it if the card allows
    // ("弃掉2主属性token即可弃掉这张牌", "消耗2水token可立即弃掉这张牌"), otherwise just show it.
    public IEnumerator OfferPaidHeal(CharacterCard p, InjuryData injury, System.Action done)
    {
        var m = Regex.Match(injury.effect, @"(?:弃掉|消耗)\s*(\d+)\s*(主属性|金|木|水|火|土)\s*token\s*(?:即)?可(?:立即)?弃掉这张牌");
        if (!m.Success)
        {
            yield return ChoicePrompt.Ask(p.playerName + " 的" + injury.Title + "\n" + injury.effect, new List<string> { "关闭" }, null);
            yield break;
        }
        int n = int.Parse(m.Groups[1].Value);
        var e = m.Groups[2].Value == "主属性" ? p.primaryElement : ElementOf(m.Groups[2].Value);
        string cost = n + " 枚" + CombatCard.ElementChar(e) + " token";
        if (CombatPanel.Count(p, e) < n)
        {
            yield return ChoicePrompt.Ask(p.playerName + " 的" + injury.Title + "\n" + injury.effect + "\n\n需要 " + cost + " 才能弃掉，现在不够",
                new List<string> { "关闭" }, null);
            yield break;
        }
        int answer = 1;
        yield return ChoicePrompt.Ask(p.playerName + " 的" + injury.Title + "\n" + injury.effect + "\n\n花 " + cost + " 弃掉这张负伤？",
            new List<string> { "弃掉", "取消" }, x => answer = x);
        if (answer != 0) yield break;
        for (int i = 0; i < n; i++) CombatPanel.Spend(p, e);
        p.injuries.Remove(injury);
        p.Refresh();
        done?.Invoke();
    }

    // 伤药 / 仙草 clicked in the backpack. 伤药: discard one injury card the player picks.
    // 仙草: discard all of them, +1 token of each card's element. The item is used up.
    public IEnumerator UseHealItem(CharacterCard p, int slot, System.Action done)
    {
        var item = p.items[slot];
        var removable = p.injuries.FindAll(x => x && !x.special);   // 特殊负伤 can't be removed.
        if (removable.Count == 0)
        {
            yield return ChoicePrompt.Ask(item.itemName + "：" + p.playerName + " 身上没有能去除的负伤牌", new List<string> { "关闭" }, null);
            yield break;
        }
        var lines = new List<string>();
        if (item.mechanic == ItemMechanic.Salve)
        {
            int answer = removable.Count;
            yield return AskInjury(p.playerName + " 使用伤药：弃掉哪张负伤牌？", removable, "取消", x => answer = x);
            if (answer >= removable.Count) yield break;
            var chosen = removable[answer];
            p.injuries.Remove(chosen);
            lines.Add(p.playerName + " 的" + chosen.Title + "弃掉了（伤药）");
        }
        else
        {
            int answer = 1;
            yield return ChoicePrompt.Ask(p.playerName + " 使用仙草：弃掉身上全部 " + removable.Count + " 张负伤牌？",
                new List<string> { "使用", "取消" }, x => answer = x);
            if (answer != 0) yield break;
            foreach (var injury in removable)
            {
                p.injuries.Remove(injury);
                int n = p.GainToken(injury.element, tokenPrefab);
                lines.Add(p.playerName + " 的" + injury.Title + "弃掉了（仙草）" + (n > 0 ? "，获得 1 枚" + CombatCard.ElementChar(injury.element) + " token" : ""));
            }
        }
        p.items[slot] = null;
        p.Refresh();
        yield return Tell(item.itemName, lines);
        done?.Invoke();
    }

    // --- Helpers ---

    static IEnumerator Tell(string title, List<string> lines)
    {
        if (lines.Count == 0) yield break;
        yield return ChoicePrompt.Ask("<b>" + title + "</b>\n" + string.Join("\n", lines.ToArray()), new List<string> { "确定" }, null);
    }

    static void Note(List<string> log, string line)
    {
        if (log != null) log.Add(line);
    }

    public static Element ElementOf(string c)
    {
        foreach (Element e in System.Enum.GetValues(typeof(Element)))
            if (CombatCard.ElementChar(e) == c) return e;
        return Element.Wood;
    }
}
