using CosmicShore.Core;
using CosmicShore.Game.IO;
using System.Collections;
using System.Collections.Generic;
using CosmicShore.Integrations.PlayFab.PlayStream;
using UnityEngine;
using UnityEngine.UI;
using CosmicShore.App.Systems.UserActions;
using CosmicShore.Game.UI;
using UnityEngine.Serialization;
using System.Linq;

namespace CosmicShore.Game.Arcade
{
    public class MiniGame : MonoBehaviour
    {
        [SerializeField] protected MiniGames gameMode;
        [SerializeField] protected int NumberOfRounds = int.MaxValue;
        [SerializeField] protected List<TurnMonitor> TurnMonitors;
        [SerializeField] protected ScoreTracker ScoreTracker;
        [SerializeField] GameCanvas GameCanvas;
        [SerializeField] Player playerPrefab;
        [SerializeField] GameObject PlayerOrigin;
        [SerializeField] float EndOfTurnDelay = 0f;
        [SerializeField] bool EnableTrails = true;
        [SerializeField] ShipTypes DefaultPlayerShipType = ShipTypes.Dolphin;
        [FormerlySerializedAs("DefaultPlayerGuide")]
        [SerializeField] SO_Guide DefaultPlayerCaptain;

        protected Button ReadyButton;
        protected GameObject EndGameScreen;
        protected MiniGameHUD HUD;
        protected LinkedList<Player> Players = new();
        protected CountdownTimer countdownTimer;

        List<Teams> PlayerTeams = new() { Teams.Green, Teams.Red, Teams.Gold };
        List<string> PlayerNames = new() { "PlayerOne", "PlayerTwo", "PlayerThree" };

        /// <summary>
        /// This value DETERMINES the number of players for this session as opposed
        /// to tracking it.
        /// </summary>
        public static int NumberOfPlayers = 1;  // TODO: P1 - support excluding single player games (e.g for elimination)
        public static int IntensityLevel = 1;
        static ShipTypes playerShipType = ShipTypes.Dolphin;
        static bool playerShipTypeInitialized;

        public static ShipTypes PlayerShipType
        {
            get
            {
                return playerShipType;
            }
            set
            {
                playerShipType = value;
                playerShipTypeInitialized = true;
            }
        }
        public static SO_Guide PlayerCaptain;

        // Game State Tracking
        protected int TurnsTakenThisRound = 0;
        int RoundsPlayedThisGame = 0;

        // PlayerId Tracking
        Player RemainingPlayersActivePlayer;
        protected LinkedList<Player> RemainingPlayers = new();
        [HideInInspector] public Player ActivePlayer;
        protected bool gameRunning;

        // Firebase analytics events
        public delegate void MiniGameStart(MiniGames mode, ShipTypes ship, int playerCount, int intensity);
        public static event MiniGameStart OnMiniGameStart;

        public delegate void MiniGameEnd(MiniGames mode, ShipTypes ship, int playerCount, int intensity, int highScore);

        public static event MiniGameEnd OnMiniGameEnd;

        protected virtual void Awake()
        {
            EndGameScreen = GameCanvas.EndGameScreen;
            HUD = GameCanvas.MiniGameHUD;
            ReadyButton = HUD.ReadyButton;
            countdownTimer = HUD.CountdownTimer;
            ScoreTracker.GameCanvas = GameCanvas;

            if (DefaultPlayerCaptain == null)
            {
                Debug.LogError("No Default Captain Set - This scene will not be able to launch without going through the main menu. Please set DefaultPlayerCaptain of the minigame script.");
            }

            if (PlayerCaptain == null)
            {
                PlayerCaptain = DefaultPlayerCaptain;
            }

            for (int i = 0; i < TurnMonitors.Count; i++)
            {
                var turnMonitor = TurnMonitors[i];
                if (turnMonitor is TimeBasedTurnMonitor tbtMonitor)
                {
                    tbtMonitor.Display = HUD.RoundTimeDisplay;
                }
                else if (turnMonitor is VolumeCreatedTurnMonitor hvtMonitor) // TODO: consolidate with above
                {
                    hvtMonitor.Display = HUD.RoundTimeDisplay;
                }
                else if (turnMonitor is ShipCollisionTurnMonitor scMonitor) // TODO: consolidate with above
                {
                    scMonitor.Display = HUD.RoundTimeDisplay;
                }
                else if (turnMonitor is DistanceTurnMonitor dtMonitor) // TODO: consolidate with above
                {
                    dtMonitor.Display = HUD.RoundTimeDisplay;
                }
            }

            GameManager.UnPauseGame();
        }

