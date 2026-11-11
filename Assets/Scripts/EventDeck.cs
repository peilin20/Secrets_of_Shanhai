using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The event card pile. Players can't draw from it directly: a story card's draw button calls Draw().
// Drawn cards fly from the pile into the first empty event slots.
public class EventDeck : MonoBehaviour
{
    public List<EventData> cards = new List<EventData>();

    [Header("UI")]
    public EventCard eventCardPrefab;
    public RectTransform[] slots;
    public Text countText;

    [Header("Animation")]
    public float flyTime = 0.5f;
    public float delayBetweenCards = 0.15f;
    public float cardScale = 0.5f;   // Size of a card sitting in a slot.

    readonly List<EventData> pile = new List<EventData>();

    int totalDrawn;

    public int CardsLeft => pile.Count;
    public int EventsOnTable => slots.Length - FreeSlots;

    public int FreeSlots
    {
        get
        {
            int free = 0;
            foreach (var slot in slots) if (slot.childCount == 0) free++;
            return free;
        }
    }

    void Start()
    {
        pile.AddRange(cards);
        for (int i = pile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pile[i], pile[j]) = (pile[j], pile[i]);
        }
        UpdateCount();
    }

    // Draws up to `count` cards into empty slots. Returns how many were drawn.
    public int Draw(int count)
    {
        int drawn = 0;
        foreach (var slot in slots)
        {
            if (drawn >= count || pile.Count == 0) break;
            if (slot.childCount > 0) continue;

            // Take the top card, except that the game's very first event can't be one marked notFirstEvent.
            int index = pile.Count - 1;
            if (totalDrawn == 0)
                while (index > 0 && pile[index].notFirstEvent) index--;
            var data = pile[index];
            pile.RemoveAt(index);
            totalDrawn++;

            // The card belongs to its slot right away, so the slot counts as taken while it flies.
            var card = Instantiate(eventCardPrefab, slot);
            card.SetEvent(data);
            StartCoroutine(FlyIn(card.transform, drawn * delayBetweenCards));
            drawn++;
        }
        UpdateCount();
        return drawn;
    }

    // Draws these exact cards (e.g. a story card's own events) into empty slots, taking them out of the pile if they're in it.
    public int DrawSpecific(List<EventData> wanted)
    {
        int drawn = 0;
        int next = 0;
        foreach (var slot in slots)
        {
            while (next < wanted.Count && !wanted[next]) next++;
            if (next >= wanted.Count) break;
            if (slot.childCount > 0) continue;
            var data = wanted[next++];
            pile.Remove(data);
            totalDrawn++;
            var card = Instantiate(eventCardPrefab, slot);
            card.SetEvent(data);
            StartCoroutine(FlyIn(card.transform, drawn * delayBetweenCards));
            drawn++;
        }
        UpdateCount();
        return drawn;
    }

    IEnumerator FlyIn(Transform card, float delay)
    {
        var rt = (RectTransform)card;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        Vector3 start = transform.position;
        Vector3 startScale = Vector3.one * cardScale * 0.3f;
        card.position = start;
        card.localScale = startScale;
        card.gameObject.SetActive(delay <= 0f);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
            card.gameObject.SetActive(true);
        }

        for (float t = 0f; t < flyTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / flyTime);
            card.position = Vector3.Lerp(start, card.parent.position, k);
            card.localScale = Vector3.Lerp(startScale, Vector3.one * cardScale, k);
            yield return null;
        }
        rt.anchoredPosition = Vector2.zero;
        card.localScale = Vector3.one * cardScale;
    }

    void UpdateCount()
    {
        if (countText) countText.text = pile.Count > 0 ? pile.Count + " left" : "Empty";
    }
}
