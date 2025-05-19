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

        // �ݵ�� ������Ʈ owner�� Destroy �ؾ� ��
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}