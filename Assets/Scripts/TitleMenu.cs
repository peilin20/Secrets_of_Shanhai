using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Title screen: 开始游戏 loads the game scene (which opens on character creation),
// 设置 and Credits open their panels, each with a 返回 button.
public class TitleMenu : MonoBehaviour
{
    public string gameScene = "SampleScene";
    public Button startButton;
    public Button settingsButton;
    public Button creditsButton;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public Button settingsBackButton;
    public Button creditsBackButton;

    void Awake()
    {
        if (startButton) startButton.onClick.AddListener(() => SceneManager.LoadScene(gameScene));
        // The same settings panel as the 设置 button in the corner of every scene.
        if (settingsButton) settingsButton.onClick.AddListener(OpenSharedSettings);
        if (creditsButton) creditsButton.onClick.AddListener(() => Open(creditsPanel));
        if (settingsBackButton) settingsBackButton.onClick.AddListener(CloseAll);
        if (creditsBackButton) creditsBackButton.onClick.AddListener(CloseAll);
        CloseAll();
    }

    void OpenSharedSettings()
    {
        CloseAll();
        var overlay = GameObject.Find("SettingsOverlay");
        var panel = overlay ? overlay.transform.Find("SettingsPanel") : null;
        if (panel) panel.gameObject.SetActive(true);
        else Open(settingsPanel);
    }

    void Open(GameObject panel)
    {
        CloseAll();
        if (panel) panel.SetActive(true);
    }

    void CloseAll()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
    }
}
