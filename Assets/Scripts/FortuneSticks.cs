using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 1d6 roll as a fortune-stick box (抽签): click the box, it shakes, and a wooden stick rises with the number.
public class FortuneSticks : MonoBehaviour
{
    public int sides = 6;

    [Header("UI")]
    public Button boxButton;
    public RectTransform body;      // Shaken as a whole (box + sticks).
    public RectTransform stick;     // The stick that comes out.
    public Text numberText;

    [Header("Animation")]
    public float shakeTime = 0.5f;
    public float shakeAngle = 8f;
    public float riseTime = 0.35f;
    public float hiddenY = 0f;
    public float shownY = 120f;

    public int LastResult { get; private set; }

    bool rolling;

    void Awake()
    {
        if (boxButton) boxButton.onClick.AddListener(Roll);
    }

    void Start()
    {
        SetStickY(hiddenY);
        numberText.text = "";
    }

    public bool IsRolling => rolling;

    public void Roll()
    {
        if (!rolling) StartCoroutine(RollRoutine(null));
    }

    // Rolls and reports the drawn number when the stick is out (used by combat).
    public void Roll(System.Action<int> onDone)
    {
        if (!rolling) StartCoroutine(RollRoutine(onDone));
    }

    IEnumerator RollRoutine(System.Action<int> onDone)
    {
        rolling = true;

        // Put the last stick back in the box.
        numberText.text = "";
        SetStickY(hiddenY);

        // Shake, fading out.
        for (float t = 0f; t < shakeTime; t += Time.deltaTime)
        {
            float fade = 1f - t / shakeTime;
            body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 45f) * shakeAngle * fade);
            yield return null;
        }
        body.localRotation = Quaternion.identity;

        LastResult = Random.Range(1, sides + 1);
        numberText.text = LastResult.ToString();

        // Stick rises out of the box.
        for (float t = 0f; t < riseTime; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / riseTime, 3f);
            SetStickY(Mathf.Lerp(hiddenY, shownY, k));
            yield return null;
        }
        SetStickY(shownY);

        rolling = false;
        onDone?.Invoke(LastResult);
    }

    void SetStickY(float y)
    {
        stick.anchoredPosition = new Vector2(stick.anchoredPosition.x, y);
    }
}
