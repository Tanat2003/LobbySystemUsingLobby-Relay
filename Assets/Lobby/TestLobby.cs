using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class TestLobby : MonoBehaviour
{
    private Lobby hostLobby;
    private float heartBeatTimer;
    private string playerName = "NewPlayer" + UnityEngine.Random.Range(10,99);
    private async void Start()
    {
        await UnityServices.InitializeAsync(); //await & async �ѹ���ԧ�������ѧ�Կ�����ͧunity���������GameService�ͧunity
                                               //��е�ͧ���������к��ѹ��Թ����ҵ�ͧ���ѹ����ʹ(�������error)   

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Sign IN" + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); //Sign In To use the service

    }
    private void Update()
    {
        HandleLobbyHeartBeat();
    }
    private async void HandleLobbyHeartBeat()
    {
        if (hostLobby != null)
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer < 0f)
            {
                float heartBeatTimeMax = 15;
                heartBeatTimer = heartBeatTimeMax;

                //���ѭ�ҳ��������ͺ�����ⴹź(GameService��ź��ͺ���������ա������͹��Ǥ����Heartbeat�ء15��)
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }

    }
    [NaughtyAttributes.Button]
    private async void CreateLobby()
    {
        try
        {
            string lobbyName = "My Lobby";
            int maxPlayers = 4;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Player
                {            
                   Data  = new Dictionary<string, PlayerDataObject>
                   {
                       //�������ö�觢�������������� ���͢�����,������������(�ѹ������ҧ�������ҡ㹤������)
                       {"PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName) }
                   }
                },

            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers,createLobbyOptions);
            hostLobby = lobby;

            Debug.Log("Create Lobby " + lobby.Name + " MaxPlayer: " + lobby.MaxPlayers + " CODE: "+lobby.LobbyCode);
            PrintPlayer(hostLobby);

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }


    }
    [NaughtyAttributes.Button]
    private async void ListLobby()
    {
        //��try catch�ѹ�˹�����������null�͹�����ҧ��await
        try
        {
            //��Ǣͧ��ͺ��� ����ö��ͧ�������¼�ҹ���������
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25, //�ӹǹ��ͺ����������Թ
                Filters = new List<QueryFilter> //������� ��ҡ����ù��ͺ����ѧ� �ѹ���������ҡ�ӹǹ�����ҧ���ͧ �ҡ����0
                { new QueryFilter
                (QueryFilter.FieldOptions.AvailableSlots,"0",QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder> //�������ӴѺ������ҷ���Ǵ����ҧ
                {
                    new QueryOrder(false,QueryOrder.FieldOptions.Created)
                },

            };

            //QueryResponse �����ʵ�ͧ��ͺ����������Ѻ��Ƿ���������
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            Debug.Log("Lobby Found! " + queryResponse.Results.Count);

            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);

            }

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }



    private async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log("JoinLobbyWithCode" + lobbyCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }


    }

    private async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
     
    }
    private void PrintPlayer(Lobby lobby)
    {
        Debug.Log("Player In Lobby"+lobby.Name);
        foreach(Player player in lobby.Players)
        {
            Debug.Log("PlayerID: "+player.Id+" " + player.Data["PlayerName"].Value);
        }
    }
}
