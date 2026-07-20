using UnityEngine;
using System.Collections;

public class Card : MonoBehaviour{
    [SerializeField] protected Sprite CardImage;
    [SerializeField] private string streamingImagePath;
    [SerializeField] private string officialImageUrl;
    [SerializeField] private Sprite BackImage;
    [SerializeField] private Vector2 renderedCardSize = new Vector2(2.32f, 3.2f);
    private SpriteRenderer mSpriteRenderer;
    public int cardID;
    public string cardName;
    [SerializeField] private bool flip = false;

    public Sprite FrontImage => CardImage;
    public Sprite BackImageSprite => BackImage;
    public string DisplayName => string.IsNullOrWhiteSpace(cardName) ? name : cardName;
    public string StreamingImagePath => streamingImagePath;
    public string OfficialImageUrl => officialImageUrl;

    public void Configure(CardDefinition definition)
    {
        if (definition == null) return;
        cardID = definition.id;
        cardName = definition.name;
        Type = definition.CardType;
        officialImageUrl = definition.imageUrl;
        streamingImagePath = string.Empty;
        gameObject.name = $"{definition.id:D5}_{definition.cardNumber}";
        if (isActiveAndEnabled && CardImage == null && !string.IsNullOrWhiteSpace(officialImageUrl))
            StartCoroutine(LoadOfficialImage());
    }

    private Sprite runtimeSprite;
    private bool acquiredCachedImage;

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
        ConfigureRendererSize();
        posStatus = PositionStatus.InDeck;
    }
    

    void Start()
    {
        ApplyFrontSprite(CardImage);
        
    }

    private void OnEnable()
    {
        if (CardImage == null && !string.IsNullOrWhiteSpace(officialImageUrl))
            StartCoroutine(LoadOfficialImage());
    }

    private void OnDisable()
    {
        if (acquiredCachedImage)
        {
            if (CardImage == runtimeSprite) CardImage = null;
            CardImageCache.Release(officialImageUrl);
        }
        runtimeSprite = null;
        acquiredCachedImage = false;
    }

    private IEnumerator LoadOfficialImage()
    {
        yield return CardImageCache.Acquire(officialImageUrl, sprite =>
        {
            if (sprite == null) return;
            if (!isActiveAndEnabled)
            {
                CardImageCache.Release(officialImageUrl);
                return;
            }
            runtimeSprite = sprite;
            acquiredCachedImage = true;
            CardImage = sprite;
            ApplyFrontSprite(sprite);
        });
    }

    private void ConfigureRendererSize()
    {
        if (mSpriteRenderer == null) return;
        mSpriteRenderer.drawMode = SpriteDrawMode.Sliced;
        mSpriteRenderer.size = renderedCardSize;
    }

    private void ApplyFrontSprite(Sprite sprite)
    {
        if (mSpriteRenderer == null) return;
        ConfigureRendererSize();
        mSpriteRenderer.sprite = sprite;
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
