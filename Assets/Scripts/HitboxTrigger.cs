using UnityEngine;
using Photon.Pun;

public class HitboxTrigger : MonoBehaviourPunCallbacks
{
    public PhotonView ownerPhotonView;                    // 공격자의 PhotonView
    public PlayerController ownerPlayerController;        // 공격자의 PlayerController 참조
    public GameObject fallingSwordPrefab;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ownerPhotonView == null || !ownerPhotonView.IsMine) return;

        if (other.CompareTag("Player"))
        {
            PhotonView targetPV = other.GetComponent<PhotonView>();
            if (targetPV != null && !targetPV.IsMine)
            {
                targetPV.RPC("TakeDamage", RpcTarget.All);
            }
        }

        if (other.CompareTag("Shield"))
        {
            Vector2 contactPoint = transform.position;

            if (ownerPlayerController != null && ownerPlayerController.sword != null)
            {
                ownerPlayerController.sword.SetActive(false);
            }

            Vector2 force = Vector2.up + (Random.value < 0.5f ? Vector2.left : Vector2.right);
            force = force.normalized * 10f;

            ownerPhotonView.RPC("DropSwordWithForce", RpcTarget.All, contactPoint.x, contactPoint.y + 2, force.x, force.y);
            return;
        }
    }
}