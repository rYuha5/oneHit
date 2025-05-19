using UnityEngine;
using Photon.Pun;

public class HitboxTrigger : MonoBehaviour
{
    public PhotonView ownerPhotonView;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!ownerPhotonView.IsMine) return;

        if (other.CompareTag("Shield"))
        {
            Debug.Log("���п� ƨ��!");

            // Į ƨ��� RPC ��û
            ownerPhotonView.RPC("BounceSwordRPC", RpcTarget.All);
            return;
        }

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