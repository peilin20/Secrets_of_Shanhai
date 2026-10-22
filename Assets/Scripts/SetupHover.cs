using UnityEngine;
using UnityEngine.EventSystems;

// Calls back when the pointer moves over a setup choice, to show its details.
public class SetupHover : MonoBehaviour, IPointerEnterHandler
{
    public System.Action onHover;

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHover?.Invoke();
    }
}
