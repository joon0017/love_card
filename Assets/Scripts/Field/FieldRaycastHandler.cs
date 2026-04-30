using UnityEngine;

public class FieldRaycastHandler : MonoBehaviour
{
    private bool hasCard = false;
    [SerializeField]
    private Card.PositionStatus fieldPosition;
    private Card occupyingCard;

    public Card.PositionStatus FieldPosition => fieldPosition;

    public int GetRaycastStatus(){
        return (int)fieldPosition;
    }

    public bool HasCard(){
        return hasCard || occupyingCard != null;
    }

    public bool CanAcceptCard(Card card){
        return !HasCard() || occupyingCard == card;
    }

    public void Occupy(Card card){
        if(card == null) return;

        occupyingCard = card;
        hasCard = true;
        card.SetPositionStatus(fieldPosition);
    }

    public void Clear(Card card = null){
        if(card != null && occupyingCard != null && occupyingCard != card) return;

        occupyingCard = null;
        hasCard = false;
    }

    public void SetHasCard(bool status){
        hasCard = status;
        if(!status) occupyingCard = null;
    }

}
