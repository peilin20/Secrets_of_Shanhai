using UnityEngine;
using UnityEngine.UI;

// The 五行生克 chart in the combat and social windows: the small one (click to enlarge) and the enlarged one.
// The pictures are Assets/Resources/ElementChart/zh.png (Chinese) and en.png (English); the one shown follows
// the language. Replace either file with a new picture of the same name to change the chart.
public class ElementChartArt : MonoBehaviour
{
    Image small, big;
    Text smallHint, bigHint;

    // Called by the combat / social window with its chart button and enlarged chart.
    public static void Setup(Button chartButton, GameObject zoom)
    {
        if (!chartButton && !zoom) return;
        var host = chartButton ? chartButton.gameObject : zoom;   // Shown whenever the window is open.
        var art = host.GetComponent<ElementChartArt>();
        if (!art) art = host.AddComponent<ElementChartArt>();
        art.small = Find<Image>(chartButton ? chartButton.transform : null, "Chart");
        art.big = Find<Image>(zoom ? zoom.transform : null, "Chart");
        art.smallHint = chartButton ? chartButton.GetComponentInChildren<Text>(true) : null;
        art.bigHint = zoom ? zoom.GetComponentInChildren<Text>(true) : null;

        // The round chart brings its own frame: no paper behind it, and as big as fits.
        if (art.big)
        {
            // The enlarged view's own backing is see-through (it still catches the click that closes it).
            var backing = zoom.GetComponent<Image>();
            if (backing) backing.color = new Color(0f, 0f, 0f, 0f);
            var paper = art.big.transform.parent as RectTransform;
            if (paper && paper != zoom.transform)
            {
                var paperImage = paper.GetComponent<Image>();
                if (paperImage) paperImage.color = new Color(0f, 0f, 0f, 0f);
                paper.sizeDelta = new Vector2(820f, 820f);
            }
            var brt = (RectTransform)art.big.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            art.big.preserveAspect = true;
        }
        if (art.small) art.small.preserveAspect = true;
        if (art.bigHint) art.bigHint.color = new Color(0.2f, 0.16f, 0.12f);   // Readable now that the backing is see-through.
        art.Apply();
    }

    static T Find<T>(Transform root, string name) where T : Component
    {
        if (!root) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name && t.GetComponent<T>()) return t.GetComponent<T>();
        return null;
    }

    void OnEnable() { L.Changed += Apply; Apply(); }
    void OnDisable() { L.Changed -= Apply; }

    void Apply()
    {
        var sprite = Resources.Load<Sprite>(L.Current == Language.English ? "ElementChart/en" : "ElementChart/zh");
        if (!sprite) sprite = Resources.Load<Sprite>("ElementChart/zh");
        if (sprite)
        {
            if (small) small.sprite = sprite;
            if (big) big.sprite = sprite;
        }
        if (smallHint) smallHint.text = L.T("chart.enlarge");
        if (bigHint) bigHint.text = L.T("zoom.close");
    }
}
