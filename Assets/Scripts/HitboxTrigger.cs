using UnityEngine;
using Photon.Pun;

public class HitboxTrigger : MonoBehaviour
{
    public PhotonView ownerPhotonView;                    // 공격자의 PhotonView
    public PlayerController ownerPlayerController;        // 공격자의 PlayerController 참조
    public GameObject fallingSwordPrefab;                 // 튕겨서 떨어뜨릴 칼 프리팹

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!ownerPhotonView.IsMine) return;

        // 상대의 방패에 닿았을 때
        if (other.CompareTag("Shield"))
        {
            Vector2 contactPoint = transform.position;

            // 원래 칼 숨기기
            if (ownerPlayerController != null && ownerPlayerController.sword != null)
            {
                ownerPlayerController.sword.SetActive(false);
            }

            // 튕기는 칼 생성 요청
            ownerPhotonView.RPC("DropSwordWithForce", RpcTarget.All, contactPoint.x, contactPoint.y);
            return;
        }

        // 상대 본체에 닿았을 때
        if (other.CompareTag("Player"))
        {
            PhotonView targetPV = other.GetComponent<PhotonView>();
            if (targetPV != null && !targetPV.IsMine)
            {
                targetPV.RPC("TakeDamage", RpcTarget.All);
            }
        }
    }
}