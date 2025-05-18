using System.Collections;
using UnityEngine;

public class SwordController : MonoBehaviour
{
    public GameObject hitbox;                // ��Ʈ�ڽ� ������Ʈ (BoxCollider2D, isTrigger)
    public float attackDuration = 1f;      // ��Ʈ�ڽ� ���� �ð�
    private bool isAttacking = false;

    void Start()
    {
        if (hitbox != null)
            hitbox.SetActive(false);         // ���� �� ��Ȱ��ȭ
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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Hit player: " + other.name);
            // ���⼭ ������ �Ǵ� �ǰ� ó�� ���� ȣ�� ����
        }
    }
}