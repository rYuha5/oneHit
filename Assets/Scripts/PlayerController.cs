using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    public PhotonView pv;
    private Rigidbody2D rb;
    private Animator animator;
    private CapsuleCollider2D col2D;

    private bool isGrounded = true;
    public GameObject sword;  // 검 오브젝트 (자식으로 붙여야 함)
    private bool hasSword = true;

    Vector3 curPos;
    float curScaleX;

    void Start()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        col2D = GetComponent<CapsuleCollider2D>();
        curScaleX = transform.localScale.x;
    }

    void Update()
    {
        if (pv.IsMine)
        {
            float h = Input.GetAxisRaw("Horizontal");
            rb.velocity = new Vector2(h * moveSpeed, rb.velocity.y);
            curPos = transform.position;

            if (h != 0)
            {
                animator.SetBool("1_Move", true);
                float direction = h > 0 ? -1f : 1f;
                if (transform.localScale.x != direction)
                {
                    pv.RPC("FlipScaleRPC", RpcTarget.AllBuffered, direction);
                }
            }
            else
            {
                animator.SetBool("1_Move", false);
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

    [PunRPC]
    void FlipScaleRPC(float direction)
    {
        Vector3 scale = transform.localScale;
        scale.x = direction;
        transform.localScale = scale;
        curScaleX = direction;
    }

    [PunRPC]
    void JumpRPC()
    {
        rb.velocity = Vector2.zero;
        rb.AddForce(Vector2.up * 700);
    }

    [PunRPC]
    public void ForceSetPositionRPC(float x, float y)
    {
        Vector3 newPos = new Vector3(x, y, transform.position.z);
        transform.position = newPos;
        curPos = newPos;
        rb.velocity = Vector2.zero;
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
