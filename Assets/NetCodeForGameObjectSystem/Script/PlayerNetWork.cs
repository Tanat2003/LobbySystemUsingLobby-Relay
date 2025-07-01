using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetWork : NetworkBehaviour
{
    //ทุกอย่างที่ใช้networkอย่าinitilizeในawakeกับStart,Update ให้ใช้ OnNetWorkSpawn();แทน
    //netWork มีแค่serverที่สามารถรันคำสั่งได้ถ้าclientต้องการรัน แต่เปลี่ยนค่าnetworkVariableค่าdefaultมันจะเซ็ตได้จากserver(สามารถเปลี่ยนได้)
   
    
    private NetworkVariable<int> randomNumber = new NetworkVariable<int>(1,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner); //variableที่เราจะใช้ส่งข้อมูลผ่านNetWorkcแบบส่งให้ทั้งclientและอื่นๆไรงี้ห้ามเป็นค่าnull ต้องinitializedหรือกำหนดค่ามัน

    public override void OnNetworkSpawn() //เฉพาะตอนที่NetWorkSpawn
    {
        randomNumber.OnValueChanged += (int previousValue, int newValue) =>
        {

        };
    }


    private void Update()
    {
        if (!IsOwner) //เฉพาะOwnerของobjนั้นๆที่มีสคริปต์นี้ติดถึงรันได้
            return;

        Vector3 moveDir = new Vector3(0, 0, 0);
        if(Input.GetKeyDown(KeyCode.W))
        {
            moveDir.z += 1f;
        } if(Input.GetKeyDown(KeyCode.S))
        {
            moveDir.z -= 1f;
        } if(Input.GetKeyDown(KeyCode.A))
        {
            moveDir.x += -1f;
        } if(Input.GetKeyDown(KeyCode.D))
        {
            moveDir.x += 1f;
        }
        float moveSPD = 5f;
        transform.position += moveDir * moveSPD * Time.deltaTime;
    }
}
