using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Shows one SocialData on a card: NPC name, its candidate tokens under the name, picture,
// then 初始好感度 / outcomes / 失败 / 特殊.
public class SocialCard : MonoBehaviour
{
    public SocialData data;

    [Header("UI")]
    public Text titleText;
    public RectTransform poolRow;     // One circle per candidate token.
    public Image circleTemplate;      // Inactive; copied per token.
    public Image socialImage;
    public Text bodyText;

    void Start()
    {
        Refresh();
        if (Application.isPlaying) ImageZoom.Enable(socialImage);   // Click the picture: full screen.
    }

    public void SetSocial(SocialData social)
    {
        data = social;
        Refresh();
    }

    public void Refresh()
    {
        if (!data) return;
        if (titleText) titleText.text = data.title;
        if (poolRow && circleTemplate)
        {
            foreach (Transform child in poolRow)
                if (child != circleTemplate.transform) Destroy(child.gameObject);
            foreach (var e in data.pool)
            {
                var circle = Instantiate(circleTemplate, poolRow);
                circle.gameObject.SetActive(true);
                circle.color = ElementToken.ColorFor(e);
                circle.GetComponentInChildren<Text>().text = CombatCard.ElementChar(e);
            }
        }
        if (socialImage)
        {
            // Crops to fill the frame instead of stretching.
            socialImage.sprite = data.image;
            socialImage.enabled = data.image != null;
            var fitter = socialImage.GetComponent<AspectRatioFitter>();
            if (fitter && data.image) fitter.aspectRatio = data.image.rect.width / data.image.rect.height;
        }
        if (bodyText)
        {
            // No space after a label: Unity's text wraps at spaces, which would push Chinese text to the next line.
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(data.description)) sb.Append(data.description).Append("\n\n");
            sb.Append("<b>").Append(L.T("sc.start")).Append("</b>").Append(data.startingFavor);
            foreach (var outcome in data.outcomes)
                sb.Append("\n\n<b>").Append(outcome.label).Append(L.T("sc.need", outcome.favorNeeded)).Append("</b>").Append(outcome.reward);
            sb.Append("\n\n<b>").Append(L.T("cc.fail")).Append("</b>").Append(data.failureResult);
            if (!string.IsNullOrEmpty(data.special)) sb.Append("\n\n<b>").Append(L.T("cc.special")).Append("</b>").Append(data.special);
            bodyText.text = sb.ToString();
        }
    }
}
