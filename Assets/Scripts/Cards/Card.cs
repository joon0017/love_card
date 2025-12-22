using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] protected Sprite CardImage;
    [SerializeField] private Sprite BackImage;
    private SpriteRenderer mSpriteRenderer;
    public int cardID;
    public string cardName;
    [SerializeField] private bool flip = false;

    public PositionStatus PosStatus;
    public CardType Type;
    public enum PositionStatus
    {
        InDeck=1,
        InHand=2,
        InBoard=3,
        InGrave=4
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

    private void OnCollisionEnter(Collision other) {
        Debug.Log("Collided with " + other.gameObject.name);
        this.gameObject.transform.parent = other.transform;

    }
    private void OnCollisionExit(Collision other) {
        Debug.Log("Stopped Colliding with " + other.gameObject.name);
        this.gameObject.transform.parent = null;
    }
}
