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
        await UnityServices.InitializeAsync(); //await & async มันจะยิงคำสั่งไปยังเซิฟเวอร์ของunityเวลาเราใช้GameServiceของunity
                                               //และต้องการรอให้ระบบมันเทรินค่ามาต้องใช้อันนี้ตลอด(จะได้ไม่error)   

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

                //ส่งสัญญาณไม่ให้ล็อบบี้นี้โดนลบ(GameServiceจะลบล็อบบี้ที่ไม่มีการเคลื่อนไหวควรส่งHeartbeatทุก15วิ)
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
                       //เราสามารถส่งข้อมูลใหม่ไปได้โดย ชื่อข้อมูล,ประเภทข้อมูล(อันนี้สร้างขึ้นมาได้จากในคลาสเลย)
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
        //ใช้try catchกันเหนียวไม่ให้ค่าnullตอนระหว่างรอawait
        try
        {
            //คิวของล็อบบี้ สามารถกรองข้อมูลโดยผ่านคลาสได้เลย
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25, //จำนวนล็อบบี้ที่จะรีเทริน
                Filters = new List<QueryFilter> //ฟิวเตอร์ อยากรีเทิรนล็อบบี้ยังไง อันนี้ฟิวเตอร์จากจำนวนที่ว่างในห้อง มากกว่า0
                { new QueryFilter
                (QueryFilter.FieldOptions.AvailableSlots,"0",QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder> //ออเดอร์ลำดับตามเวลาที่ฟิวด์สร้าง
                {
                    new QueryOrder(false,QueryOrder.FieldOptions.Created)
                },

            };

            //QueryResponse คือลิสต์ของล็อบบี้ที่แมตซ์กับคิวที่เราใส่ไป
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
