using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] protected Sprite CardImage;
    [SerializeField] private Sprite BackImage;
    private SpriteRenderer mSpriteRenderer;

    [SerializeField] private bool flip = false;

    PositionStatus PosStatus;
    public enum PositionStatus
    {
        InDeck,
        InHand,
        InBoard,
        InGrave
    }


    void Awake()
    {
        mSpriteRenderer = GetComponent<SpriteRenderer>();
        PosStatus = PositionStatus.InDeck;
    }
    

    void Start()
    {
        mSpriteRenderer.sprite = CardImage;
        
    }


    void Update()
    {
        if (flip)
        {
            FlipCard();
            flip = false;
        }
    }

    void FlipCard()
    {
        if (mSpriteRenderer.sprite == BackImage)
        {
            mSpriteRenderer.sprite = CardImage;
        }
        else
        {
            mSpriteRenderer.sprite = BackImage;
        }
    }
}
