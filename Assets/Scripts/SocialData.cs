using System.Collections.Generic;
using UnityEngine;

// One way a social encounter can end well, e.g. "大妈很喜欢我们" at 10 好感度.
[System.Serializable]
public class SocialOutcome
{
    public string label;
    public int favorNeeded;
    [TextArea(2, 6)] public string reward;
    public string labelEn;
    [TextArea(2, 6)] public string rewardEn;
    public string Label => L.Current == Language.English && !string.IsNullOrEmpty(labelEn) ? labelEn : label;
    public string Reward => L.Current == Language.English && !string.IsNullOrEmpty(rewardEn) ? rewardEn : reward;
}

// A social card: the NPC for one social option of an event.
// 3 rounds; each round one player bets tokens of an element and the NPC draws an element from its pool.
[CreateAssetMenu(fileName = "NewSocial", menuName = "Shanhai/Social")]
public class SocialData : ScriptableObject
{
    public string title;
    public Sprite image;
    [TextArea(3, 10)] public string description;
    public List<Element> pool = new List<Element>();   // The NPC's candidate tokens; drawn without putting back.
    public int randomOthers;                           // Plus this many random other elements (different ones), picked each encounter.
    public int startingFavor = 2;                      // 初始好感度 (shared by the team)
    public bool hasSpecies;                            // For items that care about the NPC's species (香囊).
    public Species species;

    [Header("Outcomes, best first")]
    public List<SocialOutcome> outcomes = new List<SocialOutcome>();

    [Header("失败")]
    [TextArea(2, 6)] public string failureResult;
    public CombatData failureCombat;                   // Optional: a failed talk turns into this combat.

    [Header("特殊")]
    [TextArea(2, 6)] public string special;
    public bool hasSpeciesBonus;                       // e.g. 九尾 lets 大妈 touch their tail: every outcome needs less.
    public Species bonusSpecies;
    public int bonusAmount;
    public string bonusButton;                         // The button's text, e.g. "九尾让大妈摸尾巴"

    [Header("English")]
    public string titleEn;
    public string bonusButtonEn;
    [TextArea(2, 6)] public string descriptionEn;
    [TextArea(2, 6)] public string failureResultEn;
    [TextArea(2, 6)] public string specialEn;

    public string Title => L.Current == Language.English && !string.IsNullOrEmpty(titleEn) ? titleEn : title;
    public string Description => L.Current == Language.English && !string.IsNullOrEmpty(descriptionEn) ? descriptionEn : description;
    public string FailureResult => L.Current == Language.English && !string.IsNullOrEmpty(failureResultEn) ? failureResultEn : failureResult;
    public string Special => L.Current == Language.English && !string.IsNullOrEmpty(specialEn) ? specialEn : special;
    public string BonusButton => L.Current == Language.English && !string.IsNullOrEmpty(bonusButtonEn) ? bonusButtonEn : bonusButton;
}
