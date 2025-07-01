using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetWork : NetworkBehaviour
{
    //�ء���ҧ�����network����initilize�awake�ѺStart,Update ����� OnNetWorkSpawn();᷹
    //netWork ����server�������ö�ѹ���������client��ͧ����ѹ ������¹���networkVariable���default�ѹ������ҡserver(����ö����¹��)
   
    
    private NetworkVariable<int> randomNumber = new NetworkVariable<int>(1,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner); //variable�����Ҩ����觢����ż�ҹNetWorkcẺ�������client��������ç�������繤��null ��ͧinitialized���͡�˹�����ѹ

    public override void OnNetworkSpawn() //੾�е͹���NetWorkSpawn
    {
        randomNumber.OnValueChanged += (int previousValue, int newValue) =>
        {

        };
    }


    private void Update()
    {
        if (!IsOwner) //੾��Owner�ͧobj��������ʤ�Ի����Դ�֧�ѹ��
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
