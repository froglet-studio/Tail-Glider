using CosmicShore.Game.Arcade;
using CosmicShore.Utility.ClassExtensions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CosmicShore.App.UI.Elements
{
    public class MultiplayerView : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button joinGameButton;
        [SerializeField] private Button hostGameButton;
        [SerializeField] private Button spectateGameButton;
        
        [Header("Captain Settings")]
        [SerializeField] private SO_Captain host;
        [SerializeField] private SO_Captain client;
        
        private void Awake()
        {
            hostGameButton.onClick.AddListener(HostGame);
            joinGameButton.onClick.AddListener(JoinGame);
            spectateGameButton.onClick.AddListener(SpectateGame);
        }

        private void HostGame()
        {
            this.LogWithClassMethod("", "Hosting a game.");
            NetworkManager.Singleton.StartHost();
        }

        private void JoinGame()
        {
            this.LogWithClassMethod("", "Joining a game as client.");
            NetworkManager.Singleton.StartClient();
        }

        private void SpectateGame()
        {
            this.LogWithClassMethod("", "Join a game as spectator.");
            NetworkManager.Singleton.StartClient();
        }

        private void LoadMiniGame(ShipTypes type, SO_Captain captain, int intensity, int playerCount)
        {
            MiniGame.PlayerShipType = type;
            MiniGame.PlayerCaptain = captain;
            MiniGame.IntensityLevel = intensity;
            MiniGame.NumberOfPlayers = playerCount;

            SceneManager.LoadScene("MinigameCellularDuel");
        }
    }
}
