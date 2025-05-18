using UnityEngine;
using Photon.Pun;

public class HitboxTrigger : MonoBehaviour
{
    public PhotonView ownerPhotonView; // ���� �� ��Ʈ�ڽ��� ���������

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!ownerPhotonView.IsMine) return; // ���� ������ Į�� �ƴϸ� ó�� �� ��

        if (other.CompareTag("Player"))
        {
            PhotonView targetPV = other.GetComponent<PhotonView>();
            if (targetPV != null && !targetPV.IsMine)
            {
                Debug.Log("����! ���: " + other.name);
                targetPV.RPC("TakeDamage", RpcTarget.All);
            }
        }
    }
}