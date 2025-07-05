using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestRelay : MonoBehaviour
{
    public static TestRelay Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    //private async void Start()
    //{
    //    await UnityServices.InitializeAsync();

    //    //Sing IN as anonymouse
    //    await AuthenticationService.Instance.SignInAnonymouslyAsync();

    //    AuthenticationService.Instance.SignedIn += () =>
    //    {
    //        Debug.Log("SingIN: " + AuthenticationService.Instance.PlayerId);
    //    };
    //}

    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);

            //joinCode�ͧrealy�����Ҩ����������ͧ�������������relay���ǡѹ
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = new RelayServerData(allocation,"dtls");
            //Set��ҵ�ҧ��UnityTransport�ͧhost���ç�Ѻrelay������ҧ��(�����ҧ��host)
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            //NetworkManager.Singleton.GetComponent<UnityTransport>().
            //    SetRelayServerData(allocation.RelayServer.IpV4,
            //    (ushort)allocation.RelayServer.Port,
            //    allocation.AllocationIdBytes,
            //    allocation.Key,
            //    allocation.ConnectionData

            //    );
            NetworkManager.Singleton.StartHost();
            
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }

    }
    public async void JoinRelay(string joinCode)
    {
        try
        {
           JoinAllocation joinAllocation =  await RelayService.Instance.JoinAllocationAsync(joinCode);
            //�緤��UnityTransport�ͧ�����JoinRelay
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            //Set��ҵ�ҧ��UnityTransport�ͧhost���ç�Ѻrelay������ҧ��(�����ҧ��host)
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);    

            //NetworkManager.Singleton.GetComponent<UnityTransport>().
            //    SetClientRelayData(
            //    joinAllocation.RelayServer.IpV4,
            //    (ushort)joinAllocation.RelayServer.Port,
            //    joinAllocation.AllocationIdBytes,
            //    joinAllocation.Key,
            //    joinAllocation.ConnectionData,
            //    joinAllocation.HostConnectionData

            //    );
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

}
