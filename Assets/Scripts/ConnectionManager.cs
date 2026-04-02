using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
   public string ProfileName { get; private set; }
    
   public string SessionName { get; private set; }
   
   private readonly int _maxPlayers = 4;
   
   private ConnectionState _state = ConnectionState.Disconnected;
   
   private ISession _session;
   
   private NetworkManager _networkManager;
   
   private enum ConnectionState
   {
       Disconnected,
       Connecting,
       Connected,
   }

   
   
    private async void Awake()
    {
        try
        {
            _networkManager = GetComponent<NetworkManager>();
            _networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            _networkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.Log(e.StackTrace); // TODO handle exception
        }
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
    {
        if (_networkManager.LocalClient.IsSessionOwner)
        {
            Debug.Log($"Client-{_networkManager.LocalClientId} is the session owner!");
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (_networkManager.LocalClientId == clientId)
        {
            Debug.Log($"Client-{clientId} is connected and can spawn {nameof(NetworkObject)}s.");
        }
    }

   private void OnDestroy()
   {
       _session?.LeaveAsync();
   }

   public async Task CreateOrJoinSessionAsync(string profileName, string sessionName)
   {
       _state = ConnectionState.Connecting;
   
       try
       {
           ProfileName = profileName;
           SessionName = sessionName;
           
           AuthenticationService.Instance.SwitchProfile(profileName);
           await AuthenticationService.Instance.SignInAnonymouslyAsync();
   
            var options = new SessionOptions() {
                Name = sessionName,
                MaxPlayers = _maxPlayers
            }.WithDistributedAuthorityNetwork();
   
            _session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
   
           _state = ConnectionState.Connected;
       }
       catch (Exception e)
       {
           _state = ConnectionState.Disconnected;
           Debug.LogException(e);
       }
   }
}