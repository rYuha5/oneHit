using UnityEngine;
using Photon.Pun;

public class FallingSword : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        // 로컬 플레이어만 반응
        if (!player.photonView.IsMine) return;

        // 내 칼 다시 보이게
        if (player.sword != null)
        {
            player.sword.SetActive(true);
        }

        // 이 떨어진 칼 오브젝트 삭제
        Destroy(gameObject);
    }
}