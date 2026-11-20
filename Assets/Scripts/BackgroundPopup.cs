using UnityEngine;
using UnityEngine.UI;

// Small window that shows a background's full description.
public class BackgroundPopup : MonoBehaviour
{
    public Text titleText;
    public Text descriptionText;
    public Button closeButton;
    public Button dimButton;

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
        if (dimButton) dimButton.onClick.AddListener(Hide);
    }

    public void Show(BackgroundData background)
    {
        if (!background) return;
        titleText.text = background.Name;
        descriptionText.text = background.Description;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
