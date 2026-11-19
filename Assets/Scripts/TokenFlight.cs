using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Reusable "a token flies to a character" animation. A copy of the token leaves the token pool
// (the library token of that element at the top of the table; the middle of the screen if there is none),
// arcs over to the character card's token area, and only on arrival is the real token added
// (CharacterCard.GainToken, so 负伤 that block or add tokens still apply).
//
//   yield return TokenFlight.Give(card, Element.Fire, tokenPrefab);              // one token
//   yield return TokenFlight.GiveEach(cards, c => c.primaryElement, tokenPrefab); // one each, a little apart
public class TokenFlight : MonoBehaviour
{
    public static float duration = 0.55f;   // Seconds per flight.
    public static float stagger = 0.12f;    // Gap between flights that start together.
    public static float arcHeight = 120f;   // How high the path bows (canvas units).

    static TokenFlight runner;

    static TokenFlight Runner
    {
        get
        {
            if (!runner) runner = new GameObject("TokenFlights").AddComponent<TokenFlight>();
            return runner;
        }
    }

    // One token flies to the card; finishes when it has landed. `from` overrides where it starts.
    public static IEnumerator Give(CharacterCard card, Element element, ElementToken prefab, bool temporary = false, RectTransform from = null)
    {
        if (!card || !prefab) yield break;
        bool done = false;
        Runner.StartCoroutine(Fly(card, element, prefab, temporary, from, 0f, () => done = true));
        while (!done) yield return null;
    }

    // One token for each card (element chosen per card), started a little apart; finishes when all have landed.
    public static IEnumerator GiveEach(IEnumerable<CharacterCard> cards, System.Func<CharacterCard, Element> element, ElementToken prefab, bool temporary = false)
    {
        if (!prefab) yield break;
        int pending = 0;
        float delay = 0f;
        foreach (var card in cards)
        {
            if (!card) continue;
            pending++;
            Runner.StartCoroutine(Fly(card, element(card), prefab, temporary, null, delay, () => pending--));
            delay += stagger;
        }
        while (pending > 0) yield return null;
    }

    static IEnumerator Fly(CharacterCard card, Element element, ElementToken prefab, bool temporary, RectTransform from, float delay, System.Action done)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        var canvas = card.GetComponentInParent<Canvas>();
        var area = card.tokenArea ? card.tokenArea : (RectTransform)card.transform;
        if (!canvas || !card.gameObject.activeInHierarchy)
        {
            card.GainToken(element, prefab, temporary);
            done();
            yield break;
        }
        canvas = canvas.rootCanvas;
        if (!from) from = Pool(element);

        // The flying copy: just the token's look, on top of everything in the table's canvas.
        var go = new GameObject("FlyingToken", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.SetAsLastSibling();
        var image = go.GetComponent<Image>();
        var look = prefab.GetComponent<Image>();
        image.sprite = look ? look.sprite : null;
        image.preserveAspect = true;
        image.raycastTarget = false;
        var color = ElementToken.ColorFor(element);
        if (temporary) color.a = 0.5f;
        image.color = color;
        var prefabRt = (RectTransform)prefab.transform;
        Vector2 size = prefabRt.rect.size.sqrMagnitude > 0f ? prefabRt.rect.size : new Vector2(40f, 40f);
        rt.sizeDelta = size;

        Vector3 start = from ? from.position : canvas.transform.position;
        Vector3 end = area.position;
        // The path bows towards the middle of the screen (down when it runs along the top, up along the bottom).
        float lift = arcHeight * canvas.transform.lossyScale.y;
        if ((start.y + end.y) * 0.5f > canvas.transform.position.y) lift = -lift;
        Vector3 startScale = Vector3.one * 1.3f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (!go) break;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            end = area ? area.position : end;   // The card may move (e.g. dragged) mid-flight.
            rt.position = Vector3.Lerp(start, end, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * lift);
            rt.localScale = Vector3.Lerp(startScale, Vector3.one, k);
            yield return null;
        }
        if (go) Destroy(go);
        card.GainToken(element, prefab, temporary);
        done();
    }

    // The token pool: the library token of this element, if the table has one.
    static RectTransform Pool(Element element)
    {
        foreach (var token in FindObjectsByType<ElementToken>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (token.isLibrarySource && token.element == element) return (RectTransform)token.transform;
        return null;
    }
}
