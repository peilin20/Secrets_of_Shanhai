using UnityEngine;

// A 负伤 (injury) card. Injuries share the 6 backpack slots with items.
// What its effect text says is handled by InjuryDeck (draw, going on stage, healing…).
[CreateAssetMenu(fileName = "NewInjury", menuName = "Shanhai/Injury")]
public class InjuryData : ScriptableObject
{
    public string difficulty;          // 难度等级: Low / Medium
    public Element element;
    [TextArea(2, 6)] public string effect;
    public int copiesInPile = 1;       // 总量

    [Header("Special")]
    public bool special;               // e.g. 寻药者's 特殊负伤: no element, can't be removed by anything.
    public string customTitle;

    public string Title => !string.IsNullOrEmpty(customTitle) ? customTitle : CombatCard.ElementChar(element) + "负伤";

    // 难度 1–3 (the 负伤 sheet says Low / Medium / High).
    public int Level => difficulty == "Medium" ? 2 : difficulty == "High" ? 3 : 1;

    // 伤痛类型 for each element (负伤规则).
    public static string TypeOf(Element e)
    {
        switch (e)
        {
            case Element.Metal: return "锐器造成的划伤、穿刺";
            case Element.Wood: return "慢性感染、毒性，延迟发作或有传染性的病症";
            case Element.Water: return "精神、心灵上的打击";
            case Element.Fire: return "烧伤、上火";
            case Element.Earth: return "砸伤，骨头类伤害（压扁，骨折）";
        }
        return "";
    }
}
