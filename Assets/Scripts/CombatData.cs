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
}
