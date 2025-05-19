using System.Collections;
using UnityEngine;

public class SwordController : MonoBehaviour
{
    public GameObject hitbox;                // 히트박스 오브젝트 (BoxCollider2D, isTrigger)
    public float attackDuration = 1f;      // 히트박스 유지 시간
    private bool isAttacking = false;

    void Start()
    {
        if (hitbox != null)
            hitbox.SetActive(false);         // 시작 시 비활성화
    }

    public void StartAttack()
    {
        if (!isAttacking)
            StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (hitbox != null)
            hitbox.SetActive(true);

        yield return new WaitForSeconds(attackDuration);

        if (hitbox != null)
            hitbox.SetActive(false);

        isAttacking = false;
    }
}