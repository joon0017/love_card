using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class Card : MonoBehaviour{
    [SerializeField] protected Sprite CardImage;
    [SerializeField] private string streamingImagePath;
    [SerializeField] private Sprite BackImage;
    private SpriteRenderer mSpriteRenderer;
    public int cardID;
    public string cardName;
    [SerializeField] private bool flip = false;

    public Sprite FrontImage => CardImage;
    public Sprite BackImageSprite => BackImage;
    public string DisplayName => string.IsNullOrWhiteSpace(cardName) ? name : cardName;
    public string StreamingImagePath => streamingImagePath;

    private Sprite runtimeSprite;
    private Texture2D runtimeTexture;

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

    private void OnEnable()
    {
        if (CardImage == null && !string.IsNullOrWhiteSpace(streamingImagePath))
            StartCoroutine(LoadStreamingImage());
    }

    private void OnDisable()
    {
        if (runtimeSprite != null)
        {
            if (CardImage == runtimeSprite) CardImage = null;
            Destroy(runtimeSprite);
        }
        if (runtimeTexture != null) Destroy(runtimeTexture);
        runtimeSprite = null;
        runtimeTexture = null;
    }

    private IEnumerator LoadStreamingImage()
    {
        string path = Path.Combine(Application.streamingAssetsPath, streamingImagePath).Replace('\\', '/');
        string uri = path.Contains("://") ? path : "file:///" + path;
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(uri))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not load card image '{streamingImagePath}': {request.error}", this);
                yield break;
            }

            runtimeTexture = DownloadHandlerTexture.GetContent(request);
            runtimeTexture.name = cardName + " Texture";
            runtimeSprite = Sprite.Create(runtimeTexture, new Rect(0, 0, runtimeTexture.width, runtimeTexture.height), new Vector2(0.5f, 0.5f), 100f);
            runtimeSprite.name = cardName + " Sprite";
            CardImage = runtimeSprite;
            if (mSpriteRenderer != null) mSpriteRenderer.sprite = runtimeSprite;
        }
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
