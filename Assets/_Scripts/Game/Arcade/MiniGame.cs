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
		protected List<Player> Players;
		protected CountdownTimer countdownTimer;

		List<Teams> PlayerTeams = new() { Teams.Green, Teams.Red, Teams.Gold };
		List<string> PlayerNames = new() { "PlayerOne", "PlayerTwo", "PlayerThree" };

		// Configuration set by player
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
		int activePlayerId;
		int RemainingPlayersActivePlayerIndex = -1;
		protected List<int> RemainingPlayers = new();
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
			Players = new List<Player>();
			for (var i = 0; i < NumberOfPlayers; i++)
			{
				Players.Add(Instantiate(playerPrefab));
				Players[i].defaultShip = playerShipTypeInitialized ? PlayerShipType : DefaultPlayerShipType;
				Players[i].Team = PlayerTeams[i];
				Players[i].PlayerName = PlayerNames[i];
				Players[i].PlayerUUID = PlayerNames[i];
				Players[i].name = "Player" + (i + 1);
				Players[i].gameObject.SetActive(true);
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

			RemainingPlayers = new();
			// TODO: change index to player object.
			for (var i = 0; i < Players.Count; i++)
			{
				RemainingPlayers.Add(i);
			}

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
			OnMiniGameStart?.Invoke(gameMode, PlayerShipType, NumberOfPlayers, IntensityLevel);
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

			ScoreTracker.StartTurn(Players[activePlayerId].PlayerName, Players[activePlayerId].Team);

			Debug.Log($"Player {activePlayerId + 1} Get Ready! {Time.time}");
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

			for (int i = 0; i < Players.Count; i++)
			{
				Debug.Log($"MiniGame.EndGame - Player Score: {ScoreTracker.GetScore(Players[i].PlayerName)} ");
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

		void LoopActivePlayerIndex()
		{
			RemainingPlayersActivePlayerIndex++;
			RemainingPlayersActivePlayerIndex %= RemainingPlayers.Count;
		}

		List<int> EliminatedPlayers = new();

		protected void EliminateActivePlayer()
		{
			// TODO Add to queue and resolve when round ends
			EliminatedPlayers.Add(activePlayerId);
		}

		protected void ResolveEliminations()
		{
			EliminatedPlayers.Reverse();
			// TODO: replace index with player object.
			for (int i = 0; i < EliminatedPlayers.Count; i++)
			{
				RemainingPlayers.Remove(EliminatedPlayers[i]);
			}

			EliminatedPlayers = new List<int>();

			if (RemainingPlayers.Count <= 0)
			{
				EndGame();
			}
		}

		protected virtual void ReadyNextPlayer()
		{
			LoopActivePlayerIndex();
			activePlayerId = RemainingPlayers[RemainingPlayersActivePlayerIndex];
			ActivePlayer = Players[activePlayerId];

			for (int i = 0; i < Players.Count; i++)
			{
				Debug.Log($"PlayerUUID: {Players[i].PlayerUUID}");
				Players[i].gameObject.SetActive(Players[i].PlayerUUID == ActivePlayer.PlayerUUID);
			}

			Player.ActivePlayer = ActivePlayer;
		}

		protected virtual void SetupTurn()
		{
			ReadyNextPlayer();

			// Wait for player ready before activating turn monitor (only really relevant for time based monitor)
			for (int i = 0; i < TurnMonitors.Count; i++)
			{
				TurnMonitors[i].NewTurn(Players[activePlayerId].PlayerName);
				TurnMonitors[i].PauseTurn();
			}

			ActivePlayer.transform.SetPositionAndRotation(PlayerOrigin.transform.position, PlayerOrigin.transform.rotation);
			ActivePlayer.GetComponent<InputController>().Paused = true;
			ActivePlayer.Ship.Teleport(PlayerOrigin.transform);
			ActivePlayer.Ship.GetComponent<ShipTransformer>().Reset();
			ActivePlayer.Ship.TrailSpawner.PauseTrailSpawner();
			ActivePlayer.Ship.ResourceSystem.Reset();
			ActivePlayer.Ship.SetGuide(PlayerCaptain);

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