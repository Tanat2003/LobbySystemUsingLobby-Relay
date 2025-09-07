using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{


    public static LobbyUI Instance { get; private set; }


    [SerializeField] private Transform playerSingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private Button changeMarineButton;
    [SerializeField] private Button changeNinjaButton;
    [SerializeField] private Button changeZombieButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button changeGameModeButton;
    [SerializeField] private Button startGameModeButton;
    [SerializeField] private Button passwordEnterButton;
    [SerializeField] private Toggle canJoinDuringGameButton;

    public bool playerEnterRightPassword;
    public bool ableToJoinLobby;

    private void Awake()
    {
        Instance = this;

        playerSingleTemplate.gameObject.SetActive(false);

        changeMarineButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Marine);
        });
        changeNinjaButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Ninja);
        });
        changeZombieButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Zombie);
        });

        leaveLobbyButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.LeaveLobby();
        });

        changeGameModeButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.ChangeGameMode();
        });
        startGameModeButton.onClick.AddListener(() =>
        {

            startGameModeButton.gameObject.SetActive(false);
            LobbyManager.Instance.StartGame();

        });

    }

    private void Start()
    {
        LobbyManager.Instance.OnJoinedLobby += UpdateLobby_Event;
        LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
        LobbyManager.Instance.OnLobbyGameModeChanged += UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnPrivateLobbyCreate += LobbyCreatesUI_OnPrivateCreate;

        Hide();
        passwordEnterButton.gameObject.SetActive(false);
        ableToJoinLobby = true;
    }



    private void LobbyCreatesUI_OnPrivateCreate(object sender, System.EventArgs e)
    {
        passwordEnterButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
    }

    private void LobbyManager_OnLeftLobby(object sender, System.EventArgs e)
    {
        ClearLobby();
        Hide();
    }

    private void UpdateLobby_Event(object sender, LobbyManager.LobbyEventArgs e)
    {
        UpdateLobby(LobbyManager.Instance.GetJoinedLobby());

    }



    private void UpdateLobby(Lobby lobby)
    {
        ClearLobby();
        LobbyListUI.Instance.gameObject.SetActive(false);
        foreach (Player player in lobby.Players)
        {

            Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
            playerSingleTransform.gameObject.SetActive(true);
            LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();

            lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                LobbyManager.Instance.IsLobbyHost() &&
                player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
            );

            lobbyPlayerSingleUI.UpdatePlayer(player);
        }

        startGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
        changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
        canJoinDuringGameButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());

        lobbyNameText.text = lobby.Name;
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        gameModeText.text = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;
        

        Show();
    }

    private void ClearLobby()
    {
        foreach (Transform child in container)
        {
            if (child == playerSingleTemplate) continue;
            Destroy(child.gameObject);
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
    public void EnterPasswordForLobby(string requiredPassword, Lobby lobby)
    {
        if (playerEnterRightPassword)
            return;
        Debug.Log("We show EnterPasswordUI!");
        UI_InputWindow.Show_Static("Enter Password", "", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 12,
            () =>
            {
                LobbyManager.Instance.LeaveLobby();
            },
                 (string inputPassword) =>
                  {


                      if (inputPassword == requiredPassword)
                      {
                          Debug.Log("? Password correct!");
                          playerEnterRightPassword = true;


                          LobbyManager.Instance.UpdatePlayerEnterPassword("True");
                          LobbyManager.Instance.JoinLobby(lobby);
                          UpdateLobby(lobby);

                          //TestRelay.Instance.JoinRelay(LobbyManager.Instance.GetJoinedLobby().Data["StartGame"].Value);
                      }
                      else
                      {
                          Debug.Log("Password false");
                          LobbyManager.Instance.LeaveLobby();
                          LobbyManager.Instance.UpdatePlayerEnterPassword("False");
                      }


                  });
    }


    public void UpdateJoinLobbyEnable()
    {
        ableToJoinLobby = !ableToJoinLobby;

        LobbyManager.Instance.UpdateJoinDuringGameLobby(ableToJoinLobby);
    }
}