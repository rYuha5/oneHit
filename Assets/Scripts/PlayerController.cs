using System.Collections;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
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

    public bool isBlocking = false;
    private bool lastSentMoveState = false;
    public bool isFrozen = false;

    private float curScaleX;

    void Start()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        col2D = GetComponent<CapsuleCollider2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        defaultGravityScale = rb.gravityScale;
        curScaleX = transform.localScale.x;

        swordController = sword.GetComponent<SwordController>();
        var hitbox = sword.GetComponentInChildren<HitboxTrigger>();

        if (swordController == null)
            Debug.LogWarning("swordController 연결 실패");

        if (spumPrefab != null)
        {
            spumPrefab.OverrideControllerInit();
            if (!spumPrefab.allListsHaveItemsExist())
                Debug.LogWarning("애니메이션 리스트 비어 있음");
        }

        if (hitbox != null)
        {
            hitbox.ownerPhotonView = pv;
            hitbox.ownerPlayerController = this;
            hitbox.fallingSwordPrefab = fallingSwordPrefab;
        }

        sword.SetActive(true);
        shield.SetActive(false);
    }

    void Update()
    {
        if (!pv.IsMine || isFrozen) return;

        float h = Input.GetAxisRaw("Horizontal");
        bool isNowMoving = h != 0;

        if (isNowMoving != lastSentMoveState)
        {
            lastSentMoveState = isNowMoving;
            pv.RPC("SyncMoveState", RpcTarget.Others, isNowMoving);
            spumPrefab.PlayAnimation(isNowMoving ? PlayerState.MOVE : PlayerState.IDLE, 0);
        }

        rb.velocity = new Vector2(h * moveSpeed, rb.velocity.y);

        isGrounded = Physics2D.OverlapCircle((Vector2)transform.position + new Vector2(0, -0.5f), 0.07f, 1 << LayerMask.NameToLayer("Ground"));
        if (isGrounded && !canJump) canJump = true;

        if (!isGrounded && Mathf.Abs(h) == 0)
            spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);

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
        else if (h == 0 && isGrounded)
        {
            spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
        }

        rb.gravityScale = (Input.GetKey(KeyCode.DownArrow) && !isGrounded) ? fastFallGravityScale : defaultGravityScale;

        if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded && canJump)
        {
            spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
            canJump = false;
            Jump();
            pv.RPC("JumpRPC", RpcTarget.Others);
        }

        if (Input.GetKeyDown(KeyCode.Z) && canAttack && !isBlocking && hasSword)
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

        if (!pv.IsMine)
        {
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

    public void ResetForNextRound()
    {
        hasSword = true;
        sword.SetActive(true);
        shield.SetActive(false);
        swordController.hitbox = null;
        isBlocking = false;
        rb.velocity = Vector2.zero;
        spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
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
            spumPrefab?.PlayAnimation(isMoving ? PlayerState.MOVE : PlayerState.IDLE, 0);
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
        spumPrefab?.PlayAnimation(PlayerState.ATTACK, 0);
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
        rb.velocity = Vector2.zero;
    }

    [PunRPC]
    public void DropSwordWithForce(float x, float y, float fx, float fy)
    {
        if (!pv.IsMine) return;

        hasSword = false;
        sword.SetActive(false);
        Vector2 spawnPos = new Vector2(x, y);
        GameObject droppedSword = PhotonNetwork.Instantiate("fallingweapon", spawnPos, Quaternion.identity);
        droppedSword.GetComponent<Rigidbody2D>()?.AddForce(new Vector2(fx, fy), ForceMode2D.Impulse);

        pv.RPC("SetHasSword", RpcTarget.AllBuffered, false);
    }

    [PunRPC]
    public void SetHasSword(bool value)
    {
        hasSword = value;
        sword.SetActive(hasSword && !isBlocking);
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
}
