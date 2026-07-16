using UnityEngine;
using UnityEngine.EventSystems;

public class DressingRoomViewerCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private DressingRoomManager dressingRoomManager;
    private int cardIndex;

    public void Initialize(DressingRoomManager manager, int index)
    {
        dressingRoomManager = manager;
        cardIndex = index;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dressingRoomManager?.BeginViewerCardDrag(this, cardIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        dressingRoomManager?.UpdateViewerCardDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dressingRoomManager?.EndViewerCardDrag(this, cardIndex, eventData);
    }
}
