using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    public static HandManager instance;
    private void Awake() {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private List<GameObject> cards = new List<GameObject>();
    private readonly Dictionary<GameObject, Coroutine> movementCoroutines = new Dictionary<GameObject, Coroutine>();

    public void AddCardToList(GameObject card, bool animate = true)
    {
        if (card == null) return;
        if (cards.Contains(card))
        {
            UpdateCardPositions(animate);
            return;
        }

        cards.Add(card);
        UpdateCardPositions(animate);
    }

    public void RemoveCardFromList(GameObject card)
    {
        if (card == null) return;

        StopCardMovement(card);
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
    [SerializeField, Min(0f)] private float handAnimationDuration = 0.18f;
    [SerializeField] private Vector3 startPosition = Vector3.zero;
    public void UpdateCardPositions()
    {
        UpdateCardPositions(false);
    }

    public void UpdateCardPositions(bool animate)
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
            MoveCardToHandPosition(cards[i], targetPos, animate);
        }
    }

    private void MoveCardToHandPosition(GameObject card, Vector3 targetPosition, bool animate)
    {
        if (card == null) return;

        StopCardMovement(card);
        if (!animate || handAnimationDuration <= 0f || !gameObject.activeInHierarchy)
        {
            card.transform.position = targetPosition;
            return;
        }

        movementCoroutines[card] = StartCoroutine(AnimateCardToPosition(card, targetPosition));
    }

    private IEnumerator AnimateCardToPosition(GameObject card, Vector3 targetPosition)
    {
        Vector3 start = card.transform.position;
        float elapsed = 0f;

        while (elapsed < handAnimationDuration)
        {
            if (card == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / handAnimationDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            card.transform.position = Vector3.Lerp(start, targetPosition, easedT);
            yield return null;
        }

        if (card != null)
        {
            card.transform.position = targetPosition;
            movementCoroutines.Remove(card);
        }
    }

    private void StopCardMovement(GameObject card)
    {
        if (card == null) return;
        if (!movementCoroutines.TryGetValue(card, out Coroutine coroutine)) return;

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        movementCoroutines.Remove(card);
    }
}
