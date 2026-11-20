using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemEffect
{
    public string label;
    public string value;
    public string labelEn;
    public string valueEn;

    // What's shown: English when the game is in English and there is an English text. `value` stays the rule text.
    public string Label => L.Current == Language.English && !string.IsNullOrEmpty(labelEn) ? labelEn : label;
    public string Value => L.Current == Language.English && !string.IsNullOrEmpty(valueEn) ? valueEn : value;
}

public enum ItemKind
{
    Weapon,       // 装备: equip it and use it every round
    Consumable    // 消耗品: used once, then it's gone
}

// How an item works in combat. Auto reads simple effects from the card text
// (e.g. "消耗1主属性token，+1d6主属性战斗值"); the rest are the special rules of specific cards.
public enum ItemMechanic
{
    Auto,
    TokensTimesThree,   // 破阵刀: each token spent = +3, attribute = the most-spent element
    Pursuit,            // 追击刃: when a teammate's hit is countered, spend 1 token to hit for 2 of that element
    FiveElementCharm,   // 五行符: each token spent (max 1 per element) = a separate +2 of that element
    Escape,             // 脚底抹油: spend 3 tokens, the team retreats; 2+ countering tokens = draw an item
    IncenseBurner,      // 合香炉: spend 3 same-element tokens = +6; if it counters the enemy, another +5
    BloodMist,          // 血雾: draw an injury card, +5 of its element
    Bewitching,         // 迷魂香: draw an injury card; next round the enemy takes its element
    InjuryTransfer,     // 替伤符: per token, move a same-element injury card to a teammate or the enemy (-2d4 to win)
    Sachet,             // 香囊: 全胜 / 胜利 need 1d4 less (4 if same species)
    ExtraTurn,          // 定身术: the current fighter fights again this round
    Retreat,            // 撤离道具: the team retreats safely
    RainCharm,          // 祈雨符: each 金 token spent = 1d4 temporary 水 tokens
    Reroll,             // 贡品: right after a roll, by someone not fighting: roll again
    Disguise,           // 易容面具: the enemy takes you for a species you pick
    SocialOnly,         // 社交道具: social events only
    FailureGuard,       // 失败保护: on a failed combat, protects one character from the failure
    Burst,              // 单次高强度输出: one token of each element = 10 of your primary element
    SpiceTea,           // 香料茶: 2d4 temporary 水 tokens for this event
    Salve,              // 伤药: discard one of your injury cards (used from the backpack)
    Panacea,            // 仙草: discard all your injury cards, +1 token of each one's element (used from the backpack)
    Sugar,              // 糖: in social, the speaker counts it as 1 token of any element
    Rattle,             // 拨浪鼓: +2 favor with children / naughty spirits (not wired up yet)
    NoEffect,           // nothing to use
    WaterPerToken,      // 赤鱬: spend any number of 水 tokens, +1 水 combat value each
    Pouch,              // 锦囊: opened from the backpack, it becomes the items in `contents`
    Money               // 一些钱: combat +1d4 金 (read from the card); social: affinity rises to what 成功 needs
}

// An item card: name, value, element, picture, description and numerical effects.
[CreateAssetMenu(fileName = "NewItem", menuName = "Shanhai/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public ItemKind kind;
    public ItemMechanic mechanic;
    public int value;                 // 0 = no value shown
    public bool hasElement = true;    // false hides the element circle
    public Element element;
    public Sprite image;
    [TextArea(3, 10)] public string description;
    public List<ItemEffect> effects = new List<ItemEffect>();

    [Header("English")]
    public string itemNameEn;
    [TextArea(3, 10)] public string descriptionEn;

    // Shown names and text follow the language; the Chinese fields stay what the rules read.
    public string Name => L.Current == Language.English && !string.IsNullOrEmpty(itemNameEn) ? itemNameEn : itemName;
    public string Description => L.Current == Language.English && !string.IsNullOrEmpty(descriptionEn) ? descriptionEn : description;

    [Header("Pile and class")]
    public int copiesInPile = 1;      // 总量: how many of this card go into the item pile
    public ClassData exclusiveTo;     // 专属: only this class can hold or equip it (empty = anyone)
    public bool speciesOnly;          // 专属 to a species instead (赤鱬: 白泽专属)
    public Species exclusiveSpecies;
    public int backpackSlots;         // Equipment normally takes no backpack slot; this many if it does (赤鱬: 2)
    public List<ItemData> contents = new List<ItemData>();   // 锦囊: what's inside

    public bool IsExclusive => exclusiveTo || speciesOnly;
    // Who it's 专属 to, for messages: a class name or a species name.
    public string ExclusiveName => exclusiveTo ? exclusiveTo.Name : speciesOnly ? L.SpeciesName(exclusiveSpecies) : "";
}
