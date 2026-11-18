using System.Collections.Generic;
using UnityEngine;

public enum Language
{
    Chinese,
    English
}

// One place for every translated UI string. To translate more text later, add a line to the table below
// and use L.T("your.key") in code (or put a LocalizedText component with that key on a Text).
public static class L
{
    // key → (中文, English). {0}, {1}… are filled in by L.T(key, args).
    static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>
    {
        // Settings
        { "settings.language",      new[] { "语言：中文", "Language: English" } },

        // Title screen
        { "title.start",            new[] { "开始游戏", "Start Game" } },
        { "title.settings",         new[] { "设置", "Settings" } },
        { "title.credits",          new[] { "制作人员", "Credits" } },
        { "title.back",             new[] { "返回", "Back" } },
        { "title.creditsBody",      new[] { "（待填写）", "(To be added)" } },
        { "settings.title",         new[] { "设置", "Settings" } },
        { "settings.close",         new[] { "关闭", "Close" } },

        // Tutorial: 开局介绍
        { "tut.next",               new[] { "下一步", "Next" } },
        { "tut.skip",               new[] { "跳过教程", "Skip Tutorial" } },
        { "tut.welcome",            new[] { "欢迎来到教程！我们先认识一下桌面上的东西。", "Welcome to the tutorial! First, let's look around the table." } },
        { "tut.cards",              new[] { "这里是全队的角色卡，每位玩家一张。", "These are the party's character cards, one for each player." } },
        { "tut.card",               new[] { "角色卡上有你的名字、立绘、物种、职业、背景和正在装备的道具。", "A character card shows your name, portrait, species, class, background and equipped item." } },
        { "tut.element",            new[] { "右上角的圆形是你的主属性。", "The circle in the top right is your main element." } },
        { "tut.tokens", new[] {
            "这里放着你的token。战斗和社交时会消耗它们。\n主属性token是游戏中唯一会随着剧情推进自动恢复的token。主线卡会写明每位玩家获得多少主属性token：主线卡大部分是休息站点，角色们活着抵达这些节点时，通常能够获得资源补充。",
            "Your tokens live here. You spend them in combat and social encounters.\nPrimary-element tokens are the only tokens that come back on their own as the story moves on. Each main story card says how many primary tokens every player gets: most main story cards are resting points, and characters who reach them alive usually get to restock." } },
        { "tut.backpack",           new[] { "点这里打开背包。消耗品和负伤牌共用6个格子；装备不占格子，另外放着。", "Click here to open your backpack. Consumables and injury cards share its 6 slots; equipment doesn't take a slot and is kept apart." } },
        { "tut.story", new[] {
            "这是主线卡，讲述当前的故事。\n点卡片底部的按钮抽取事件卡；事件都结束后，会自动翻到下一张主线卡。",
            "This is the main story card. It tells the current part of the story.\nUse the button at the bottom of the card to draw event cards; once the events are done, the next main story card turns over by itself." } },
        { "tut.eventSlots", new[] {
            "抽到的事件卡会放在这里。点开一张事件卡，选择战斗或社交来解决它。\n所有事件都解决后，就会翻开下一张主线卡。",
            "Drawn event cards go here. Open one and choose combat or social to resolve it.\nOnce every event is resolved, the next main story card is turned." } },
        { "tut.itemPile", new[] {
            "道具牌堆。需要抽道具时点这里，再点一名玩家的角色卡，把道具交给这名玩家。",
            "The item pile. When you draw an item, click here, then click a player's character card to give it to them." } },
        { "tut.eventPile",          new[] { "事件牌堆。事件卡由主线卡上的按钮抽取，不能直接点这里。", "The event pile. Event cards are drawn with the main story card's button, not from here." } },
        { "tut.tokenLibrary",       new[] { "需要手动给角色加token时，可以从这里拖一枚到角色卡上。", "To add a token to a character by hand, drag one from here onto their card." } },
        { "tut.fortune",            new[] { "这是签筒，相当于掷一个六面骰（1d6）。点一下试试！", "This is the fortune-stick box: it rolls a six-sided die (1d6). Click it to try!" } },
        { "tut.introDone",          new[] { "开局介绍到这里就结束了。接下来打一场教学战斗。", "That's the end of the introduction. Next, a practice fight." } },

        // Tutorial: 战斗
        { "tut.combatStart", new[] {
            "桌上出现了一张事件卡「大黄狗妖」。点开这张事件卡。",
            "An event card, \"The Yellow Dog Demon\", is on the table. Click it to open it." } },
        { "tut.combatOption",       new[] { "每张事件卡都可以选择不同的解决方式，比如战斗或社交。这次选择战斗。", "Each event card can be resolved in different ways, such as combat or social. This time, choose combat." } },
        { "tut.enemy", new[] {
            "这是敌人的战斗卡。右上角是敌人的属性，下面写着「全胜」和「胜利」需要的战斗值，以及失败会怎样。",
            "This is the enemy's combat card. The top right shows the enemy's element; below are the combat values needed for a great success and a success, and what happens if you fail." } },
        { "tut.chart", new[] {
            "五行克制：你的攻击属性克制敌人时，战斗值×2；被敌人克制时，战斗值×½（向下取整）。点这张图可以放大。",
            "The five elements: if your attack's element counters the enemy's, its combat value is doubled; if the enemy's counters yours, it is halved (rounded down). Click the chart to enlarge it." } },
        { "tut.pick",               new[] { "①每一轮选一名玩家上场。点一名玩家的名字。", "① Each round, one player goes on stage. Click a player's name." } },
        { "tut.weapon", new[] {
            "上场玩家装备的武器会出现在这里。武器卡上写着要消耗什么token、能打出多少战斗值。点卡片可以放大。\n除非卡上特别说明，角色只有上场时才可以使用装备，每轮1次。上场的角色也可以选择不使用装备，只使用道具。",
            "The player's equipped weapon appears here. The card says which tokens it costs and how much combat value it deals. Click it to enlarge.\nUnless a card says otherwise, a character can only use their equipment while on stage, once per round. The character on stage may also choose not to use their equipment and only use items." } },
        { "tut.fight", new[] {
            "②点这个按钮，用武器出战。按提示选择要消耗的token；需要掷骰时，会弹出签筒，点它抽签。",
            "② Click this button to attack with the weapon. Choose the tokens to spend when asked; if a roll is needed, the fortune-stick box pops up: click it to draw." } },
        { "tut.total",              new[] { "③打出的战斗值按克制换算后，加进这场战斗的总值。", "③ The combat value, after counters, is added to the fight's total." } },
        { "tut.items", new[] {
            "④这里是背包：道具、装备和负伤牌都在这里。点上面的名字可以看别人的背包。每张道具牌上都写明了什么时候可以使用（比如仅限战斗中、仅上场角色可用）。把鼠标移到道具上可以看到效果，以及现在能不能用。",
            "④ This is the backpack: items, equipment and injury cards. Click a name above it to see another player's backpack. Each item card says when it can be used (for example only in combat, or only by the character on stage). Hover over an item to see what it does and whether it can be used right now." } },
        { "tut.endRound",           new[] { "⑤点「结束本轮」进入下一轮。", "⑤ Click \"End round\" to move to the next round." } },
        { "tut.finishCombat", new[] {
            "战斗一共3轮。剩下两轮由你们自己来：选人上场→出战→结束本轮。",
            "A fight lasts 3 rounds. Play the other two yourselves: pick a player → attack → end the round." } },
        { "tut.result", new[] {
            "3轮结束后，用战斗总值和门槛比较：达到「胜利」或「全胜」就能拿到卡上的奖励，否则按失败结算。",
            "After 3 rounds, the total is compared with the thresholds: reach success or great success to get the card's reward; otherwise the failure result applies." } },
        { "tut.continue",           new[] { "点「继续」结束这场战斗。", "Click \"Continue\" to finish the fight." } },
        { "tut.combatDone", new[] { "战斗教程结束！接下来是社交教程。", "That's the end of the combat tutorial! Next: the social tutorial." } },
        { "tut.socialStart", new[] { "桌上又出现了「大黄狗妖」。这次试试和它社交：点开这张事件卡。", "\"The Yellow Dog Demon\" is on the table again. This time, try talking to it: click the event card." } },
        { "tut.socialOption", new[] { "选择社交选项。", "Choose the social option." } },
        { "tut.socialGoal", new[] { "社交的目的是把对方聊高兴，从对方那里套出信息、拿到奖励，所以要想办法提升对方的好感度。对方喜欢听什么，和对方的属性有关：根据对方的属性，猜猜用什么属性说话能让对方高兴。", "The goal of a social encounter is to put the opponent in a good mood, so you can get information and rewards out of them: raise their affinity. What they like to hear depends on their elements: guess from their elements which element to talk with to please them." } },
        { "tut.npc", new[] { "这是对方的社交卡：上面写着初始好感度、每个结果需要多少好感度，以及失败会怎样。", "This is the opponent's social card: it shows the starting affinity, how much affinity each outcome needs, and what happens if you fail." } },
        { "tut.pool", new[] { "这是NPC的候选属性池。每轮NPC会从中随机抽出一枚属性，抽过的不放回（会变暗）。", "This is the NPC's pool of elements. Each round the NPC draws one at random; drawn ones aren't put back (they fade)." } },
        { "tut.favor", new[] { "好感度是全队共享的。这里写着现在的好感度，以及达到每个结果需要的好感度。好感度降到0会立刻失败。", "Affinity is shared by the whole team. Here you see the current affinity and how much each outcome needs. If affinity drops to 0, you fail at once." } },
        { "tut.socialChart", new[] { "社交看相生：你下注的属性和NPC抽到的属性相同是大成功，相生是成功，其他是失败。点这张图可以放大。", "In social encounters, generation matters: the same element as the NPC's draw is a great success, elements that generate each other are a success, anything else fails. Click the chart to enlarge it." } },
        { "tut.socialPick", new[] { "①每一轮选一名玩家说话。点一名玩家的名字。", "① Each round, one player talks. Click a player's name." } },
        { "tut.speak", new[] { "②点这个按钮，选一个属性，下注至少1枚这个属性的token（下注的token会花掉）。然后NPC会抽一枚属性。", "② Click this button, pick an element and bet at least 1 token of it (bet tokens are spent). Then the NPC draws an element." } },
        { "tut.draw", new[] { "③NPC抽到的属性和结果显示在这里：同属性是大成功，好感度加下注数×5（第1轮）或×2（之后）；相生是成功，加下注数（第3轮减半）；其他是失败，扣掉下注数。", "③ The NPC's element and the result are shown here: the same element is a great success, affinity + bet ×5 (round 1) or ×2 (later); elements that generate each other are a success, affinity + bet (halved in round 3); anything else fails, affinity − bet." } },
        { "tut.socialItems", new[] { "④这里是背包，和战斗时一样。每张道具牌上都写明了什么时候可以使用，把鼠标移到道具上可以看到效果，以及现在能不能用。", "④ This is the backpack, as in combat. Each item card says when it can be used; hover over an item to see what it does and whether it can be used right now." } },
        { "tut.socialEnd", new[] { "⑤选择继续下一轮，或者在好感度已经够的时候，选择以某个结果结束这次社交。", "⑤ Choose to go on to the next round or, once the affinity is high enough, end the talk with that outcome." } },
        { "tut.finishSocial", new[] { "社交最多3轮。剩下的由你们自己来：选人说话→下注→继续或结束。", "A talk lasts at most 3 rounds. Play the rest yourselves: pick who talks → bet → go on or end." } },
        { "tut.socialResult", new[] { "结束时按达到的结果拿奖励；3轮结束时好感度还不够任何结果，就按失败结算。", "When the talk ends you get the reward of the outcome you reached; if after 3 rounds the affinity is not enough for any outcome, the failure result applies." } },
        { "tut.socialContinue", new[] { "点「继续」结束这次社交。", "Click \"Continue\" to finish the talk." } },
        { "tut.socialDone", new[] { "社交教程结束！点「下一步」开始游戏。", "That's the end of the social tutorial! Click \"Next\" to start the game." } },

        // Character creation
        { "setup.defaultName",      new[] { "玩家{0}", "Player {0}" } },
        { "setup.enterName",        new[] { "输入你的名字", "Enter your name" } },
        { "setup.ok",               new[] { "确定", "OK" } },
        { "setup.back",             new[] { "返回", "Back" } },
        { "setup.continue",         new[] { "继续", "Continue" } },
        { "setup.chooseSpecies",    new[] { "{0}：选择种族", "{0}: Choose a species" } },
        { "setup.pickSpecies",      new[] { "选择{0}", "Choose {0}" } },
        { "setup.chooseElement",    new[] { "{0}：选择主属性", "{0}: Choose your main element" } },
        { "setup.elementIntro",     new[] { "在山海的世界里，角色做任何事都离不开五行元素的流动，元素间的相生相克常常是决定胜败的关键。主属性决定了角色最容易用出什么属性的效果，擅长应对的战斗和社交场景通常也与此相关。\n角色身上携带的元素token是战斗和社交对抗中的关键资源。主属性token是游戏中唯一会随着剧情推进自动恢复的token；道具、装备、能力发挥出的效果也通常与使用者的主属性息息相关。\n你可以根据对角色形象的想象选择主属性。如果希望团队最优化，建议玩家们互相商量，尽量选择不同的主属性。", "In the world of Shanhai, everything a character does depends on the flow of the five elements, and how they generate and counter each other often decides who wins. Your main element is the element whose effects come to you most easily, and it usually shapes which fights and conversations you handle best.\nThe element tokens a character carries are the key resource in combat and social encounters. Main-element tokens are the only tokens that come back on their own as the story moves on, and the effects of items, equipment and abilities are usually tied to the user's main element.\nChoose the element that fits how you imagine your character. For the strongest team, talk it over and try to pick different main elements." } },
        { "setup.divine",           new[] { "占卜", "Divination" } },
        { "setup.random",           new[] { "随机", "Random" } },
        { "setup.divineHint",       new[] { "输入生日（公历月、日），出生年份按年龄推算，以日主五行为主属性", "Enter your birthday (month and day); your birth year comes from your age, and your day stem (日主) element becomes your main element" } },
        { "setup.randomHint",       new[] { "随机选一个五行", "Pick one of the five elements at random" } },
        { "setup.birthPrompt",      new[] { "{0}：输入你的生日（公历月、日）", "{0}: Enter your birthday (month and day)" } },
        { "setup.badDate",          new[] { "{0}：这不是一个有效的日期，请重新输入生日的月、日", "{0}: That isn't a real date. Enter your birthday's month and day again" } },
        { "setup.enterAge",         new[] { "{0}：输入你的年龄", "{0}: Enter your age" } },
        { "setup.badAge",           new[] { "{0}：请输入一个有效的年龄（整数）", "{0}: Enter a valid age (a whole number)" } },
        { "setup.age",              new[] { "年龄", "Age" } },
        { "setup.speciesSubtitle",  new[] { "种族是这名角色与生俱来的特质，不同种族在文化习俗、外貌、天赋能力上有很大区别。\n你的种族偶尔也会影响遇到的生物如何与你互动。有些精怪会对特定的种族抱有极大的好感或者敌意。在遭遇这些生物时，也许能够在社交中更易成功，或者让战斗更加凶险。", "Your species is something your character is born with. Species differ greatly in culture and customs, appearance and innate abilities.\nYour species can also affect how the creatures you meet treat you. Some spirits hold great affection, or great hostility, towards particular species. When you meet them, socialising may come easier, or a fight may turn more dangerous." } },
        { "setup.speciesIntro",     new[] { "种族的独特天赋有时会与特定的职业契合，但这并不意味着一定要选择这些组合。\n\n<b>如何选择</b>\n你可以从以下四个种族中为你的角色挑选一个。把鼠标移到立绘上查看种族介绍，点击选择。", "A species' unique talents sometimes suit particular classes, but that doesn't mean you have to choose those combinations.\n\n<b>How to choose</b>\nChoose one of the four species below for your character. Hover over a portrait to read about the species; click to pick it." } },
        { "setup.speciesHint",      new[] { "把鼠标移到立绘上查看物种说明，点击选择", "Hover over a portrait to read about the species; click to pick it" } },
        { "setup.year",             new[] { "年", "Year" } },
        { "setup.month",            new[] { "月", "Month" } },
        { "setup.day",              new[] { "日", "Day" } },
        { "setup.divineResult",     new[] { "{0}：占卜结果为：{1}元素", "{0}: The divination says: {1}" } },
        { "setup.randomResult",     new[] { "{0}：你的主元素为：{1}元素", "{0}: Your main element is {1}" } },
        { "setup.gotTokens",        new[] { "{0}：获得了 1 个{1} token", "{0}: You get 1 {1} token" } },
        { "story.primaryToken",     new[] { "翻开新的主线卡：每位玩家获得 1 枚自己主属性的 token", "New main story card: every player gets 1 token of their main element" } },
        { "setup.chooseBackground", new[] { "{0}：选择背景", "{0}: Choose a background" } },
        { "setup.chooseClass",      new[] { "{0}：选择职业", "{0}: Choose a class" } },
        { "setup.weapons",          new[] { "武器卡：{0}", "Weapon cards: {0}" } },
        { "setup.gotWeapons",       new[] { "{0}：获得武器卡 {1}，要装备哪一张？", "{0}: You get the weapon cards {1}. Which one do you equip?" } },
        { "setup.equip",            new[] { "装备 {0}", "Equip {0}" } },
        { "setup.noEquip",          new[] { "暂不装备", "Don't equip" } },
        { "setup.gotConsumables",   new[] { "{0}（{1}）：获得消耗品 {2}", "{0} ({1}): You get the consumables {2}" } },
        { "setup.gotItems",         new[] { "{0}（{1}）：获得道具 {2}", "{0} ({1}): You get the items {2}" } },
        { "setup.startInjury",      new[] { "{0}（{1}）：开局携带{2}：{3}", "{0} ({1}): You start with {2}: {3}" } },
        { "setup.and",              new[] { " 和 ", " and " } },
        { "setup.listSep",          new[] { "、", ", " } },
        { "setup.randomCharacter",  new[] { "随机角色", "Random Character" } },
        { "setup.randomDone",       new[] { "{0}：随机角色已建好", "{0}: Your random character is ready" } },

        // Piles
        { "pile.items",             new[] { "道具牌堆", "Item Card Pile" } },
        { "pile.events",            new[] { "事件牌堆", "Event Card Pile" } },
        { "pile.left",              new[] { "剩余 {0} 张", "{0} left" } },
        { "pile.empty",             new[] { "已空", "Empty" } },
        { "items.givePrompt",       new[] { "点击一名玩家的角色卡，把这张道具交给该玩家", "Click a player's card to give them this item" } },
        { "items.none",             new[] { "没有道具", "No items" } },

        // Character card
        { "card.species",           new[] { "种族：", "Species: " } },
        { "card.class",             new[] { "职业：", "Class: " } },
        { "card.element",           new[] { "属性：", "Element: " } },
        { "card.equipped",          new[] { "装备：", "Equipped: " } },
        { "card.background",        new[] { "背景：", "Background: " } },
        { "card.items",             new[] { "道具 ({0}/{1})", "Items ({0}/{1})" } },

        // Story card
        { "story.drawEvents",       new[] { "抽取 {0} 张事件卡", "Draw {0} Event Cards" } },
        { "story.drawEvent",        new[] { "抽取 1 张事件卡", "Draw 1 Event Card" } },
        { "fortune.title",          new[] { "承天问卜", "Ask of Heaven" } },
        { "fortune.hint",           new[] { "点击签筒抽签（1d{0}）", "Click the stick box to draw (1d{0})" } },
        { "fortune.result",         new[] { "抽到 {0}", "You drew {0}" } },
        { "story.eventsDrawn",      new[] { "已抽取事件卡", "Event Cards Drawn" } },
        { "story.noEventsLeft",     new[] { "事件卡已抽完", "No event cards left" } },
        { "story.slotsFull",        new[] { "事件栏已满", "Event slots are full" } },
    };

