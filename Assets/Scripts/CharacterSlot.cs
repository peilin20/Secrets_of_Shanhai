using UnityEngine;

// A spot on the table where a character card sits. A character card dropped over it snaps into place.
[RequireComponent(typeof(RectTransform))]
public class CharacterSlot : MonoBehaviour
{
    // True if the given world position is inside this slot.
    public bool Contains(Vector3 worldPosition)
    {
        var rt = (RectTransform)transform;
        return rt.rect.Contains(rt.InverseTransformPoint(worldPosition));
    }
}