        protected virtual void Start()
        {
            Players.Clear();
            for (var i = 0; i < NumberOfPlayers; i++)
            {
                Player currentPlayer = Instantiate(playerPrefab);
                Players.AddLast(currentPlayer);
                currentPlayer.defaultShip = playerShipTypeInitialized ? PlayerShipType : DefaultPlayerShipType;
                currentPlayer.Team = PlayerTeams[i];
                currentPlayer.PlayerName = PlayerNames[i];
                currentPlayer.PlayerUUID = PlayerNames[i];
                currentPlayer.name = "Player" + (i + 1);
                currentPlayer.gameObject.SetActive(true);
            }

            ReadyButton.onClick.AddListener(OnReadyClicked);
            ReadyButton.gameObject.SetActive(false);

            // Give other objects a few moments to start
            StartCoroutine(StartNewGameCoroutine());
        }

        IEnumerator StartNewGameCoroutine()
        {
            yield return new WaitForSeconds(.2f);

            StartNewGame();
        }

        public void OnReadyClicked()
        {
            ReadyButton.gameObject.SetActive(false);

            countdownTimer.BeginCountdown(() =>
            {
                StartTurn();

                ActivePlayer.GetComponent<InputController>().Paused = false;

                if (EnableTrails)
                {
                    ActivePlayer.Ship.TrailSpawner.ForceStartSpawningTrail();
                    ActivePlayer.Ship.TrailSpawner.RestartTrailSpawnerAfterDelay(2f);
                }
            });
        }

        public virtual void StartNewGame()
        {
            //Debug.Log($"Playing as {PlayerGuide.Name} - \"{PlayerGuide.Description}\"");
            if (PauseSystem.Paused)
            {
                PauseSystem.TogglePauseGame();
            }

            RemainingPlayers = new(Players);

            StartGame();
        }

        protected virtual void Update()
        {
            if (!gameRunning)
            {
                return;
            }

            for (int i = 0; i < TurnMonitors.Count; i++)
            {
                var turnMonitor = TurnMonitors[i];
                if (turnMonitor.CheckForEndOfTurn())
                {
                    EndTurn();
                    return;
                }
            }
        }

        void StartGame()
        {
            gameRunning = true;
            Debug.Log($"MiniGame.StartGame, ... {Time.time}");
            EndGameScreen.SetActive(false);
            RoundsPlayedThisGame = 0;
            OnMiniGameStart?.Invoke(gameMode, PlayerShipType, Players.Count, IntensityLevel);
            StartRound();
        }

        void StartRound()
        {
            Debug.Log($"MiniGame.StartRound - Round {RoundsPlayedThisGame + 1} Start, ... {Time.time}");
            TurnsTakenThisRound = 0;
            SetupTurn();
        }

        protected void StartTurn()
        {
            for (int i = 0; i < TurnMonitors.Count; i++)
            {
                TurnMonitors[i].ResumeTurn();
            }

            ScoreTracker.StartTurn(ActivePlayer.PlayerName, ActivePlayer.Team);

            Debug.Log($"Player {ActivePlayer.PlayerName} Get Ready! {Time.time}");
        }

        protected virtual void EndTurn()
        {
            Player.ActivePlayer.Ship.TryGetComponent<Silhouette>(out Silhouette silhouette);
            silhouette.Clear();
            StartCoroutine(EndTurnCoroutine());
        }

        IEnumerator EndTurnCoroutine()
        {
            for (int i = 0; i < TurnMonitors.Count; i++)
            {
                TurnMonitors[i].PauseTurn();
            }
            ActivePlayer.GetComponent<InputController>().Paused = true;
            ActivePlayer.Ship.TrailSpawner.PauseTrailSpawner();

            yield return new WaitForSeconds(EndOfTurnDelay);

            TurnsTakenThisRound++;

            ScoreTracker.EndTurn();
            Debug.Log($"MiniGame.EndTurn - Turns Taken: {TurnsTakenThisRound}, ... {Time.time}");

            if (TurnsTakenThisRound >= RemainingPlayers.Count)
            {
                EndRound();
            }
            else
            {
                SetupTurn();
            }
        }

