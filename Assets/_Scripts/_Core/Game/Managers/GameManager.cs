using UnityEngine;
using UnityEngine.SceneManagement;
using TailGlider.Utility.Singleton;
using UnityEngine.Advertisements;
using System.Collections;

namespace StarWriter.Core
{
    [DefaultExecutionOrder(0)]
    public class GameManager : SingletonPersistent<GameManager>
    {
        public Player player;

        private GameSetting gameSettings;

        public delegate void OnPlayGameEvent();
        public static event OnPlayGameEvent onPlayGame;

        public delegate void OnDeathEvent();
        public static event OnDeathEvent onDeath;

        public delegate void OnExtendGameEvent();
        public static event OnExtendGameEvent onExtendGamePlay;

        public delegate void OnGameOverEvent();
        public static event OnGameOverEvent onGameOver;

        public delegate void OnPhoneFlipEvent(bool state);
        public static event OnPhoneFlipEvent onPhoneFlip;

        CameraManager cameraManager;

        private readonly float phoneFlipThreshold = .3f;
        private int deathCount = 0;
        public int DeathCount { get { return deathCount; } }

        private void OnEnable()
        {
            AdsManager.adShowComplete += OnAdShowComplete;
            AdvertisementMenu.onDeclineAd += EndGame;   // TODO: let's move this to live in AdsManager
            ShipExplosionHandler.onExplosionCompletion += OnExplosionCompletion;
        }

        private void OnDisable()
        {
            AdsManager.adShowComplete -= OnAdShowComplete;
            AdvertisementMenu.onDeclineAd -= EndGame;
            ShipExplosionHandler.onExplosionCompletion -= OnExplosionCompletion;
        }
        void Start()
        {
            cameraManager = CameraManager.Instance;
            gameSettings = GameSetting.Instance;
        }
        private void Update()
        {
            if (Mathf.Abs(UnityEngine.Input.acceleration.y) < phoneFlipThreshold) return;

            if (UnityEngine.Input.acceleration.y < 0)
                onPhoneFlip(true);
            else
                onPhoneFlip(false);
        }

        public void OnClickTutorialButton()
        {
            UnPauseGame();
            SceneManager.LoadScene(1);
        }
        
        /// <summary>
        /// Toggles the Gyro On/Off
        /// </summary>
        public void OnClickGyroToggleButton()
        {
            // Set gameSettings Gyro status
            gameSettings.ChangeGyroEnabledStatus();
        }

        /// <summary>
        /// Starts Tutorial or Game based on hasSkippedTutorial status
        /// </summary>
        public void OnClickPlayButton()
        {
            UnPauseGame();
            SceneManager.LoadScene(2);
        }

        private void OnExplosionCompletion()
        {
            Debug.Log("GameManager.Death");
            
            PauseGame();

            deathCount++;
            onDeath?.Invoke();

            if (deathCount >= 2)
                EndGame();
        }

        public void ExtendGame()
        {
            Debug.Log("GameManager.ExtendGame");
            
            onExtendGamePlay?.Invoke();

            // We disabled the player's colliders during the tail collision. let's turn them back on

            StartCoroutine(ToggleCollisionCoroutine());

            // TODO: getting an error with the below line that timescale can only be set from the main thread
            UnPauseGame();

            // TODO: unpause game and make sure player is in safe area
            // TODO: Garrett game scene stuff
        }

        IEnumerator ToggleCollisionCoroutine()
        {
            yield return new WaitForSeconds(.5f);
            player.ToggleCollision(true);
        }

        public static void EndGame()
        {
            Debug.Log("GameManager.EndGame");
            onGameOver?.Invoke();
            //Instance.player.ToggleCollision(true);
        }

        public void RestartGame()
        {
            Debug.Log("GameManager.RestartGame");
            deathCount = 0;
            
            SceneManager.LoadScene(2);
            UnPauseGame();
        }

        public void ReturnToLobby()
        {
            SceneManager.LoadScene(0);
            UnPauseGame();
            cameraManager.OnMainMenu();
        }

        public static void UnPauseGame()
        {
            if (PauseSystem.GetIsPaused()) TogglePauseGame();
        }

        public static void PauseGame()
        {
            if (!PauseSystem.GetIsPaused()) TogglePauseGame();
        }

        /// <summary>
        /// Toggles the Pause System
        /// </summary>
        public static void TogglePauseGame()
        {
            PauseSystem.TogglePauseGame();
        }

        public void WaitOnPlayerLoading()
        {
            onPlayGame?.Invoke();
        }

        public void OnAdShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
        {
            if (showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
            {
                Debug.Log("Unity Ads Rewarded Ad Completed. Extending game.");

                ExtendGame();
            }
        }
    }
}