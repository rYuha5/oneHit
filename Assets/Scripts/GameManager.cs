// GameManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public Transform[] spawnPoints;
    public Text countdownText;
    public GameObject playerPrefab;

    private GameObject localPlayer;
    private PhotonView pv;

    private void Awake()
    {
        Instance = this;
        pv = GetComponent<PhotonView>();
        if (pv == null)
            Debug.LogError("PhotonView가 GameManager 오브젝트에 없습니다!");
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnImmediately();
            StartCoroutine(WaitForSecondPlayer());
        }
        else
        {
            Debug.LogWarning("Photon 연결이 안 되어 있음!");
        }
    }

    void SpawnImmediately()
    {
        int index = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        Vector3 spawnPos = spawnPoints[Mathf.Clamp(index, 0, spawnPoints.Length - 1)].position;

        localPlayer = PhotonNetwork.Instantiate("knight", spawnPos, Quaternion.identity);
        Debug.Log("Spawned at " + spawnPos);

        localPlayer.GetComponent<PlayerController>().enabled = true;
    }

    IEnumerator WaitForSecondPlayer()
    {
        while (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        int index = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        Vector3 spawnPos = spawnPoints[Mathf.Clamp(index, 0, spawnPoints.Length - 1)].position;

        localPlayer.GetComponent<PhotonView>().RPC(
            "ForceSetPositionRPC",
            RpcTarget.All,
            spawnPos.x,
            spawnPos.y
        );

        yield return new WaitForSeconds(0.5f);
        pv.RPC("StartCountdown", RpcTarget.All);
    }

    [PunRPC]
    void StartCountdown()
    {
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        Rigidbody2D rb = localPlayer.GetComponent<Rigidbody2D>();
        PlayerController controller = localPlayer.GetComponent<PlayerController>();

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        controller.enabled = false;
        countdownText.gameObject.SetActive(true);

        for (int i = 5; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "START!";
        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);

        if (localPlayer != null)
        {
            rb.isKinematic = false;
            controller.enabled = true;
        }
    }
}
