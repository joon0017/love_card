using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    public static HandManager instance;
    private void Awake() {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private List<GameObject> cards = new List<GameObject>();

    public void AddCardToList(GameObject card)
    {
        if (card == null) return;
        if (cards.Contains(card))
        {
            UpdateCardPositions();
            return;
        }

        cards.Add(card);
        UpdateCardPositions();
    }

    public void RemoveCardFromList(GameObject card)
    {
        if (card == null) return;

        cards.Remove(card);
        UpdateCardPositions();
    }

    public bool IsCardInHand(GameObject card)
    {
        return cards.Contains(card);
    }

    [SerializeField] float maxSpacing = 1.5f;
    [SerializeField] float minSpacing = 0.5f;
    [SerializeField] int maxCardsForScaling = 10;
    [SerializeField] private Vector3 startPosition = Vector3.zero;
    public void UpdateCardPositions()
    {
        cards.RemoveAll(card => card == null);

        int cardNum = cards.Count;
        if (cardNum == 0) return;

        int scalingRange = Mathf.Max(1, maxCardsForScaling - 1);
        float t = Mathf.Clamp01((cardNum - 1) / (float)scalingRange);
        float spacing = Mathf.Lerp(maxSpacing, minSpacing, t);

        float totalWidth = (cardNum - 1) * spacing;
        float startOffset = -totalWidth / 2f;

        for (int i = 0; i < cardNum; i++){
            Vector3 targetPos = startPosition + new Vector3(startOffset + i * spacing, 0f, - i * 0.01f);
            cards[i].transform.position = targetPos;
        }
    }
}
