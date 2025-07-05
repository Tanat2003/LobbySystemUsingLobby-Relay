using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetWork : NetworkBehaviour
{
    //�ء���ҧ�����network����initilize�awake�ѺStart,Update ����� OnNetWorkSpawn();᷹
    //netWork ����server�������ö�ѹ���������client��ͧ����ѹ ������¹���networkVariable���default�ѹ������ҡserver(����ö����¹��)


    private NetworkVariable<MyCustomData> randomNumber = new NetworkVariable<MyCustomData>
        (new MyCustomData
        {
            _int = 50,
            _bool = true,

        },
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); //variable�����Ҩ����觢����ż�ҹNetWorkcẺ�������client��������ç�������繤��null ��ͧinitialized���͡�˹�����ѹ

    [SerializeField] private Transform spawnObjectPrefab;
    
    //struct�觼�ҹNetWork������ͧimplement INetworkSerializable����reference��Ҵ���
    public struct MyCustomData: INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public FixedString128Bytes message; //�����觢�����string��ҹnetwork �ѹ��ͧ��˹���Ҵ�����Ŵ�����������fixedstring

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref message);
        }
    }
    public override void OnNetworkSpawn() //੾�е͹���NetWorkSpawn
    {
        randomNumber.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) =>
        {
            Debug.Log("Owner: " + OwnerClientId + "has Create " + newValue._int);
        };
    }


    private void Update()
    {//੾��Owner�ͧobj��������ʤ�Ի����Դ�֧�ѹ��
        if (IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Transform spawnObjectTransform = Instantiate(spawnObjectPrefab);
                spawnObjectTransform.GetComponent<NetworkObject>().Spawn(true); // Spawn�NetWork ���networkobject����öspawn�networkServer��ҹ��
            }

            Vector3 moveDir = new Vector3(0, 0, 0);
            while (Input.GetKeyDown(KeyCode.W))
            {
                moveDir.z += 1f;
            }
            while(Input.GetKeyDown(KeyCode.S))
            {
                moveDir.z -= 1f;
            }
            while (Input.GetKeyDown(KeyCode.A))
            {
                moveDir.x += -1f;
            }
            while (Input.GetKeyDown(KeyCode.D))
            {
                moveDir.x += 1f;
            }
            float moveSPD = 10f;
            transform.position += moveDir * moveSPD * Time.deltaTime;



        }
            
       
    }

    
}
