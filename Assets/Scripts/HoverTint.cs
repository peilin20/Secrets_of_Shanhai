using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Greyed out normally, full colour while the mouse is over it or while it's selected (e.g. the species portraits).
public class HoverTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image target;
    public Color normal = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color hover = Color.white;
    public System.Action onEnter;
    public System.Action onExit;

    bool selected;
    bool over;

    public bool Selected
    {
        get => selected;
        set { selected = value; Apply(); }
    }

    void OnEnable() { over = false; Apply(); }

    public void OnPointerEnter(PointerEventData eventData) { over = true; Apply(); onEnter?.Invoke(); }
    public void OnPointerExit(PointerEventData eventData) { over = false; Apply(); onExit?.Invoke(); }

    void Apply()
    {
        if (target) target.color = selected || over ? hover : normal;
    }
}
