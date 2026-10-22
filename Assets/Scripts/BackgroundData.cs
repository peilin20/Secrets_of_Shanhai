using UnityEngine;

// A background card. Only the name shows on the character card; the description shows in a popup.
[CreateAssetMenu(fileName = "NewBackground", menuName = "Shanhai/Background")]
public class BackgroundData : ScriptableObject
{
    public string backgroundName;
    [TextArea(5, 20)] public string description;
}
