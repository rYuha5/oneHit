using UnityEngine;
using Photon.Pun;

public class FallingSword : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        // ���� �÷��̾ ����
        if (!player.photonView.IsMine) return;

        // �� Į �ٽ� ���̰�
        if (player.sword != null)
        {
            player.sword.SetActive(true);
        }

        // �� ������ Į ������Ʈ ����
        Destroy(gameObject);
    }
}