    const string PrefKey = "language";
    static Language? current;

    public static event System.Action Changed;

    public static Language Current
    {
        get
        {
            if (!current.HasValue)
            {
                // PlayerPrefs can't be read while Unity is loading assets (OnValidate); show English until it can.
                try { current = (Language)PlayerPrefs.GetInt(PrefKey, (int)Language.English); }
                catch (UnityException) { return Language.English; }
            }
            return current.Value;
        }
        set
        {
            if (current == value) return;
            current = value;
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static void Toggle()
    {
        Current = Current == Language.Chinese ? Language.English : Language.Chinese;
    }

    public static string T(string key, params object[] args)
    {
        if (!table.TryGetValue(key, out var row)) return key;
        string s = row[(int)Current];
        return args.Length > 0 ? string.Format(s, args) : s;
    }

    // Fixed labels already in the scene. When the game starts, any Text showing one of these (in either
    // language) gets a LocalizedText added, so it switches with the setting. Text set from code isn't listed here.
    static readonly string[] sceneLabels = { "pile.items", "pile.events", "items.none" };

    public static string SceneLabelKey(string text)
    {
        foreach (var key in sceneLabels)
            if (System.Array.IndexOf(table[key], text) >= 0) return key;
        return null;
    }

    public static string SpeciesName(Species s)
    {
        if (Current == Language.Chinese) return CombatPanel.SpeciesName(s);
        switch (s)
        {
            case Species.Fox: return "Nine-Tailed Fox";
            case Species.Human: return "Human";
        }
        return s.ToString();
    }

    public static string ElementName(Element e)
    {
        return Current == Language.Chinese ? CombatCard.ElementChar(e) : e.ToString();
    }
}
