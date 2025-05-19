using UnityEngine;
using Photon.Pun;

public class HitboxTrigger : MonoBehaviour
{
    public PhotonView ownerPhotonView;                    // �������� PhotonView
    public PlayerController ownerPlayerController;        // �������� PlayerController ����
    public GameObject fallingSwordPrefab;                 // ƨ�ܼ� ����߸� Į ������

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!ownerPhotonView.IsMine) return;

        // ����� ���п� ����� ��
        if (other.CompareTag("Shield"))
        {
            Vector2 contactPoint = transform.position;

            // �ݻ� ���� ��� (�ڽ��� �ݴ� ���� + �ణ ����)
            Vector2 forceDir = (ownerPhotonView.transform.position - other.transform.position).normalized;
            forceDir += Vector2.up * 0.5f;

            // ���� Į �����
            if (ownerPlayerController != null && ownerPlayerController.sword != null)
            {
                ownerPlayerController.sword.SetActive(false);
            }

            // ƨ��� Į ���� ��û
            ownerPhotonView.RPC("DropSwordWithForce", RpcTarget.All, contactPoint.x, contactPoint.y, forceDir.x, forceDir.y);
            return;
        }

        // ��� ��ü�� ����� ��
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