        protected void EndRound()
        {
            RoundsPlayedThisGame++;

            ResolveEliminations();

            Debug.Log($"MiniGame.EndRound - Rounds Played: {RoundsPlayedThisGame}, ... {Time.time}");

            if (RoundsPlayedThisGame >= NumberOfRounds || RemainingPlayers.Count <= 0)
            {
                EndGame();
            }
            else
            {
                StartRound();
            }
        }

        void EndGame()
        {
            Debug.Log($"MiniGame.EndGame - Rounds Played: {RoundsPlayedThisGame}, ... {Time.time}");
            Debug.Log($"MiniGame.EndGame - Winner: {ScoreTracker.GetWinner()} ");

            LinkedListNode<Player> currentPlayer = Players.First;
            while (currentPlayer != null)
            {
                Debug.Log($"MiniGame.EndGame - Player Score: {ScoreTracker.GetScore(currentPlayer.Value.PlayerName)} ");
                currentPlayer = currentPlayer.Next;
            }

            LeaderboardManager.Instance.ReportGameplayStatistic(gameMode, PlayerShipType, IntensityLevel, ScoreTracker.GetHighScore(), ScoreTracker.GolfRules);

            UserActionSystem.Instance.CompleteAction(new UserAction(
                    UserActionType.PlayGame,
                    ScoreTracker.GetHighScore(),
                    UserAction.GetGameplayUserActionLabel(gameMode, PlayerShipType, IntensityLevel)));

            CameraManager.Instance.SetEndCameraActive();
            PauseSystem.TogglePauseGame();
            gameRunning = false;
            EndGameScreen.SetActive(true);
            ScoreTracker.DisplayScores();
            OnMiniGameEnd?.Invoke(gameMode, PlayerShipType, NumberOfPlayers, IntensityLevel, ScoreTracker.GetHighScore());
        }

        void LoopActivePlayer()
        {
            LinkedListNode<Player> currentActivePlayer = RemainingPlayers.First;
            RemainingPlayers.RemoveFirst();
            RemainingPlayers.AddLast(currentActivePlayer);
            RemainingPlayersActivePlayer = RemainingPlayers.First.Value;
        }

        List<Player> EliminatedPlayers = new();

        protected void EliminateActivePlayer()
        {
            // TODO Add to queue and resolve when round ends
            EliminatedPlayers.Add(ActivePlayer);
        }

        protected void ResolveEliminations()
        {
            EliminatedPlayers.Reverse();
            for (int i = 0; i < EliminatedPlayers.Count; i++)
            {
                RemainingPlayers.Remove(EliminatedPlayers[i]);
            }

            EliminatedPlayers.Clear();

            if (RemainingPlayers.Count == 0)
            {
                EndGame();
            }
        }

        protected virtual void ReadyNextPlayer()
        {
            LoopActivePlayer();
            ActivePlayer = RemainingPlayersActivePlayer;

            LinkedListNode<Player> currentPlayer = Players.First;
            while (currentPlayer != null)
            {
                currentPlayer = currentPlayer.Next;
                Debug.Log($"PlayerUUID: {currentPlayer.Value.PlayerUUID}");
                currentPlayer.Value.gameObject.SetActive(currentPlayer.Value.PlayerUUID == ActivePlayer.PlayerUUID);
                currentPlayer = currentPlayer.Next;
            }

            Player.ActivePlayer = ActivePlayer;
        }

        protected virtual void SetupTurn()
        {
            ReadyNextPlayer();

            // Wait for player ready before activating turn monitor (only really relevant for time based monitor)
            for (int i = 0; i < TurnMonitors.Count; i++)
            {
                TurnMonitors[i].NewTurn(ActivePlayer.PlayerName);
                TurnMonitors[i].PauseTurn();
            }

            ActivePlayer.transform.SetPositionAndRotation(PlayerOrigin.transform.position, PlayerOrigin.transform.rotation);
            ActivePlayer.GetComponent<InputController>().Paused = true;
            ActivePlayer.Ship.Teleport(PlayerOrigin.transform);
            ActivePlayer.Ship.GetComponent<ShipTransformer>().Reset();
            ActivePlayer.Ship.TrailSpawner.PauseTrailSpawner();
            ActivePlayer.Ship.ResourceSystem.Reset();
            ActivePlayer.Ship.Guide = PlayerCaptain;

            CameraManager.Instance.SetupGamePlayCameras(ActivePlayer.Ship.FollowTarget);

            // For single player games, don't require the extra button press
            if (Players.Count > 1)
            {
                ReadyButton.gameObject.SetActive(true);
            }
            else
            {
                OnReadyClicked();
            }
        }
    }
}