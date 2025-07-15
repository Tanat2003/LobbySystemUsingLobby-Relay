using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerNetWork : NetworkBehaviour
{
    //ทุกอย่างที่ใช้networkอย่าinitilizeในawakeกับStart,Update ให้ใช้ OnNetWorkSpawn();แทน
    //netWork มีแค่serverที่สามารถรันคำสั่งได้ถ้าclientต้องการรัน แต่เปลี่ยนค่าnetworkVariableค่าdefaultมันจะเซ็ตได้จากserver(สามารถเปลี่ยนได้)


    private NetworkVariable<MyCustomData> randomNumber = new NetworkVariable<MyCustomData>
        (new MyCustomData
        {
            _int = 50,
            _bool = true,

        },
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); //variableที่เราจะใช้ส่งข้อมูลผ่านNetWorkcแบบส่งให้ทั้งclientและอื่นๆไรงี้ห้ามเป็นค่าnull ต้องinitializedหรือกำหนดค่ามัน

    [SerializeField] private Transform spawnObjectPrefab;
    
    //structส่งผ่านNetWorkไม่ได้ต้องimplement INetworkSerializableเพื่อreferenceค่าด้วย
    public struct MyCustomData: INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public FixedString128Bytes message; //เวลาส่งข้อมูลstringผ่านnetwork มันต้องกำหนดขนาดข้อมูลด่วยเราเลยใช้fixedstring

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref message);
        }
    }
    public override void OnNetworkSpawn() //เฉพาะตอนที่NetWorkSpawn
    {
        


        randomNumber.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) =>
        {
            Debug.Log("Owner: " + OwnerClientId + "has Create " + newValue._int);
        };
    }
    private void Start()
    {
        //StorePlayerId When PlayerPrefabsSpawn
        if (IsOwner && IsClient)
        {
           StartCoroutine(DelayRegister());
        }
    }


    private void Update()
    {
        if (IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Transform spawnObjectTransform = Instantiate(spawnObjectPrefab);
                spawnObjectTransform.GetComponent<NetworkObject>().Spawn(true); // SpawnในNetWork และnetworkobjectสามารถspawnในnetworkServerเท่านั้น
            }

            



        }
            
       
    }

    private IEnumerator DelayRegister()
    {
        Debug.Log("?? WaitUntil LobbyManager.Instance != null...");
        yield return new WaitUntil(() => LobbyManager.Instance != null);

        Debug.Log("? LobbyManager.Instance is ready, calling RPC...");
        SendPlayerIdToHostServerRpc(AuthenticationService.Instance.PlayerId);
    }


    [ServerRpc(RequireOwnership = false)]
    private void SendPlayerIdToHostServerRpc(string playerId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        LobbyManager.Instance.RegisterPlayerId(clientId, playerId);
    }


}
