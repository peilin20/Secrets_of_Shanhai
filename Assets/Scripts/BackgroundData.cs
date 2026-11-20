using UnityEngine;

// A background card. Only the name shows on the character card; the description shows in a popup.
[CreateAssetMenu(fileName = "NewBackground", menuName = "Shanhai/Background")]
public class BackgroundData : ScriptableObject
{
    public string backgroundName;
    [TextArea(5, 20)] public string description;

    [Header("What the player starts with")]
    public System.Collections.Generic.List<ItemData> startItems = new System.Collections.Generic.List<ItemData>();
    public System.Collections.Generic.List<ItemData> pileItems = new System.Collections.Generic.List<ItemData>();   // Taken out of the item pile (e.g. 灵气调查者: 香囊).
    public int randomConsumables;      // Drawn from the item pile (e.g. 行脚商人: 2).
    public InjuryData startInjury;     // e.g. 寻药者's 特殊负伤.

    [Header("English")]
    public string backgroundNameEn;
    [TextArea(5, 20)] public string descriptionEn;

    public string Name => L.Current == Language.English && !string.IsNullOrEmpty(backgroundNameEn) ? backgroundNameEn : backgroundName;
    public string Description => L.Current == Language.English && !string.IsNullOrEmpty(descriptionEn) ? descriptionEn : description;
}
