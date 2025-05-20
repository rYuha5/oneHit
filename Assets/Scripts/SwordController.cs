using System.Collections;
using UnityEngine;
using Photon.Pun;

public class SwordController : MonoBehaviourPunCallbacks
{
    public GameObject hitboxPrefab;
    public GameObject fallingSwordPrefab;
    public float attackDuration = 0.3f;

    [HideInInspector] public GameObject hitbox;  // 현재 활성 히트박스
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
            Destroy(hitbox);        // 기존 히트박스 제거
            yield return null;     // 1 프레임 기다림
        }

        hitbox = Instantiate(hitboxPrefab, transform);
        hitbox.transform.localPosition = new Vector3(0.5f, 0f, 0f); // 칼끝 위치로 조정 (필요시 수정)

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
            Destroy(hitbox);  // 공격 종료 후 히트박스 제거

        isAttacking = false;
    }

}
