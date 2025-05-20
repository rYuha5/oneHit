using System.Collections;
using UnityEngine;
using Photon.Pun;

public class SwordController : MonoBehaviourPunCallbacks
{
    public GameObject hitboxPrefab;
    public GameObject fallingSwordPrefab;
    public float attackDuration = 0.3f;

    [HideInInspector] public GameObject hitbox;  // ���� Ȱ�� ��Ʈ�ڽ�
    private bool isAttacking = false;

    public void StartAttack()
    {
        var pc = GetComponentInParent<PlayerController>();
        if (pc != null && (pc.isBlocking || !pc.hasSword)) return;

        if (!isAttacking)
            StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (hitbox != null)
        {
            Destroy(hitbox);        // ���� ��Ʈ�ڽ� ����
            yield return null;     // 1 ������ ��ٸ�
        }

        hitbox = Instantiate(hitboxPrefab, transform);
        hitbox.transform.localPosition = new Vector3(0.5f, 0f, 0f); // Į�� ��ġ�� ���� (�ʿ�� ����)

        var trigger = hitbox.GetComponent<HitboxTrigger>();
        if (trigger != null)
        {
            var pc = GetComponentInParent<PlayerController>();
            trigger.ownerPhotonView = pc.pv;
            trigger.ownerPlayerController = pc;
            trigger.fallingSwordPrefab = fallingSwordPrefab;
        }

        yield return new WaitForSeconds(attackDuration);

        if (hitbox != null)
            Destroy(hitbox);  // ���� ���� �� ��Ʈ�ڽ� ����

        isAttacking = false;
    }

}
