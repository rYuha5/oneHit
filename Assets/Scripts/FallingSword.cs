using UnityEngine;
using Photon.Pun;

public class FallingSword : MonoBehaviourPunCallbacks
{
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        PlayerController player = collision.collider.GetComponent<PlayerController>();
        if (player == null || !player.photonView.IsMine) return;

        player.photonView.RPC("SetHasSword", RpcTarget.AllBuffered, true);
        player.swordController.hitbox = null;

        // 반드시 오브젝트 owner가 Destroy 해야 함
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}