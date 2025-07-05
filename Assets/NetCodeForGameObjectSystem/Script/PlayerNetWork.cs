using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
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


    private void Update()
    {//เฉพาะOwnerของobjนั้นๆที่มีสคริปต์นี้ติดถึงรันได้
        if (IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Transform spawnObjectTransform = Instantiate(spawnObjectPrefab);
                spawnObjectTransform.GetComponent<NetworkObject>().Spawn(true); // SpawnในNetWork และnetworkobjectสามารถspawnในnetworkServerเท่านั้น
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
