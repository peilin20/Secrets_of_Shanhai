using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Put on a card's picture: clicking the picture shows it full screen (click anywhere to close).
// ImageZoom.Show(sprite) can also be called from code.
[RequireComponent(typeof(Image))]
public class ImageZoom : MonoBehaviour, IPointerClickHandler
{
    static GameObject viewer;
    static Image viewerImage;

    // While this says so, the click goes to the parent instead (e.g. a character card waiting to be given an item).
    public System.Func<bool> passClickToParent;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (passClickToParent != null && passClickToParent() && transform.parent)
        {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerClickHandler);
            return;
        }
        var image = GetComponent<Image>();
        if (image && image.sprite && image.enabled) Show(image.sprite);
    }

    // Makes this picture clickable (zooms on click).
    public static ImageZoom Enable(Image image)
    {
        if (!image) return null;
        image.raycastTarget = true;
        var zoom = image.GetComponent<ImageZoom>();
        if (!zoom) zoom = image.gameObject.AddComponent<ImageZoom>();
        return zoom;
    }

    public static void Show(Sprite sprite)
    {
        if (!sprite) return;
        if (!viewer)
        {
            // Its own canvas, above everything (tutorial, settings button…).
            viewer = new GameObject("ImageZoom", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(viewer);
            var canvas = viewer.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            var scaler = viewer.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            var drt = (RectTransform)dim.transform;
            drt.SetParent(viewer.transform, false);
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);
            dim.GetComponent<Button>().onClick.AddListener(() => viewer.SetActive(false));

            var pic = new GameObject("Picture", typeof(RectTransform), typeof(Image));
            var prt = (RectTransform)pic.transform;
            prt.SetParent(drt, false);
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(60f, 70f); prt.offsetMax = new Vector2(-60f, -40f);
            viewerImage = pic.GetComponent<Image>();
            viewerImage.preserveAspect = true;
            viewerImage.raycastTarget = false;

            var hint = new GameObject("Hint", typeof(RectTransform), typeof(Text), typeof(LocalizedText));
            var hrt = (RectTransform)hint.transform;
            hrt.SetParent(drt, false);
            hrt.anchorMin = new Vector2(0f, 0f); hrt.anchorMax = new Vector2(1f, 0f);
            hrt.pivot = new Vector2(0.5f, 0f);
            hrt.anchoredPosition = new Vector2(0f, 20f);
            hrt.sizeDelta = new Vector2(0f, 40f);
            var t = hint.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 24;
            t.color = new Color(1f, 1f, 1f, 0.7f);
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            t.text = L.T("zoom.close");
            hint.GetComponent<LocalizedText>().key = "zoom.close";
        }
        viewerImage.sprite = sprite;
        viewer.SetActive(true);
    }
}
