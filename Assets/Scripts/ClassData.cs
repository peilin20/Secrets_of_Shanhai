using System.Collections.Generic;
using UnityEngine;

// A class (职业), e.g. 武者. Choosing it gives the player its weapon cards.
// Class weapons are always usable and never appear in the item card pile.
[CreateAssetMenu(fileName = "NewClass", menuName = "Shanhai/Class")]
public class ClassData : ScriptableObject
{
    public string className;
    [TextArea(3, 8)] public string description;
    public List<ItemData> weapons = new List<ItemData>();

    [Header("English")]
    public string classNameEn;
    [TextArea(3, 8)] public string descriptionEn;

    public string Name => L.Current == Language.English && !string.IsNullOrEmpty(classNameEn) ? classNameEn : className;
    public string Description => L.Current == Language.English && !string.IsNullOrEmpty(descriptionEn) ? descriptionEn : description;
}
