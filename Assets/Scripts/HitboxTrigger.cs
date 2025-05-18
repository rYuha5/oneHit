using UnityEngine;
using Photon.Pun;

public class HitboxTrigger : MonoBehaviour
{
    public PhotonView ownerPhotonView; // 누가 이 히트박스를 만들었는지

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!ownerPhotonView.IsMine) return; // 내가 소유한 칼이 아니면 처리 안 함

        if (other.CompareTag("Player"))
        {
            PhotonView targetPV = other.GetComponent<PhotonView>();
            if (targetPV != null && !targetPV.IsMine)
            {
                Debug.Log("적중! 대상: " + other.name);
                targetPV.RPC("TakeDamage", RpcTarget.All);
            }
        }
    }
}