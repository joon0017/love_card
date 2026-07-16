using UnityEngine;

public class Card : MonoBehaviour{
    [SerializeField] protected Sprite CardImage;
    [SerializeField] private Sprite BackImage;
    private SpriteRenderer mSpriteRenderer;
    public int cardID;
    public string cardName;
    [SerializeField] private bool flip = false;

    public Sprite FrontImage => CardImage;
    public Sprite BackImageSprite => BackImage;
    public string DisplayName => string.IsNullOrWhiteSpace(cardName) ? name : cardName;

    public PositionStatus posStatus;
    public CardType Type;
    public enum PositionStatus
    {
        InDeck=1,
        InHand=2,
        InDiscard=3,
        DressingRoom=3,
        FieldLeft=4,
        FieldCenter=5,
        FieldRight=6,
        FieldLiveLeft=7,
        FieldLiveCenter=8,
        FieldLiveRight=9
    }

    public int GetPositionStatus(){
        return (int)posStatus;
    }

    public enum CardType
    {
        Member=1,
        Live=2,
        Energy=3
    }


    void Awake()
    {
        mSpriteRenderer = GetComponent<SpriteRenderer>();
        posStatus = PositionStatus.InDeck;
    }
    

    void Start()
    {
        if(mSpriteRenderer != null) mSpriteRenderer.sprite = CardImage;
        
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
        if(mSpriteRenderer == null) return;

        if (mSpriteRenderer.sprite == BackImage)
        {
            mSpriteRenderer.sprite = CardImage;
        }
        else
        {
            mSpriteRenderer.sprite = BackImage;
        }
    }


    public void SetPositionStatus(PositionStatus status){
        posStatus = status;
    }

    public void SetRaycastStatue(int index){
        SetPositionStatus((PositionStatus)index);
    }

    public void AddToHand(bool animate = true){
        posStatus = PositionStatus.InHand;

        if(HandManager.instance == null){
            Debug.LogWarning("Cannot add card to hand because no HandManager exists in the scene.");
            return;
        }

        HandManager.instance.AddCardToList(this.gameObject, animate);
    }

}
