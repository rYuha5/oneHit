using System.Collections;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private float defaultGravityScale;
    public float fastFallGravityScale = 30f;
    private bool canAttack = true;
    public float attackCooldown = 0.5f;

    private PhotonView pv;
    private Rigidbody2D rb;
    private CapsuleCollider2D col2D;

    public SPUM_Prefabs spumPrefab;
    public SwordController swordController;
    public GameObject sword;
    public GameObject shield;
    public GameObject fallingSwordPrefab;

    private bool isGrounded = true;
    private bool canJump = true;
    private bool hasSword = true;

    private Vector3 curPos;
    private float curScaleX;
    private bool isMoving = false;
    private bool isBlocking = false;
    private bool lastSentMoveState = false;
    private float moveSyncCooldown = 0.1f;
    private float moveSyncTimer = 0f;


    void Start()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        col2D = GetComponent<CapsuleCollider2D>();
        defaultGravityScale = rb.gravityScale;
        curScaleX = transform.localScale.x;
        swordController = sword.GetComponent<SwordController>();
        var hitbox = sword.GetComponentInChildren<HitboxTrigger>();

        if (swordController == null)
        {
            Debug.LogWarning("swordController 연결 실패! R_Weapon에 SwordController가 붙었는지 확인하세요.");
        }

        if (spumPrefab != null)
        {
            spumPrefab.OverrideControllerInit();
            if (!spumPrefab.allListsHaveItemsExist())
                Debug.LogWarning("애니메이션 리스트에 비어있는 항목 있음!");
        }
        if (hitbox != null)
        {
            hitbox.ownerPhotonView = pv;
            hitbox.ownerPlayerController = this;
            hitbox.fallingSwordPrefab = fallingSwordPrefab;
        }
        Debug.Log($"spumPrefab: {spumPrefab != null}");
        sword.SetActive(true);
        shield.SetActive(false);
    }

    void Update()
    {
        if (pv.IsMine)
        {
            float h = Input.GetAxisRaw("Horizontal");
            bool isNowMoving = (h != 0);

            moveSyncTimer += Time.deltaTime;

            if (isNowMoving != lastSentMoveState && moveSyncTimer >= moveSyncCooldown)
            {
                lastSentMoveState = isNowMoving;
                moveSyncTimer = 0f;

                pv.RPC("SyncMoveState", RpcTarget.Others, isNowMoving);
                spumPrefab.PlayAnimation(isNowMoving ? PlayerState.MOVE : PlayerState.IDLE, 0);
            }
            rb.velocity = new Vector2(h * moveSpeed, rb.velocity.y);
            curPos = transform.position;

            isGrounded = Physics2D.OverlapCircle((Vector2)transform.position + new Vector2(0, -0.5f), 0.07f, 1 << LayerMask.NameToLayer("Ground"));
            if (isGrounded && !canJump) canJump = true;

            if (!isGrounded && Mathf.Abs(h) == 0)
            {
                spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
            }

            if (h > 0)
            {
                pv.RPC("FlipScaleRPC", RpcTarget.AllBuffered, -1f);
                spumPrefab?.PlayAnimation(PlayerState.MOVE, 0);
            }
            else if (h < 0)
            {
                pv.RPC("FlipScaleRPC", RpcTarget.AllBuffered, 1f);
                spumPrefab?.PlayAnimation(PlayerState.MOVE, 0);
            }

            if (h == 0 && isGrounded)
            {
                spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
            }

            if (Input.GetKey(KeyCode.DownArrow) && !isGrounded)
            {
                rb.gravityScale = fastFallGravityScale;
            }
            else
            {
                rb.gravityScale = defaultGravityScale;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded && canJump)
            {
                spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
                canJump = false;
                Jump();
                pv.RPC("JumpRPC", RpcTarget.Others);
            }

            if (Input.GetKeyDown(KeyCode.Z) && canAttack && hasSword)
            {
                canAttack = false;
                if (spumPrefab != null)
                    spumPrefab.PlayAnimation(PlayerState.ATTACK, 0);

                pv.RPC("PlayAttack", RpcTarget.Others);
                swordController?.StartAttack(); // 히트박스 실행

                StartCoroutine(ResetAttackCooldown());
            }


            bool holdingX = Input.GetKey(KeyCode.X);

            if (holdingX && !isBlocking)
            {
                isBlocking = true;
                pv.RPC("PlayDefense", RpcTarget.Others);
                sword.SetActive(false);
                shield.SetActive(true);
                rb.velocity = Vector2.zero;
                spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
            }
            else if (!holdingX && isBlocking)
            {
                isBlocking = false;
                pv.RPC("NotPlayDefense", RpcTarget.Others);
                if(hasSword) sword.SetActive(true);
                shield.SetActive(false);
            }
        }
        else
        {
            if ((transform.position - curPos).sqrMagnitude >= 100)
                transform.position = curPos;
            else
                transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10);

            Vector3 scale = transform.localScale;
            scale.x = curScaleX;
            transform.localScale = scale;
        }
    }

    IEnumerator ResetAttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    [PunRPC]
    void FlipScaleRPC(float direction)
    {
        float absX = Mathf.Abs(transform.localScale.x);
        Vector3 scale = transform.localScale;
        scale.x = direction > 0 ? absX : -absX;
        transform.localScale = scale;
        curScaleX = scale.x;
    }

    [PunRPC]
    void SyncMoveState(bool isMoving)
    {
        if (!pv.IsMine)
        {
            spumPrefab.PlayAnimation(isMoving ? PlayerState.MOVE : PlayerState.IDLE, 0);
        }
    }

    [PunRPC]
    void JumpRPC()
    {
        if (!pv.IsMine) Jump();
    }

    [PunRPC]
    void PlayAttack()
    {
        if (spumPrefab != null)
        {
            spumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
        }
        swordController?.StartAttack();
    }

    [PunRPC]
    void PlayDefense()
    {
        if (spumPrefab != null)
        {
            sword.SetActive(false);
            shield.SetActive(true);
            spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    [PunRPC]
    void NotPlayDefense()
    {
        if (spumPrefab != null)
        {
            sword.SetActive(true);
            shield.SetActive(false);
        }
    }

    [PunRPC]
    public void ForceSetPositionRPC(float x, float y)
    {
        Vector3 newPos = new Vector3(x, y, transform.position.z);
        transform.position = newPos;
        curPos = newPos;
        rb.velocity = Vector2.zero;
    }

    [PunRPC]
    void DropSwordWithForce(float x, float y)
    {
        if (sword != null)
        {
            hasSword = false;
            sword.SetActive(false); // 원래 칼 숨기기
        }

        if (fallingSwordPrefab != null)
        {
            Vector2 spawnPos = new Vector2(x, y);

            // 기본 위 방향
            Vector2 force = Vector2.up;

            // 좌우 중 하나를 랜덤으로 추가
            if (Random.value < 0.5f)
                force += Vector2.left;
            else
                force += Vector2.right;

            force = force.normalized * 5f; // 튕기는 힘 크기 조절

            GameObject droppedSword = Instantiate(fallingSwordPrefab, spawnPos, Quaternion.identity);
            Rigidbody2D rb = droppedSword.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(force, ForceMode2D.Impulse);
            }
        }
    }

    IEnumerator EnableSwordAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sword != null)
            sword.SetActive(true);
    }

    [PunRPC]
    public void TakeDamage()
    {
        Debug.Log("피격당함!");
        spumPrefab.PlayAnimation(PlayerState.DAMAGED, 0);
        // hp--; if (hp <= 0) Die();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(curPos);
            stream.SendNext(hasSword);
            stream.SendNext(transform.localScale.x);
        }
        else
        {
            curPos = (Vector3)stream.ReceiveNext();
            hasSword = (bool)stream.ReceiveNext();
            curScaleX = (float)stream.ReceiveNext();
        }
    }
}