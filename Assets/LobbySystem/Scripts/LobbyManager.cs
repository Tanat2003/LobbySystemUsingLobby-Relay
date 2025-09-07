using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{


    public static LobbyManager Instance { get; private set; }


    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_ALREADY_ENTER_PASSWORD_RIGHT = "False";
    public const string KEY_PLAYER_CHARACTER = "Character";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_START_GAME = "StartGame";
    public const string KEY_PASSWORD_GAME = "Password";
    public const string KEY_CANJOIN_GAME = "CanJoin";



    public event EventHandler OnLeftLobby;
    public event EventHandler OnGameStarted;
    public event EventHandler OnPrivateLobbyCreate;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;
    public event EventHandler<LobbyEventArgs> OnLobbyPrivateCreated;
    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }


    public enum GameMode
    {
        CaptureTheFlag,
        Conquest
    }

    public enum PlayerCharacter
    {
        Marine,
        Ninja,
        Zombie
    }



    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    private Lobby joinedLobby;
    private string playerName;
    private string playerEnterPasswordStatus;

    private string password;

    private Dictionary<ulong, string> clientIdToPlayerId = new();

    #region ExcuteMethod
    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
    }

    private async void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        if (!IsServer)
            return;
        if (clientIdToPlayerId.TryGetValue(clientId, out string playerId))
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            clientIdToPlayerId.Remove(clientId);
            Debug.Log($"❌ Disconnected: {clientId} removed PlayerId: {playerId} from Lobby" + joinedLobby.Name);
        }


    }

    private void Awake()
    {
        Instance = this;
    }


    private void Update()
    {
        HandleRefreshLobbyList();
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }
    #endregion

    #region AuthenticateMethod
    public void RegisterPlayerId(ulong clientId, string playerId)
    {
        if (!clientIdToPlayerId.ContainsKey(clientId))
        {
            clientIdToPlayerId[clientId] = playerId;
            Debug.Log($"✅ Registered clientId: {clientId} → playerId: {playerId}");
        }
        else
        {
            Debug.LogWarning($"⚠️ clientId {clientId} already registered.");
        }
    }
    public async void Authenticate(string playerName)
    {
        this.playerName = playerName;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () =>
        {
            // do nothing
            Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);

            RefreshLobbyList();
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    #endregion









    #region HandleCreatedLobbyMethod
    private void HandleRefreshLobbyList()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
        {
            refreshLobbyListTimer -= Time.deltaTime;
            if (refreshLobbyListTimer < 0f)
            {
                float refreshLobbyListTimerMax = 5f;
                refreshLobbyListTimer = refreshLobbyListTimerMax;

                RefreshLobbyList();
            }
        }
    }

    private async void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                Debug.Log("Heartbeat");
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    //handle lobby that are created
    private async void HandleLobbyPolling()
    {
        if (joinedLobby != null)
        {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f)
            {
                float lobbyPollTimerMax = 1.1f;
                lobbyPollTimer = lobbyPollTimerMax;

                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                //OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                if (!IsPlayerInLobby())
                {
                    // Player was kicked out of this lobby
                    Debug.Log("Kicked from Lobby!");

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    joinedLobby = null;
                }

                //Lobby not start game yet
                if (!IsLobbyStartGame())
                {
                    if (joinedLobby.Data[KEY_PASSWORD_GAME].Value == "Password")
                    {

                        OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                    }
                    else
                    {
                        if (IsLobbyHost())
                        {
                            OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                        }
                    }



                }

                if (IsLobbyStartGame() && !IsLobbyGameHasPassword())
                {
                    if (!IsLobbyHost())
                    {
                        TestRelay.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);

                    }
                    OnGameStarted?.Invoke(this, EventArgs.Empty);
                    //If set joinedLobby to null player cannot be join while host playing in game
                    //joinedLobby = null;



                }

                //people can join while playing but need to enter password
                if (IsLobbyStartGame() && IsLobbyGameHasPassword())
                {
                    if (!IsLobbyHost())
                    {
                        if (playerEnterPasswordStatus == "True")
                        {
                            TestRelay.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                            OnGameStarted?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            ShowEnterPasswordForClient();
                        }

                    }
                    else
                    {
                        OnGameStarted?.Invoke(this, EventArgs.Empty);
                    }



                }






            }
        }
    }
    #endregion

    #region UpdateLobbyDataMethod
    public void ChangeGameMode()
    {
        if (IsLobbyHost())
        {
            GameMode gameMode =
                Enum.Parse<GameMode>(joinedLobby.Data[KEY_GAME_MODE].Value);

            switch (gameMode)
            {
                default:
                case GameMode.CaptureTheFlag:
                    gameMode = GameMode.Conquest;
                    break;
                case GameMode.Conquest:
                    gameMode = GameMode.CaptureTheFlag;
                    break;
            }

            UpdateLobbyGameMode(gameMode);
        }
    }
    public async void UpdatePasswordLobby(string passwordToChange)
    {
        try
        {
            if (joinedLobby != null)
            {
                var updateData = new Dictionary<string, DataObject>
        {
            { KEY_PASSWORD_GAME, new DataObject(DataObject.VisibilityOptions.Public, passwordToChange) }
        };

                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id,
                    new UpdateLobbyOptions { Data = updateData });

                joinedLobby = lobby;
                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                password = passwordToChange;
                Debug.Log($"✅ Lobby password updated to: " + joinedLobby.Data[KEY_PASSWORD_GAME].Value);
            }

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
       
    }
    public async void UpdateJoinDuringGameLobby(bool enable)
    {
        try
        {
            if (joinedLobby != null)
            {
                if (enable)
                {
                    var updateData = new Dictionary<string, DataObject>
                {
                    {KEY_CANJOIN_GAME,new DataObject(DataObject.VisibilityOptions.Public,value:"CanJoin",index: DataObject.IndexOptions.S1) }
                };
                    Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions { Data = updateData });
                    joinedLobby = lobby;
                    Debug.Log("We Update this Lobby Joinable To:" + joinedLobby.Data[KEY_CANJOIN_GAME].Value);
                    OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                }
                else
                {
                    var updateData = new Dictionary<string, DataObject>
                {
                    {KEY_CANJOIN_GAME,new DataObject(DataObject.VisibilityOptions.Public,value:"CannotJoin",index:DataObject.IndexOptions.S1) }
                };
                    Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions { Data = updateData });
                    joinedLobby = lobby;
                    Debug.Log("We Update this Lobby Joinable To:" + joinedLobby.Data[KEY_CANJOIN_GAME].Value);
                    OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                }



            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        

    }
    public async void UpdateLobbyGameMode(GameMode gameMode)
    {
        try
        {
            Debug.Log("UpdateLobbyGameMode " + gameMode);

            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
                }
            });

            joinedLobby = lobby;

            OnLobbyGameModeChanged?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    #endregion

    #region UpdatePlayerDataMethod
    public async void UpdatePlayerName(string playerName)
    {
        this.playerName = playerName;

        if (joinedLobby != null)
        {
            try
            {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_NAME, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerName)
                    }
                };

                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    public async void UpdatePlayerCharacter(PlayerCharacter playerCharacter)
    {
        if (joinedLobby != null)
        {
            try
            {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_CHARACTER, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerCharacter.ToString())
                    }
                };

                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    public void UpdatePlayerEnterPassword(string passwordEnterStatus)
    {
        this.playerEnterPasswordStatus = passwordEnterStatus;
        if (joinedLobby != null)
        {
            try
            {
                UpdatePlayerOptions options = new UpdatePlayerOptions();
                options.Data = new Dictionary<string, PlayerDataObject>()
                { {
                        KEY_PLAYER_ALREADY_ENTER_PASSWORD_RIGHT, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: passwordEnterStatus)
                    }

                };


            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    #endregion

    #region Join&StartLobbyMethod
    public async void JoinLobbyByCode(string lobbyCode)
    {
        Player player = GetPlayer();

        Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
        {
            Player = player
        });

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }
    public async void JoinLobby(Lobby lobby)
    {
        Player player = GetPlayer();

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
        {
            Player = player
        });

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }
    public async void StartGame()
    {
        if (IsLobbyHost())
        {
            try
            {
                string relayCode = await TestRelay.Instance.CreateRelay();
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {KEY_START_GAME,new DataObject(DataObject.VisibilityOptions.Member,relayCode) },
                    }
                });

                joinedLobby = lobby;


            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

    }

    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    #endregion
    #region PlayerLeftLobbyMethod
    private bool IsPlayerInLobby()
    {
        if (joinedLobby != null && joinedLobby.Players != null)
        {
            foreach (Player player in joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    // This player is in this lobby
                    return true;
                }
            }
        }
        return false;
    }
    public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                joinedLobby = null;

                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    #endregion
    #region LobbyStart&LobbyHasPasswordMethod
    private bool IsLobbyStartGame() => joinedLobby.Data[KEY_START_GAME].Value != "0";
    private bool IsLobbyGameHasPassword() => joinedLobby.Data[KEY_PASSWORD_GAME].Value != "Password";
    #endregion
    public bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
    private Player GetPlayer()
    {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
            { KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerCharacter.Marine.ToString()) },
            {KEY_PLAYER_ALREADY_ENTER_PASSWORD_RIGHT, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public,playerEnterPasswordStatus)}
        });
    }
    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode)
    {
        Player player = GetPlayer();

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            Player = player,
            //IsPrivate = isPrivate, //enable it if you want to disable other to join privateroom
            Data = new Dictionary<string, DataObject> {
                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) },
                {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member,"0") },
                {KEY_PASSWORD_GAME, new DataObject(DataObject.VisibilityOptions.Public,"Password") },
                //Use IndexOptions for Lobby Filter
                {KEY_CANJOIN_GAME, new DataObject(DataObject.VisibilityOptions.Public,value:"CanJoin",index: DataObject.IndexOptions.S1) }
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        if (isPrivate)
        {
            UpdatePasswordLobby(joinedLobby.LobbyCode);
            OnPrivateLobbyCreate?.Invoke(this, EventArgs.Empty);
        }
        Debug.Log("Created Lobby " + lobby.Name);
    }
    public async void CheckIfJoinLobbyHasPassword(Lobby lobby)
    {
        Player player = GetPlayer();

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
        {
            Player = player
        });

        if (joinedLobby.Data[KEY_PASSWORD_GAME].Value != "Password")
        {
            Debug.Log("LobbyHasPassword we are going to show enterpasswordUI");

            ShowEnterPasswordForClient();

        }
        else
        {
            JoinLobby(joinedLobby);
        }
    }
    public async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            //options.Filters = new List<QueryFilter> {
            //    new QueryFilter(
            //        field: QueryFilter.FieldOptions.AvailableSlots,
            //        op: QueryFilter.OpOptions.GT,
            //        value: "0")
            //};

            //Filter a lobby that can join while playing(If host not start all player will be able to join)
            options.Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.S1,
                    op: QueryFilter.OpOptions.EQ,
                    value: "CanJoin"
                    )
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder> {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync(options);

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private async void ShowEnterPasswordForClient()
    {
        Lobby latestLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
        string realPassword = latestLobby.Data[KEY_PASSWORD_GAME].Value;

        LobbyUI.Instance.EnterPasswordForLobby(realPassword, latestLobby);
    }
    public Lobby GetJoinedLobby()
    {
        return joinedLobby;
    }
}