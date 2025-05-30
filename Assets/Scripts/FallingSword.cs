using UnityEngine;
using Photon.Pun;

public class FallingSword : MonoBehaviourPunCallbacks
{
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        var player = collision.collider.GetComponent<PlayerController>();
        if (player == null) return;

        // 강제 회수 조건 완화
        if (player.photonView.IsMine && player.hasSword == false)
        {
            player.photonView.RPC("SetHasSword", RpcTarget.AllBuffered, true);
            player.swordController.hitbox = null;

            if (photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
        }
    }
}