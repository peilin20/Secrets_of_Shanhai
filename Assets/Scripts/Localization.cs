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
            "桌上出现了一张事件卡，它是背面朝上的。点一下把它翻开，看到内容后再点一次打开它。",
            "An event card is on the table, face down. Click it to turn it over, then click it again to open it." } },
        { "tut.combatOption",       new[] { "每张事件卡都可以选择不同的解决方式，比如战斗或社交。这次选择战斗。", "Each event card can be resolved in different ways, such as combat or social. This time, choose combat." } },
        { "tut.enemy", new[] {
            "这是敌人的战斗卡。右上角是敌人的属性，下面写着「全胜」和「胜利」需要的战斗值，以及失败会怎样。",
            "This is the enemy's combat card. The top right shows the enemy's element; below are the combat values needed for a critical success and a success, and what happens if you fail." } },
        { "tut.chart", new[] {
            "五行克制：你的攻击属性克制敌人时，战斗值×2；被敌人克制时，战斗值×½（向下取整）。点这张图可以放大。",
            "The five elements: if your attack's element counters the enemy's, its combat value is doubled; if the enemy's counters yours, it is halved (rounded down). Click the chart to enlarge it." } },
        { "tut.pick",               new[] { "①每一轮选一名玩家上场。点一名玩家的名字。", "① Each round, one player is the active player. Click a player's name." } },
        { "tut.weapon", new[] {
            "上场玩家装备的武器会出现在这里。武器卡上写着要消耗什么token、能打出多少战斗值。点卡片可以放大。\n除非卡上特别说明，角色只有上场时才可以使用装备，每轮1次。上场的角色也可以选择不使用装备，只使用道具。",
            "The player's equipped weapon appears here. The card says which tokens it costs and how much combat value it deals. Click it to enlarge.\nUnless a card says otherwise, a character can only use their equipment while they are the active player, once per round. The active player may also choose not to use their equipment and only use items." } },
        { "tut.fight", new[] {
            "②点这个按钮，用武器出战。按提示选择要消耗的token；需要掷骰时，会弹出签筒，点它抽签。",
            "② Click this button to attack with the weapon. Choose the tokens to spend when asked; if a roll is needed, the fortune-stick box pops up: click it to draw." } },
        { "tut.total",              new[] { "③打出的战斗值按克制换算后，加进这场战斗的总值。", "③ The combat value, after counters, is added to the fight's total." } },
        { "tut.items", new[] {
            "④这里是背包：道具、装备和负伤牌都在这里。点上面的名字可以看别人的背包。每张道具牌上都写明了什么时候可以使用（比如仅限战斗中、仅上场角色可用）。把鼠标移到道具上可以看到效果，以及现在能不能用。",
            "④ This is the backpack: items, equipment and injury cards. Click a name above it to see another player's backpack. Each item card says when it can be used (for example only in combat, or only by the active player). Hover over an item to see what it does and whether it can be used right now." } },
        { "tut.endRound",           new[] { "⑤点「结束本轮」进入下一轮。", "⑤ Click \"End round\" to move to the next round." } },
        { "tut.finishCombat", new[] {
            "战斗一共3轮。剩下两轮由你们自己来：选人上场→出战→结束本轮。",
            "A fight lasts 3 rounds. Play the other two yourselves: pick a player → attack → end the round." } },
        { "tut.result", new[] { "战斗结束：用战斗总值和敌人战斗卡上的门槛比较。加粗的是你们拿到的结果，其他结果也列在这里，可以看到避开了什么失败后果，或者错过了什么奖励。", "The fight is over: the total is compared with the thresholds on the enemy's combat card. The outcome you reached is in bold; the others are listed too, so you can see which failure you avoided or which reward you missed." } },
        { "tut.continue", new[] { "点「继续」领取奖励，结束这场战斗。", "Click \"Continue\" to collect the reward and finish the fight." } },
        { "tut.combatDone", new[] { "战斗教程结束！接下来是社交教程。", "That's the end of the combat tutorial! Next: the social tutorial." } },
        { "tut.socialStart", new[] { "桌上又出现了一张事件卡。这次试试和它社交：先点一下翻开，再点一次打开。", "Another event card is on the table. This time, try talking: click it to turn it over, then click again to open it." } },
        { "tut.socialOption", new[] { "选择社交选项。", "Choose the social option." } },
        { "tut.socialGoal", new[] { "社交的目的是把对方聊高兴，从对方那里套出信息、拿到奖励，所以要想办法提升对方的好感度。对方喜欢听什么，和对方的属性有关：根据对方的属性，猜猜用什么属性说话能让对方高兴。", "The goal of a social encounter is to put the opponent in a good mood, so you can get information and rewards out of them: raise their affinity. What they like to hear depends on their elements: guess from their elements which element to talk with to please them." } },
        { "tut.npc", new[] { "这是对方的社交卡：上面写着初始好感度、每个结果需要多少好感度，以及失败会怎样。", "This is the opponent's social card: it shows the starting affinity, how much affinity each outcome needs, and what happens if you fail." } },
        { "tut.pool", new[] { "这是NPC的候选属性池。每轮NPC会从中随机抽出一枚属性，抽过的不放回（会变暗）。", "This is the NPC's pool of elements. Each round the NPC draws one at random; drawn ones aren't put back (they fade)." } },
        { "tut.favor", new[] { "好感度是全队共享的。这里写着现在的好感度，以及达到每个结果需要的好感度。好感度降到0会立刻失败。", "Affinity is shared by the whole team. Here you see the current affinity and how much each outcome needs. If affinity drops to 0, you fail at once." } },
        { "tut.socialChart", new[] { "社交看相生：你下注的属性和NPC抽到的属性相同是「心有灵犀」，相生是「相谈甚欢」，其他是「话不投机」。点这张图可以放大。", "In social encounters, generation matters: the same element as the NPC's draw is \"in tune\", elements that generate each other are \"getting along\", anything else is \"at odds\". Click the chart to enlarge it." } },
        { "tut.socialPick", new[] { "①每一轮选一名玩家说话。点一名玩家的名字。", "① Each round, one player talks. Click a player's name." } },
        { "tut.speak", new[] { "②点这个按钮，选一个属性，下注至少1枚这个属性的token（下注的token会花掉）。然后NPC会抽一枚属性。", "② Click this button, pick an element and bet at least 1 token of it (bet tokens are spent). Then the NPC draws an element." } },
        { "tut.draw", new[] { "③NPC抽到的属性和结果显示在这里：心有灵犀（同属性），好感度加下注数×3（第1轮）或×2（第2、3轮）；相谈甚欢（相生），加下注数；话不投机（其他），扣掉下注数。", "③ The NPC's element and the result are shown here: in tune (same element), affinity + bet ×3 (round 1) or ×2 (rounds 2 and 3); getting along (generating), affinity + bet; at odds (anything else), affinity − bet." } },
        { "tut.socialItems", new[] { "④这里是背包，和战斗时一样。每张道具牌上都写明了什么时候可以使用，把鼠标移到道具上可以看到效果，以及现在能不能用。", "④ This is the backpack, as in combat. Each item card says when it can be used; hover over an item to see what it does and whether it can be used right now." } },
        { "tut.socialEnd", new[] { "⑤选择继续下一轮，或者在好感度已经够的时候，选择以某个结果结束这次社交。", "⑤ Choose to go on to the next round or, once the affinity is high enough, end the talk with that outcome." } },
        { "tut.finishSocial", new[] { "社交最多3轮。剩下的由你们自己来：选人说话→下注→继续或结束。", "A talk lasts at most 3 rounds. Play the rest yourselves: pick who talks → bet → go on or end." } },
        { "tut.socialResult", new[] { "社交结束：加粗的是你们拿到的结果，其他结果也列在这里，可以看到避开了什么失败后果，或者错过了什么奖励。3轮结束时好感度还不够任何结果，就按失败结算。", "The talk is over: the outcome you reached is in bold; the others are listed too, so you can see which failure you avoided or which reward you missed. If after 3 rounds the affinity is not enough for any outcome, the failure result applies." } },
        { "tut.socialContinue", new[] { "点「继续」领取奖励，结束这次社交。", "Click \"Continue\" to collect the reward and finish the talk." } },
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
        { "setup.gotTokens",        new[] { "{0}：获得了 1 个{1} token", "{0}: Token acquired: 1 {1}" } },
        { "story.primaryToken",     new[] { "翻开新的主线卡：每位玩家获得 1 枚自己主属性的 token", "New main story card: every player gets 1 token of their main element" } },
        { "setup.chooseBackground", new[] { "{0}：选择背景", "{0}: Choose a background" } },
        { "setup.chooseClass",      new[] { "{0}：选择职业", "{0}: Choose a class" } },
        { "setup.weapons",          new[] { "武器卡：{0}", "Weapon cards: {0}" } },
        { "setup.gotWeapons",       new[] { "{0}：获得武器卡 {1}，要装备哪一张？", "{0}: Weapon cards acquired: {1}. Which one do you equip?" } },
        { "setup.equip",            new[] { "装备 {0}", "Equip {0}" } },
        { "setup.noEquip",          new[] { "暂不装备", "Don't equip" } },
        { "setup.gotConsumables",   new[] { "{0}（{1}）：获得消耗品 {2}", "{0} ({1}): Consumables acquired: {2}" } },
        { "setup.gotItems",         new[] { "{0}（{1}）：获得道具 {2}", "{0} ({1}): Items acquired: {2}" } },
        { "setup.startInjury",      new[] { "{0}（{1}）：开局携带{2}：{3}", "{0} ({1}): You start with {2}: {3}" } },
        { "setup.and",              new[] { " 和 ", " and " } },
        { "setup.listSep",          new[] { "、", ", " } },
        { "setup.randomCharacter",  new[] { "随机角色", "Random Character" } },
        { "setup.randomDone",       new[] { "{0}：随机角色已建好", "{0}: Your random character is ready" } },

        // Piles
        { "pile.items",             new[] { "道具牌堆", "Item Card Pile" } },
        { "pile.events",            new[] { "事件牌堆", "Event Card Pile" } },
        { "zoom.close",             new[] { "点击任意处关闭", "Click anywhere to close" } },
        { "card.eventBack",         new[] { "事件", "Event" } },
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
        // Event popup
        { "event.needSpecies",      new[] { "队里没有{0}，不能选这一项", "There's no {0} in the team, so this option can't be chosen" } },
        { "event.noRoom",           new[] { "没有人的背包放得下{0}，只能放弃", "Nobody's backpack has room for {0}, so it has to be left behind" } },
        { "event.giveTo",           new[] { "获得{0}：给谁？", "Item acquired: {0}\nWho should take the {0}?" } },
        // Fixed labels in the game scene (see sceneLabels below)
        { "zoom.hint",              new[] { "点击放大", "Click to enlarge" } },
        { "common.use",             new[] { "使用", "Use" } },
        { "common.continue",        new[] { "继续", "Continue" } },
        { "combat.itemsLabel",      new[] { "道具（任何玩家随时可用）", "Items (any player can use them at any time)" } },
        { "social.itemsLabel",      new[] { "道具（装备只有说话的玩家能用）", "Items (only the speaker can use equipment)" } },
        { "story.slotsFull",        new[] { "事件栏已满", "Event slots are full" } },
        // Backpack, item popup, giving out items, injury card face
        { "bag.label",              new[] { "背包：", "Backpack: " } },
        { "bag.button",             new[] { "背包", "Bag" } },
        { "bag.gearLabel",          new[] { "装\n备", "Gear" } },
        { "bag.injury",             new[] { "负伤", "Injury" } },
        { "bag.none",               new[] { "无", "None" } },
        { "bag.title",              new[] { "{0} 的背包 ({1}/{2})   装备中：{3}   （{4}）", "{0}'s backpack ({1}/{2})   Equipped: {3}   ({4})" } },
        { "bag.equipHint",          new[] { "点击装备卡来装备，点击负伤牌查看", "click an equipment card to equip it, click an injury card to view it" } },
        { "bag.lockedHint",         new[] { "战斗中不能更换装备，点击负伤牌查看", "equipment can't be changed during combat, click an injury card to view it" } },
        { "bag.consumable",         new[] { "{0} 的道具   （{1} 是消耗品，在战斗中使用，不能装备）", "{0}'s items   ({1} is a consumable: it's used in combat and can't be equipped)" } },
        { "bag.cantEquip",          new[] { "{0} 的道具   （{1}）", "{0}'s items   ({1})" } },
        { "bag.locked",             new[] { "战斗中不能更换装备", "equipment can't be changed during combat" } },
        { "bag.exclusive",          new[] { "{0} 是{1}专属，不能装备", "{0} is exclusive to the {1} class and can't be equipped" } },
        { "bag.otherClass",         new[] { "其他职业", "another" } },
        { "deck.nobody",            new[] { "没有玩家能拿这张牌（{0}），点击道具牌堆弃掉", "Nobody can take this card ({0}). Click the item pile to discard it" } },
        { "deck.exclusiveOf",       new[] { "{0} 是{1}专属", "{0} is exclusive to the {1} class" } },
        { "deck.allFull",           new[] { "大家的道具栏都满了", "everyone's backpack is full" } },
        { "deck.onlyFor",           new[] { "{0} 是{1}专属，只能给{1}", "{0} is exclusive to the {1} class and can only go to a {1}" } },
        { "deck.full",              new[] { "{0} 的背包满了（消耗品和负伤共 {1} 格），请选别的玩家", "{0}'s backpack is full (consumables and injuries share {1} slots). Choose another player" } },
        { "injury.special",         new[] { "特殊负伤（无法去除）", "Special injury (can't be removed)" } },
        { "injury.kind",            new[] { "{0}属性负伤（难度 {1}）", "{0} injury (difficulty {1})" } },
        { "injury.title",           new[] { "{0}负伤", "{0} Injury" } },
        { "injury.effect",          new[] { "效果：", "Effect: " } },
        { "injury.typeMetal",       new[] { "锐器造成的划伤、穿刺", "Cuts and stab wounds from sharp weapons" } },
        { "injury.typeWood",        new[] { "慢性感染、毒性，延迟发作或有传染性的病症", "Chronic infections and poisons: illnesses that set in late or spread" } },
        { "injury.typeWater",       new[] { "精神、心灵上的打击", "Blows to the mind and spirit" } },
        { "injury.typeFire",        new[] { "烧伤、上火", "Burns and inner heat" } },
        { "injury.typeEarth",       new[] { "砸伤，骨头类伤害（压扁，骨折）", "Crushing blows and bone injuries (crushed, broken)" } },
        // Shared bits
        { "common.done",            new[] { "完成", "Done" } },
        { "common.cancel",          new[] { "取消", "Cancel" } },
        { "sep.comma",              new[] { "，", ", " } },
        { "sep.colon",              new[] { "：", ": " } },
        { "result.yours",           new[] { "【你们的结果】", "[Your result] " } },

        // Backpack row (combat and social)
        { "items.hover",            new[] { "鼠标移到道具上查看效果", "Hover over an item to see what it does" } },
        { "items.hoverZoom",        new[] { "鼠标移到道具上查看效果，点击卡片放大", "Hover over an item to see what it does; click a card to enlarge it" } },
        { "items.equipped",         new[] { "装备中", "Equipped" } },
        { "items.equipment",        new[] { "装备", "Equipment" } },
        { "items.consumable",       new[] { "消耗品", "Consumable" } },
        { "items.nameDetail",       new[] { "{0}：{1}", "{0}: {1}" } },
        { "items.cantUse",          new[] { "【现在不能用：{0}】", "[Can't use now: {0}]" } },
        { "items.emptyBackpack",    new[] { "（背包是空的）", "(The backpack is empty)" } },

        // Why an item can't be used right now
        { "why.socialOnly",         new[] { "只能在社交中使用", "Only usable in social encounters" } },
        { "why.combatOnly",         new[] { "只能在战斗中使用", "Only usable in combat" } },
        { "why.useFromBackpack",    new[] { "在背包里点击这张卡使用", "Use it by clicking the card in the backpack" } },
        { "why.noEffect",           new[] { "没有使用效果", "It has no use effect" } },
        { "why.failureGuard",       new[] { "战斗失败时在结算画面使用", "Used on the results screen when a fight is lost" } },
        { "why.socialGuard",        new[] { "社交失败时在结算画面使用", "Used on the results screen when a social encounter fails" } },
        { "why.afterAttack",        new[] { "本轮上场的玩家出战后才能使用", "Only after this round's active player has attacked" } },
        { "why.offStage",           new[] { "只能由未上场的玩家使用", "Only a player who isn't the active player can use it" } },
        { "why.afterRoll",          new[] { "要在一次掷骰后立刻使用", "Must be used right after a roll" } },
        { "why.noMetal",            new[] { "没有金 token", "No Metal tokens" } },
        { "why.fiveTokens",         new[] { "需要五种属性的 token 各一枚", "Needs one token of each of the five elements" } },
        { "why.speakerEquip",       new[] { "装备只有本轮说话的玩家能用", "Only this round's speaker can use equipment" } },
        { "why.speakerOnly",        new[] { "只有本轮说话的玩家能用", "Only this round's speaker can use it" } },
        { "why.need3",              new[] { "需要 3 枚 token", "Needs 3 tokens" } },
        { "why.noTransfer",         new[] { "没有能转移的负伤牌（需要负伤牌和同属性 token）", "No injury card can be moved (needs an injury card and a token of its element)" } },
        { "why.rattle",             new[] { "还没有设定哪些社交对象是孩子或调皮的精怪", "It isn't set yet which social opponents are children or mischievous spirits" } },
        { "why.afterSpeak",         new[] { "本轮说话的玩家说完后才能使用", "Only after this round's speaker has talked" } },
        { "why.poolTooSmall",       new[] { "NPC 的候选池不够再说一次", "The NPC's pool doesn't have enough left for another talk" } },

        // Combat screen
        { "combat.round",           new[] { "第 {0} / {1} 轮", "Round {0} / {1}" } },
        { "combat.over",            new[] { "战斗结束", "Fight Over" } },
        { "combat.total",           new[] { "战斗总值：{0}", "Combat total: {0}" } },
        { "combat.noCard",          new[] { "还没有关联战斗卡。", "No combat card is linked yet." } },
        { "combat.calcHint",        new[] { "出战后，这里会显示战斗值是怎么算出来的。", "After an attack, this shows how the combat value was worked out." } },
        { "combat.fightWith",       new[] { "用 {0} 出战（{1}）", "Attack with {0} ({1})" } },
        { "combat.endRound",        new[] { "⑤ 结束本轮", "⑤ End round" } },
        { "combat.endLastRound",    new[] { "⑤ 结束第 3 轮并结算", "⑤ End round 3 and settle" } },
        { "combat.skipEndRound",    new[] { "不使用装备，结束本轮", "End round without using equipment" } },
        { "combat.skipEndLast",     new[] { "不使用装备，结束并结算", "End and settle without using equipment" } },
        { "combat.hintPick",        new[] { "① 选择本轮上场的玩家", "① Select the active player" } },
        { "combat.hintAfter",       new[] { "④ 还可以使用道具，然后结束本轮", "④ You can still use items, then end the round" } },
        { "combat.hintBlocked",     new[] { "{0}，请换人", "{0}. Choose someone else" } },
        { "combat.hintFight",       new[] { "② {0} 上场，用 {1} 出战，或者不使用装备（出战前可以换人）", "② {0} is the active player: attack with {1}, or don't use equipment (you can still switch players before attacking)" } },
        { "combat.hintNoWeapon",    new[] { "{0} 上场，没有装备武器：可以使用道具，然后结束本轮", "{0} is the active player with no weapon equipped: use items, then end the round" } },
        { "combat.enemy",           new[] { "敌人", "Enemy" } },
        { "combat.counters",        new[] { "{0}克{1}", "{0} counters {1}" } },
        { "combat.elementAttr",     new[] { "{0}属性", "{0}" } },
        { "combat.noElement",       new[] { "无属性", "no element" } },
        { "combat.noCounter",       new[] { "不计克制", "counters ignored" } },
        { "combat.valueLine",       new[] { "战斗值：{0}", "Combat value: {0}" } },
        { "combat.paren",           new[] { "（{0}）", " ({0})" } },
        { "combat.great",           new[] { "全胜！", "Critical Success!" } },
        { "combat.win",             new[] { "胜利！", "Success!" } },
        { "combat.fail",            new[] { "失败", "Failure" } },
        { "combat.noThresholds",    new[] { "这张战斗卡还没有设置门槛。", "This combat card has no thresholds set yet." } },
        { "combat.greatLine",       new[] { "全胜（战斗值≥{0}）", "Critical Success (combat value ≥ {0})" } },
        { "combat.winLine",         new[] { "胜利（战斗值≥{0}）", "Success (combat value ≥ {0})" } },
        { "combat.failLine",        new[] { "失败（战斗值<{0}）", "Failure (combat value < {0})" } },
        { "combat.classToken",      new[] { "{0}（{1}）获得 {2} 枚{3} token", "{0} ({1}): Tokens acquired: {2} {3}" } },
        { "combat.retreatTitle",    new[] { "撤退", "Retreat" } },
        { "combat.retreatBody",     new[] { "全队安全撤离了战斗。", "The whole team got away from the fight safely." } },
        { "combat.retreatDraw",     new[] { "关闭后抽取 1 张道具卡", "Draw 1 item card after closing" } },
        { "combat.useGuard",        new[] { "{0} 使用失败保护", "{0}: use failure protection" } },
        { "combat.guarded",         new[] { "{0} 用失败保护抵挡了失败效果", "{0} blocked the failure effect with failure protection" } },
        { "combat.askToken",        new[] { "{0}：选择要消耗的 token", "{0}: choose a token to spend" } },
        { "combat.askTokenX3",      new[] { "{0}：选择要消耗的 token（已消耗 {1} 枚，每枚 +3）", "{0}: choose a token to spend ({1} spent so far, +3 each)" } },
        { "combat.askTokenCharm",   new[] { "{0}：选择要消耗的 token（每种属性最多 1 枚，每枚 +2）", "{0}: choose a token to spend (at most 1 per element, +2 each)" } },
        { "combat.askToken3",       new[] { "{0}：选择要消耗的 token（{1} / 3）", "{0}: choose a token to spend ({1} / 3)" } },
        { "combat.askElement3",     new[] { "{0}：选择要消耗 3 枚的属性", "{0}: choose the element to spend 3 tokens of" } },
        { "combat.askBewitch",      new[] { "{0}：迷魂香——选择要抽哪种属性的负伤牌（+2 该属性战斗值，下回合敌人变为该属性）", "{0}: choose which element's injury card to draw (+2 combat value of that element; next round the enemy becomes that element)" } },
        { "combat.askTransfer",     new[] { "{0}：选择要转移的负伤牌（每张消耗 1 枚同属性 token，特殊负伤消耗任意 1 枚）", "{0}: choose an injury card to move (each costs 1 token of its element; special injuries cost any 1 token)" } },
        { "combat.askTransferTo",   new[] { "把{0}转移给谁？", "Move {0} to whom?" } },
        { "combat.askTransferToken", new[] { "{0}：消耗哪一枚 token 转移{1}？", "{0}: which token do you spend to move {1}?" } },
        { "combat.askMetal",        new[] { "{0}：消耗几枚金 token？（每枚换 1d4 枚临时水 token）", "{0}: how many Metal tokens do you spend? (each becomes 1d4 temporary Water tokens)" } },
        { "combat.spendMetal",      new[] { "消耗 {0} 枚金", "Spend {0} Metal" } },
        { "combat.askDisguise",     new[] { "{0}：让对方把你误认为哪个种族？", "{0}: which species should the opponent mistake you for?" } },
        { "combat.askPursuit",      new[] { "{0} 的追击刃：队友的攻击被克制了，消耗 1 枚 token 追击？", "{0}'s follow-up weapon: a teammate's attack was countered. Spend 1 token to follow up?" } },
        { "combat.noPursue",        new[] { "不追击", "Don't follow up" } },
        { "combat.rolledElement",   new[] { "（属性掷出 {0}）", " (element roll: {0})" } },
        { "combat.counterBonus",    new[] { "（克制敌方，额外伤害）", " (counters the enemy: extra damage)" } },
        { "combat.drewInjury",      new[] { "（抽到{0}负伤）", " (drew a {0} injury)" } },
        { "combat.drawInjury",      new[] { "（抽{0}负伤）", " (drew a {0} injury)" } },
        { "combat.fiveTokens",      new[] { "（五种属性token各一枚）", " (one token of each element)" } },
        { "combat.escapeDraw",      new[] { "（{0} 枚克制敌方，抽取 1 张道具）", " ({0} countered the enemy: draw 1 item)" } },
        { "combat.sachetSame",      new[] { "（与目标种族相同，取最大值）", " (same species as the target: maximum)" } },
        { "combat.sachetRoll",      new[] { "（1d4）", " (1d4)" } },
        { "combat.enemyNext",       new[] { "下回合敌方属性变为{0}", "Next round the enemy's element becomes {0}" } },
        { "combat.enemyNowBewitched", new[] { "本轮敌方属性变为{0}（迷魂香）", "This round the enemy's element is {0} (item effect)" } },
        // Combat log
        { "combat.srcRound",        new[] { "第{0}轮 {1}", "Round {0}: {1}" } },
        { "combat.srcItem",         new[] { "道具 {0}", "Item: {0}" } },
        { "combat.srcPursuit",      new[] { "追击 {0} 追击刃（{1}）", "Follow-up: {0} ({1})" } },
        { "combat.uses",            new[] { "{0} 使用 {1}", "{0} uses {1}" } },
        { "combat.logNoEquip",      new[] { "第{0}轮 {1} 上场，没有使用装备", "Round {0}: {1} is the active player and doesn't use equipment" } },
        { "combat.logHit",          new[] { "{0}：{1} = {2}，{3}，{4} → +{5}", "{0}: {1} = {2}, {3}, {4} → +{5}" } },
        { "combat.logRetreatSafe",  new[] { "{0}：全队安全撤离", "{0}: the whole team retreats safely" } },
        { "combat.logEscape",       new[] { "{0}：全队安全撤退", "{0}: the whole team retreats safely" } },
        { "combat.logExtraTurn",    new[] { "{0}：获得额外回合，可以再出战一次", "{0}: gets an extra turn and may attack again" } },
        { "combat.logManual",       new[] { "{0}：效果请按卡面结算", "{0}: resolve the effect as written on the card" } },
        { "combat.logNotEnough",    new[] { "{0}：token 不足，无法使用", "{0}: not enough tokens, can't use it" } },
        { "combat.logNoTokens",     new[] { "{0}：没有 token，无法使用", "{0}: no tokens, can't use it" } },
        { "combat.logNeed3",        new[] { "{0}：需要 3 枚 token", "{0}: needs 3 tokens" } },
        { "combat.logNeed3Same",    new[] { "{0}：需要 3 枚相同属性的 token", "{0}: needs 3 tokens of the same element" } },
        { "combat.logNoInjury",     new[] { "{0}：没有负伤牌可抽", "{0}: no injury cards left to draw" } },
        { "combat.logTransfer",     new[] { "{0}：把{1}转移给 {2}", "{0}: moved {1} to {2}" } },
        { "combat.logTransferEnemy", new[] { "{0}：把{1}转移给敌人，2d4 = {2}+{3}，全胜 / 胜利条件各 -{4}", "{0}: moved {1} to the enemy, 2d4 = {2}+{3}; Critical Success / Success thresholds each -{4}" } },
        { "combat.logNoTransfer",   new[] { "{0}：没有能转移的负伤牌（需要负伤牌和同属性 token）", "{0}: no injury card can be moved (needs an injury card and a token of its element)" } },
        { "combat.logSachet",       new[] { "{0}：胜利和全胜条件 -{1}", "{0}: Success and Critical Success thresholds -{1}" } },
        { "combat.logRain",         new[] { "{0}：消耗{1}枚金，获得{2}枚临时水token（{3}）", "{0}: spent {1} Metal. Tokens acquired: {2} temporary Water ({3})" } },
        { "combat.logTea",          new[] { "{0}：获得{1}枚临时水token（{2}+{3}）", "{0}: Tokens acquired: {1} temporary Water ({2}+{3})" } },
        { "combat.logNoReroll",     new[] { "{0}：没有可以重掷的骰子", "{0}: no dice to reroll" } },
        { "combat.logReroll",       new[] { "{0}：重掷 {1} 的 {2}，{3} = {4}，{5} → {6} 改为 {7}", "{0}: rerolled {1}'s {2}, {3} = {4}, {5} → {6} becomes {7}" } },
        { "combat.logDisguise",     new[] { "{0}：本次事件中对方把 {1} 误认为{2}", "{0}: for this event, the opponent takes {1} for {2}" } },
        // Short weapon summaries on the attack button
        { "sum.timesThree",         new[] { "每消耗1枚token +3", "+3 per token spent" } },
        { "sum.charm",              new[] { "每种属性token +2", "+2 per element token" } },
        { "sum.escape",             new[] { "消耗3枚token撤退", "Spend 3 tokens to retreat" } },
        { "sum.incense",            new[] { "消耗3枚同属性token +6", "Spend 3 same-element tokens: +6" } },
        { "sum.bloodMist",          new[] { "抽负伤牌 +5", "Draw an injury card: +5" } },
        { "sum.bewitch",            new[] { "抽负伤牌，下回合改敌方属性", "Draw an injury card, change the enemy's element next round" } },
        { "sum.transfer",           new[] { "转移负伤牌", "Move injury cards" } },
        { "sum.disguise",           new[] { "选择伪装的种族", "Choose a species to disguise as" } },
        { "sum.manual",             new[] { "按卡面结算", "as written on the card" } },
        { "sum.primary",            new[] { "主属性", "primary element" } },
        { "sum.tokenElement",       new[] { "token属性", "token's element" } },
        { "sum.rolled",             new[] { "掷骰定属性", "rolled element" } },
        { "sum.costPrimary",        new[] { "，耗{0}主属性token", ", costs {0} primary token(s)" } },
        { "sum.costAny",            new[] { "，耗{0}任意token", ", costs {0} token(s) of any element" } },

        // Social screen
        { "social.over",            new[] { "对抗结束", "Talk Over" } },
        { "social.affinity",        new[] { "好感度：{0}", "Affinity: {0}" } },
        { "social.noCard",          new[] { "还没有关联社交卡。", "No social card is linked yet." } },
        { "social.noCardOption",    new[] { "这个选项还没有关联社交卡。", "This option has no social card linked yet." } },
        { "social.needLine",        new[] { "{0}：需 ≥{1}", "{0}: needs ≥ {1}" } },
        { "social.reached",         new[] { "【已达成】", " [Reached]" } },
        { "social.allLowered",      new[] { "（所有选项已 -{0}）", "(All outcomes lowered by {0})" } },
        { "social.bonusLabel",      new[] { "{0}（所有选项 -{1}）", "{0} (all outcomes -{1})" } },
        { "social.pool",            new[] { "NPC 候选池（剩 {0} 枚，不放回）", "NPC pool ({0} left, not put back)" } },
        { "social.rates",           new[] { "第 {0} 轮倍率\n心有灵犀（同属性）×{1}\n相谈甚欢（相生）×1\n话不投机：扣除下注", "Round {0} rates\nIn Tune (same element) ×{1}\nGetting Along (generating) ×1\nAt Odds: lose the bet" } },
        { "social.thinking",        new[] { "NPC 正在想……", "The NPC is thinking…" } },
        { "social.inTune",          new[] { "心有灵犀", "In Tune" } },
        { "social.gettingAlong",    new[] { "相谈甚欢", "Getting Along" } },
        { "social.atOdds",          new[] { "话不投机", "At Odds" } },
        { "social.foxTalent",       new[] { "（九尾天赋）", " (Nine-Tailed Fox talent)" } },
        { "social.bothSame",        new[] { "都是{0}", "Both {0}" } },
        { "social.generates",       new[] { "{0}生{1}", "{0} generates {1}" } },
        { "social.drawn",           new[] { "NPC 抽到：{0}", "The NPC drew: {0}" } },
        { "social.drawResult",      new[] { "{0}，好感度 {1}", "{0}, affinity {1}" } },
        { "social.logBet",          new[] { "第{0}轮 {1} 下注 {2} 枚{3}", "Round {0}: {1} bets {2} {3}" } },
        { "social.countedAs",       new[] { "（负伤：算作 {0} 枚）", " (injury: counts as {0})" } },
        { "social.logDraw",         new[] { "，NPC 抽到{0}：{1}，{2} → 好感度 {3}", ", the NPC drew {0}: {1}, {2} → affinity {3}" } },
        { "social.logInjury",       new[] { "{0} 上场：{1}负伤，好感度 -{2}", "{0} becomes the active player: {1} injury, affinity -{2}" } },
        { "social.speciesToken",    new[] { "{0}（{1}）额外获得 {2} 枚{3} token", "{0} ({1}): Extra tokens acquired: {2} {3}" } },
        { "social.noTokens",        new[] { "{0} 没有 token，不能下注，请换人", "{0} has no tokens and can't bet. Choose someone else" } },
        { "social.nobodyCanBet",    new[] { "没有玩家还能下注（每轮至少下注 1 枚 token）", "Nobody can bet any more (each round needs a bet of at least 1 token)" } },
        { "social.hintPick",        new[] { "① 选择本轮说话的玩家", "① Choose who talks this round" } },
        { "social.hintAgain",       new[] { "② {0} 再说一次（定身术）", "② {0} talks again (extra turn)" } },
        { "social.hintSpeak",       new[] { "② {0} 说话：选一个属性，下注至少 1 枚（说话前可以换人）", "② {0} talks: pick an element and bet at least 1 token (you can still switch players before talking)" } },
        { "social.speakButton",     new[] { "② 选择属性并下注", "② Pick an element and bet" } },
        { "social.endWith",         new[] { "结束：{0}", "End: {0}" } },
        { "social.hintPickOrEnd",   new[] { "① 选择说话的玩家，或者直接结束（好感度已够）", "① Choose who talks, or end now (affinity is high enough)" } },
        { "social.hintSpeakOrEnd",  new[] { "② {0} 说话，或者直接结束（好感度已够）", "② {0} talks, or end now (affinity is high enough)" } },
        { "social.nextRound",       new[] { "⑤ 继续下一轮", "⑤ Next round" } },
        { "social.endFail",         new[] { "结束对抗（好感度不够，失败）", "End the talk (not enough affinity: failure)" } },
        { "social.outOfRounds",     new[] { "3 轮结束，好感度没有达到任何选项", "3 rounds are over and affinity didn't reach any outcome" } },
        { "social.hintAfter",       new[] { "④ 还可以使用道具   ⑤ 继续下一轮，或在好感度够时结束对抗", "④ You can still use items   ⑤ Go on to the next round, or end the talk once affinity is high enough" } },
        { "social.hintAfterLast",   new[] { "④ 还可以使用道具   ⑤ 最后一轮结束，选择结果", "④ You can still use items   ⑤ The last round is over: choose the outcome" } },
        { "social.askElement",      new[] { "{0}：用哪个属性说话？", "{0}: which element do you talk with?" } },
        { "social.bet",             new[] { "下注 {0}", "Bet {0}" } },
        { "social.askBet",          new[] { "{0}：下注几枚{1}？（下得越多，输赢越大）", "{0}: how many {1} do you bet? (the more you bet, the more you win or lose)" } },
        { "social.zero",            new[] { "好感度降到 0", "Affinity dropped to 0" } },
        { "social.logLost",         new[] { "{0}，对抗失败", "{0}. The talk failed" } },
        { "social.thenCombat",      new[] { "继续后进入战斗【{0}】", "After continuing, a fight begins: {0}" } },
        { "social.retreatBody",     new[] { "全队安全撤离了对话。", "The whole team got away from the talk safely." } },
        { "social.logEnd",          new[] { "结束对抗：{0}", "Talk ended: {0}" } },
        { "social.outcomeLine",     new[] { "{0}（好感度≥{1}）", "{0} (affinity ≥ {1})" } },
        { "social.logPlus5",        new[] { "{0}：好感度 +5", "{0}: affinity +5" } },
        { "social.logExtraTurn",    new[] { "{0}：额外获得 1 回合，{1} 可以再说一次", "{0}: gets 1 extra turn, {1} may talk again" } },
        { "social.logSachet",       new[] { "{0}：所有选项所需好感度 -{1}", "{0}: affinity needed for every outcome -{1}" } },
        { "social.askSugar",        new[] { "{0}：把糖糕当作哪种属性的 token？", "{0}: which element's token should the item count as?" } },
        { "social.logSugar",        new[] { "{0}：当作 1 枚{1} token", "{0}: counts as 1 {1} token" } },
        { "social.blocked",         new[] { "（被负伤挡住了）", " (blocked by an injury)" } },
        { "social.logTransferNpc",  new[] { "{0}：把{1}转移给 NPC，2d4 = {2}+{3}，所有选项所需好感度 -{4}", "{0}: moved {1} to the NPC, 2d4 = {2}+{3}; affinity needed for every outcome -{4}" } },
        { "social.logBonus",        new[] { "特殊：{0}，所有选项所需好感度 -{1}", "Special: {0}, affinity needed for every outcome -{1}" } },
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
    static readonly string[] sceneLabels = { "pile.items", "pile.events", "items.none", "zoom.close", "zoom.hint",
        "common.use", "common.continue", "combat.itemsLabel", "social.itemsLabel" };

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
