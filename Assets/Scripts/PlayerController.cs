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

    public PhotonView pv;
    private Rigidbody2D rb;
    private CapsuleCollider2D col2D;

    public SPUM_Prefabs spumPrefab;
    public SwordController swordController;
    public GameObject sword;
    public GameObject shield;
    public GameObject fallingSwordPrefab;

    private bool isGrounded = true;
    private bool canJump = true;
    public bool hasSword = true;

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

            if (Input.GetKeyDown(KeyCode.Z) && canAttack)
            {
                canAttack = false;
                spumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
                pv.RPC("PlayAttack", RpcTarget.All);
                StartCoroutine(ResetAttackCooldown());
            }


            bool holdingX = Input.GetKey(KeyCode.X);

            if (holdingX && !isBlocking)
            {
                isBlocking = true;
                pv.RPC("EnterDefenseMode", RpcTarget.All);
                rb.velocity = Vector2.zero;
                spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
            }
            else if (!holdingX && isBlocking)
            {
                isBlocking = false;
                pv.RPC("ExitDefenseMode", RpcTarget.All);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                pv.RPC("EnterDefenseMode", RpcTarget.All);
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
            spumPrefab.PlayAnimation(PlayerState.ATTACK, 0);

        swordController?.StartAttack();
    }

    [PunRPC]
    public void EnterDefenseMode()
    {
        sword.SetActive(false);
        shield.SetActive(true);
    }

    [PunRPC]
    public void ExitDefenseMode()
    {
        if (hasSword) sword.SetActive(true);
        shield.SetActive(false);
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
    public void DropSwordWithForce(float x, float y, float fx, float fy)
    {
        if (!pv.IsMine) return;

        if (sword != null)
        {
            hasSword = false;
            sword.SetActive(false);
        }

        pv.RPC("SpawnSword", RpcTarget.All, x, y, fx, fy);
        pv.RPC("SetHasSword", RpcTarget.AllBuffered, false);
    }

    [PunRPC]
    public void SpawnSword(float x, float y, float fx, float fy)
    {
        if (!pv.IsMine) return;
        Vector2 spawnPos = new Vector2(x, y);
        Vector2 force = new Vector2(fx, fy);

        GameObject droppedSword = PhotonNetwork.Instantiate("fallingweapon", spawnPos, Quaternion.identity);
        Rigidbody2D rb = droppedSword.GetComponent<Rigidbody2D>();  
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    [PunRPC]
    public void SetHasSword(bool value)
    {
        hasSword = value;
        sword.SetActive(value);
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
        spumPrefab?.PlayAnimation(PlayerState.DEATH, 0);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(curPos);
            stream.SendNext(hasSword);
            stream.SendNext(isBlocking);
            stream.SendNext(transform.localScale.x);
        }
        else
        {
            curPos = (Vector3)stream.ReceiveNext();
            hasSword = (bool)stream.ReceiveNext();
            isBlocking = (bool)stream.ReceiveNext();
            curScaleX = (float)stream.ReceiveNext();

            sword.SetActive(hasSword && !isBlocking);
            shield.SetActive(isBlocking);
        }
    }
}