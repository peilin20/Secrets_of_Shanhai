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

    [Header("English")]
    public string customTitleEn;
    [TextArea(2, 6)] public string effectEn;

    // The effect as shown; `effect` stays the rule text the code reads.
    public string EffectText => L.Current == Language.English && !string.IsNullOrEmpty(effectEn) ? effectEn : effect;

    public string Title => !string.IsNullOrEmpty(customTitle) ? (L.Current == Language.English && !string.IsNullOrEmpty(customTitleEn) ? customTitleEn : customTitle) : L.T("injury.title", L.ElementName(element));

    // 难度 1–3 (the 负伤 sheet says Low / Medium / High).
    public int Level => difficulty == "Medium" ? 2 : difficulty == "High" ? 3 : 1;

    // 伤痛类型 for each element (负伤规则).
    public static string TypeOf(Element e)
    {
        switch (e)
        {
            case Element.Metal: return L.T("injury.typeMetal");
            case Element.Wood: return L.T("injury.typeWood");
            case Element.Water: return L.T("injury.typeWater");
            case Element.Fire: return L.T("injury.typeFire");
            case Element.Earth: return L.T("injury.typeEarth");
        }
        return "";
    }
}
