using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Shows one CombatData on a card: name, enemy element, picture, then 全胜 / 胜利 / 失败 / 特殊.
public class CombatCard : MonoBehaviour
{
    public CombatData data;

    [Header("UI")]
    public Text titleText;
    public Image elementCircle;
    public Text elementText;
    public Image combatImage;
    public Text bodyText;

    public static string ElementChar(Element e)
    {
        switch (e)
        {
            case Element.Wood: return "木";
            case Element.Fire: return "火";
            case Element.Earth: return "土";
            case Element.Metal: return "金";
            case Element.Water: return "水";
        }
        return "";
    }

    void Start()
    {
        Refresh();
    }

    void OnValidate()
    {
        Refresh();
    }

    public void SetCombat(CombatData combat)
    {
        data = combat;
        Refresh();
    }

    public void Refresh()
    {
        if (!data) return;
        if (titleText) titleText.text = data.title;
        if (elementCircle) elementCircle.color = ElementToken.ColorFor(data.element);
        if (elementText) elementText.text = ElementChar(data.element);
        if (combatImage)
        {
            // Crops to fill the frame instead of stretching.
            combatImage.sprite = data.image;
            combatImage.enabled = data.image != null;
            var fitter = combatImage.GetComponent<AspectRatioFitter>();
            if (fitter && data.image) fitter.aspectRatio = data.image.rect.width / data.image.rect.height;
        }
        if (bodyText)
        {
            // No space after a label: Unity's text wraps at spaces, which would push Chinese text to the next line.
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(data.description)) sb.Append(data.description).Append("\n\n");
            sb.Append("<b>全胜(>=").Append(data.greatSuccessThreshold).Append("战斗值):</b>").Append(data.greatSuccessReward).Append("\n\n");
            sb.Append("<b>胜利(>=").Append(data.successThreshold).Append("战斗值):</b>").Append(data.successReward).Append("\n\n");
            sb.Append("<b>失败：</b>").Append(data.failureResult);
            if (!string.IsNullOrEmpty(data.special)) sb.Append("\n\n<b>特殊：</b>").Append(data.special);
            bodyText.text = sb.ToString();
        }
    }
}
