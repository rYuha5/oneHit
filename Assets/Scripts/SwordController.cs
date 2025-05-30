using System.Collections;
using UnityEngine;
using Photon.Pun;

public class SwordController : MonoBehaviourPunCallbacks
{
    public GameObject hitboxPrefab;
    public GameObject fallingSwordPrefab;
    public float attackDuration = 0.3f;

    public HitboxTrigger hitbox;  // 현재 활성 히트박스
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
            Destroy(hitbox.gameObject);  // 기존 히트박스 제거
            hitbox = null;
            yield return null;
        }

        GameObject hitboxObj = Instantiate(hitboxPrefab, transform);
        hitboxObj.transform.localPosition = new Vector3(0.5f, 0f, 0f); // 칼끝 위치로 조정

        hitbox = hitboxObj.GetComponent<HitboxTrigger>();
        if (hitbox != null)
        {
            var pc = GetComponentInParent<PlayerController>();
            hitbox.ownerPhotonView = pc.pv;
            hitbox.ownerPlayerController = pc;
            hitbox.fallingSwordPrefab = fallingSwordPrefab;
        }

        yield return new WaitForSeconds(attackDuration);

        if (hitbox != null)
        {
            Destroy(hitbox.gameObject);  // 공격 종료 후 히트박스 제거
            hitbox = null;
        }

        isAttacking = false;
    }
}
