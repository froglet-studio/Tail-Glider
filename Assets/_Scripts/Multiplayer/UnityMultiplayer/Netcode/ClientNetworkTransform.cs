using Unity.Netcode.Components;
using UnityEngine;

namespace CosmicShore.Multiplayer.UnityMultiplayer.Netcode
{
    public enum AuthMode
    {
        Server,
        Client
    }
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        public AuthMode authMode = AuthMode.Client;
        protected override bool OnIsServerAuthoritative()
        {
            return authMode == AuthMode.Server;
        }
    }
}
