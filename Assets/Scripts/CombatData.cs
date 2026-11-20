using UnityEngine;

// A combat card: the enemy for one combat option of an event.
// After 3 rounds, the team's total combat value is compared with the 全胜 / 胜利 thresholds.
[CreateAssetMenu(fileName = "NewCombat", menuName = "Shanhai/Combat")]
public class CombatData : ScriptableObject
{
    public string title;
    public Element element;   // The enemy's element (top right of the card).
    public bool hasSpecies;   // For items that care about the enemy's species (香囊).
    public Species species;
    public Sprite image;
    [TextArea(3, 10)] public string description;

    [Header("全胜")]
    public int greatSuccessThreshold;
    [TextArea(2, 6)] public string greatSuccessReward;

    [Header("胜利")]
    public int successThreshold;
    [TextArea(2, 6)] public string successReward;

    [Header("失败")]
    [TextArea(2, 6)] public string failureResult;

    [Header("特殊")]
    [TextArea(2, 6)] public string special;

    [Header("English")]
    public string titleEn;
    [TextArea(2, 6)] public string descriptionEn;
    [TextArea(2, 6)] public string greatSuccessRewardEn;
    [TextArea(2, 6)] public string successRewardEn;
    [TextArea(2, 6)] public string failureResultEn;
    [TextArea(2, 6)] public string specialEn;

    // Shown text follows the language; the Chinese fields stay what the rules read (rewards, failure 负伤).
    public string Title => L.Current == Language.English && !string.IsNullOrEmpty(titleEn) ? titleEn : title;
    public string Description => L.Current == Language.English && !string.IsNullOrEmpty(descriptionEn) ? descriptionEn : description;
    public string GreatSuccessReward => L.Current == Language.English && !string.IsNullOrEmpty(greatSuccessRewardEn) ? greatSuccessRewardEn : greatSuccessReward;
    public string SuccessReward => L.Current == Language.English && !string.IsNullOrEmpty(successRewardEn) ? successRewardEn : successReward;
    public string FailureResult => L.Current == Language.English && !string.IsNullOrEmpty(failureResultEn) ? failureResultEn : failureResult;
    public string Special => L.Current == Language.English && !string.IsNullOrEmpty(specialEn) ? specialEn : special;
}
