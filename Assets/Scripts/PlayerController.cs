using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    private Vector3 networkPosition;
    private Vector3 networkVelocity;
    private float lastSyncTime;

    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float fastFallSpeed = 40f;
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
    private bool lastSentBlockingState = false;
    public bool isFrozen = false;

    private float curScaleX;
    private PlayerState? currentState = null;

    void Start()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        col2D = GetComponent<CapsuleCollider2D>();
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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

        if (isFrozen)
        {
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;
        }
    }

    void Update()
    {
        if (!pv.IsMine || isFrozen) return;

        bool holdingX = Input.GetKey(KeyCode.X);

        float h = Input.GetAxisRaw("Horizontal");
        bool isNowMoving = h != 0;

        if (isBlocking)
        {
            if (!holdingX)
            {
                isBlocking = false;
                pv.RPC("ExitDefenseMode", RpcTarget.All);
            }
            SyncAnimationState(PlayerState.DEFENSE);
            return;
        }
        else
        {
            if (holdingX && isGrounded)
            {
                SyncAnimationState(PlayerState.IDLE);
                isBlocking = true;
                pv.RPC("EnterDefenseMode", RpcTarget.All);
                rb.velocity = Vector2.zero;
                return;
            }
        }

        rb.velocity = new Vector2(h * moveSpeed, rb.velocity.y);

        if (Input.GetKey(KeyCode.DownArrow) && !isGrounded)
            rb.velocity += Vector2.down * fastFallSpeed * Time.fixedDeltaTime;

        isGrounded = Physics2D.OverlapCircle((Vector2)transform.position + new Vector2(0, -0.5f), 0.07f, 1 << LayerMask.NameToLayer("Ground"));
        if (isGrounded && !canJump) canJump = true;

        if (isNowMoving != lastSentMoveState || isBlocking != lastSentBlockingState)
        {
            lastSentMoveState = isNowMoving;
            lastSentBlockingState = isBlocking;
            pv.RPC("SyncMoveState", RpcTarget.Others, isNowMoving, isBlocking);
        }

        if (h > 0)
            pv.RPC("FlipScaleRPC", RpcTarget.AllBuffered, -1f);
        else if (h < 0)
            pv.RPC("FlipScaleRPC", RpcTarget.AllBuffered, 1f);

        if (isNowMoving)
            SyncAnimationState(PlayerState.MOVE);
        else
            SyncAnimationState(PlayerState.IDLE);

        if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded && canJump)
        {
            canJump = false;
            Jump();
            pv.RPC("JumpRPC", RpcTarget.Others);
        }

        if (Input.GetKeyDown(KeyCode.Z) && canAttack && hasSword)
        {
            canAttack = false;
            SyncAnimationState(PlayerState.ATTACK);
            pv.RPC("PlayAttack", RpcTarget.All);
            StartCoroutine(ResetAttackCooldown());
        }

        if (Input.GetKeyDown(KeyCode.R))
            pv.RPC("EnterDefenseMode", RpcTarget.All);
    }

    void FixedUpdate()
    {
        if (!pv.IsMine)
        {
            float lag = Time.time - lastSyncTime;
            Vector3 predictedPos = new Vector3(
                networkPosition.x + networkVelocity.x * lag,
                networkPosition.y + networkVelocity.y * lag,
                transform.position.z);
            transform.position = Vector3.Lerp(transform.position, predictedPos, Time.fixedDeltaTime * 10f);

            Vector3 scale = transform.localScale;
            scale.x = curScaleX;
            transform.localScale = scale;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(rb.velocity);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();
            lastSyncTime = Time.time;
        }
    }

    void SyncAnimationState(PlayerState state)
    {
        if (currentState == state) return;
        spumPrefab?.PlayAnimation(state, 0);
        currentState = state;

        if (pv.IsMine)
        {
            bool isMove = state == PlayerState.MOVE;
            bool isDefense = state == PlayerState.DEFENSE;
            pv.RPC("SyncMoveState", RpcTarget.Others, isMove, isDefense);
        }
    }

    IEnumerator ResetAttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void Jump()
    {
        rb.velocity = Vector2.zero;
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    public void ResetForNextRound()
    {
        hasSword = true;

        // 강제 삭제: 이전 라운드에서 떨어뜨린 칼 상태 제거
        PhotonNetwork.RemoveRPCs(pv);

        // 동기화된 상태로 다시 설정
        pv.RPC("SetHasSword", RpcTarget.AllBuffered, true);

        // 상태 재정의
        if (sword != null) sword.SetActive(true);
        if (shield != null) shield.SetActive(false);

        if (swordController != null)
            swordController.hitbox = null;

        isBlocking = false;
        rb.velocity = Vector2.zero;
        currentState = null;
        canAttack = true;
        SyncAnimationState(PlayerState.IDLE);
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
    void SyncMoveState(bool isMoving, bool isBlockingRemote)
    {
        if (!pv.IsMine)
        {
            if (isBlockingRemote)
                SyncAnimationState(PlayerState.DEFENSE);
            else if (isMoving)
                SyncAnimationState(PlayerState.MOVE);
            else
                SyncAnimationState(PlayerState.IDLE);
        }
    }

    [PunRPC] void JumpRPC() { if (!pv.IsMine) Jump(); }

    [PunRPC]
    void PlayAttack()
    {
        SyncAnimationState(PlayerState.ATTACK);
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
        shield.SetActive(false);
        if (hasSword && sword != null) sword.SetActive(true);
    }

    [PunRPC]
    public void ForceSetPositionRPC(float x, float y)
    {
        Vector3 newPos = new Vector3(x, y, transform.position.z);
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;
            rb.position = newPos;
        }
        transform.position = newPos;
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
        if (sword != null)
            sword.SetActive(hasSword && !isBlocking);
        if (hasSword && swordController != null)
            swordController.hitbox = null;
    }

    [PunRPC]
    public void TakeDamage()
    {
        SyncAnimationState(PlayerState.DEATH);
    }
}
