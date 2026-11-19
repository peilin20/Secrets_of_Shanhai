using UnityEngine;
using UnityEngine.UI;

// Put on a Text to show L.T(key) in the current language. Labels already in the scene whose text matches
// a string in the Localization table get this added automatically when the game starts.
[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour
{
    public string key;

    void OnEnable()
    {
        L.Changed += Apply;
        Apply();
    }

    void OnDisable()
    {
        L.Changed -= Apply;
    }

    public void Apply()
    {
        if (!string.IsNullOrEmpty(key)) GetComponent<Text>().text = L.T(key);
    }
}
