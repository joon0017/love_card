using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(100)]
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum GameState
    {
        NotStarted,
        Playing,
        Finished
    }

    public enum GamePhase
    {
        Setup,
        Draw,
        Main,
        Live,
        End
    }

    [System.Serializable]
    public class PhaseChangedEvent : UnityEvent<GamePhase> { }

    [System.Serializable]
    public class TurnChangedEvent : UnityEvent<int> { }

    [System.Serializable]
    public class CardDrawnEvent : UnityEvent<Card> { }

    [Header("Scene References")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private HandManager handManager;

    [Header("Game Start")]
    [SerializeField] private bool autoStartGame = true;
    [SerializeField] private bool createMissingManagers = true;
    [SerializeField] private bool shuffleOnStart = true;
    [SerializeField, Min(0)] private int openingHandSize = 5;
    [SerializeField] private GamePhase firstPhaseAfterSetup = GamePhase.Draw;

    [Header("Draw Phase")]
    [SerializeField] private bool drawAtStartOfDrawPhase = true;
    [SerializeField] private bool skipFirstTurnDraw = true;

    [Header("Events")]
    public UnityEvent OnGameStarted = new UnityEvent();
    public UnityEvent OnGameFinished = new UnityEvent();
    public PhaseChangedEvent OnPhaseChanged = new PhaseChangedEvent();
    public TurnChangedEvent OnTurnChanged = new TurnChangedEvent();
    public CardDrawnEvent OnCardDrawn = new CardDrawnEvent();

    public GameState State { get; private set; } = GameState.NotStarted;
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Setup;
    public int TurnNumber { get; private set; } = 0;
    public int RemainingDeckCards => deckManager != null ? deckManager.RemainingCards : 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ResolveManagers();
    }

    private void Start()
    {
        if (autoStartGame)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (State == GameState.Playing) return;

        ResolveManagers();
        if (deckManager == null)
        {
            Debug.LogError("Cannot start game because no DeckManager exists.");
            return;
        }

        State = GameState.Playing;
        TurnNumber = 1;
        CurrentPhase = GamePhase.Setup;

        if (shuffleOnStart)
        {
            deckManager.ShuffleDeck();
        }

        OnGameStarted.Invoke();
        OnTurnChanged.Invoke(TurnNumber);

        DrawCards(openingHandSize);
        SetPhase(firstPhaseAfterSetup, true);
    }

    public void FinishGame()
    {
        if (State == GameState.Finished) return;

        State = GameState.Finished;
        OnGameFinished.Invoke();
    }

    public void NextPhase()
    {
        if (State == GameState.NotStarted)
        {
            StartGame();
            return;
        }

        if (State != GameState.Playing) return;

        switch (CurrentPhase)
        {
            case GamePhase.Setup:
                SetPhase(GamePhase.Draw);
                break;
            case GamePhase.Draw:
                SetPhase(GamePhase.Main);
                break;
            case GamePhase.Main:
                SetPhase(GamePhase.Live);
                break;
            case GamePhase.Live:
                SetPhase(GamePhase.End);
                break;
            case GamePhase.End:
                StartNextTurn();
                break;
        }
    }

    public void StartNextTurn()
    {
        if (State != GameState.Playing) return;

        TurnNumber++;
        OnTurnChanged.Invoke(TurnNumber);
        SetPhase(GamePhase.Draw, true);
    }

    public void SetDrawPhase()
    {
        SetPhase(GamePhase.Draw);
    }

    public void SetMainPhase()
    {
        SetPhase(GamePhase.Main);
    }

    public void SetLivePhase()
    {
        SetPhase(GamePhase.Live);
    }

    public void SetEndPhase()
    {
        SetPhase(GamePhase.End);
    }

    public void DrawCard()
    {
        ResolveManagers();
        if (deckManager == null) return;

        Card drawnCard = deckManager.DrawCard();
        if (drawnCard != null)
        {
            OnCardDrawn.Invoke(drawnCard);
        }
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            DrawCard();
        }
    }

    public void ShuffleDeck()
    {
        ResolveManagers();
        deckManager?.ShuffleDeck();
    }

    private void SetPhase(GamePhase phase, bool force = false)
    {
        if (State != GameState.Playing) return;
        if (!force && CurrentPhase == phase) return;

        CurrentPhase = phase;
        OnPhaseChanged.Invoke(CurrentPhase);
        HandlePhaseStart();
    }

    private void HandlePhaseStart()
    {
        if (CurrentPhase != GamePhase.Draw) return;
        if (!drawAtStartOfDrawPhase) return;
        if (skipFirstTurnDraw && TurnNumber == 1) return;

        DrawCard();
    }

    private void ResolveManagers()
    {
        if (deckManager == null)
        {
            deckManager = Object.FindFirstObjectByType<DeckManager>();
        }

        if (handManager == null)
        {
            handManager = Object.FindFirstObjectByType<HandManager>();
        }

        if (!createMissingManagers) return;

        if (deckManager == null)
        {
            GameObject deckManagerObject = new GameObject("DeckManager");
            deckManager = deckManagerObject.AddComponent<DeckManager>();
        }

        if (handManager == null)
        {
            GameObject handManagerObject = new GameObject("HandManager");
            handManager = handManagerObject.AddComponent<HandManager>();
        }
    }
}
