using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public Transform[] spawnPoints;
    public GameObject[] playerObjects;
    public Text scoreTextP1;
    public Text scoreTextP2;
    public Text resultText;
    public Text countdownText;

    public GameObject playerPrefab;
    private GameObject localPlayer;
    private PhotonView pv;

    public int roundToWin = 3;
    private int[] scores = new int[2];
    private bool isFirstRound = true;
    private bool matchOver = false;

    void Awake()
    {
        Instance = this;
        pv = GetComponent<PhotonView>();
        playerObjects = new GameObject[2]; // 2인 대전 기준
    }

    void Start()
    {
        resultText.text = "";

        if (PhotonNetwork.IsConnected)
        {
            SpawnImmediately();
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            StartCoroutine(WaitForSecondPlayer());
        }
        else
        {
            StartInitialCountdown();
        }
    }

    void SpawnImmediately()
    {
        int index = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        Vector3 spawnPos = spawnPoints[Mathf.Clamp(index, 0, spawnPoints.Length - 1)].position;
        localPlayer = PhotonNetwork.Instantiate("knight", spawnPos, Quaternion.identity);
        playerObjects[index] = localPlayer;
    }

    IEnumerator WaitForSecondPlayer()
    {
        while (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            yield return null;

        yield return new WaitForSeconds(1f);
        StartInitialCountdown();
    }

    void StartInitialCountdown()
    {
        UpdateScoreUI();
        FreezeAllPlayers(true);
        DisableAllHitboxes();
        MoveAllPlayersToSpawn();

        if (PhotonNetwork.IsMasterClient)
        {
            double startTime = PhotonNetwork.Time + 1.0;
            pv.RPC("StartCountdownRPC", RpcTarget.All, startTime);
        }
    }

    [PunRPC]
    void StartCountdownRPC(double startTime)
    {
        StartCoroutine(CountdownRoutine(startTime));
    }

    IEnumerator CountdownRoutine(double startTime)
    {
        Rigidbody2D rb = localPlayer.GetComponent<Rigidbody2D>();
        PlayerController controller = localPlayer.GetComponent<PlayerController>();

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        controller.enabled = false;
        controller.isFrozen = true;
        countdownText.gameObject.SetActive(true);

        int countdownTime = isFirstRound ? 5 : 3;
        double endTime = startTime + countdownTime;

        while (PhotonNetwork.Time < endTime)
        {
            int secondsLeft = Mathf.CeilToInt((float)(endTime - PhotonNetwork.Time));
            countdownText.text = secondsLeft.ToString();
            yield return null;
        }

        countdownText.text = "START!";
        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);

        rb.isKinematic = false;
        controller.enabled = true;
        controller.isFrozen = false;

        isFirstRound = false;
    }

    [PunRPC]
    public void OnPlayerDefeated(int loserId)
    {
        if (matchOver) return;

        int winnerId = (loserId == 0) ? 1 : 0;
        scores[winnerId]++;

        UpdateScoreUI();
        FreezeAllPlayers(true);
        DisableAllHitboxes();

        if (scores[winnerId] >= roundToWin)
        {
            matchOver = true;
            pv.RPC("ShowRoundResult", RpcTarget.All, $"PLAYER {winnerId + 1} WINS THE MATCH!");
        }
        else
        {
            pv.RPC("ShowRoundResult", RpcTarget.All, $"PLAYER {winnerId + 1} wins the round!");
        }

        StartCoroutine(NextRoundAfterDelay());
    }

    IEnumerator NextRoundAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        DestroyAllDroppedWeapons();
        MoveAllPlayersToSpawn();

        if (!matchOver && PhotonNetwork.IsMasterClient)
        {
            double nextStartTime = PhotonNetwork.Time + 1.0;
            pv.RPC("StartCountdownRPC", RpcTarget.All, nextStartTime);
            pv.RPC("ClearResultText", RpcTarget.All);
        }
    }

    void UpdateScoreUI()
    {
        scoreTextP1.text = $"P1: {scores[0]}";
        scoreTextP2.text = $"P2: {scores[1]}";
    }

    [PunRPC]
    void ShowRoundResult(string msg)
    {
        resultText.text = msg;
    }

    [PunRPC]
    void ClearResultText()
    {
        resultText.text = "";
    }

    void FreezeAllPlayers(bool freeze)
    {
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            var controller = p.GetComponent<PlayerController>();
            var rb = p.GetComponent<Rigidbody2D>();

            controller.isFrozen = freeze;
            if (freeze)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true;
                controller.spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
            }
            else
            {
                rb.isKinematic = false;
            }
        }
    }

    void DisableAllHitboxes()
    {
        foreach (var swordController in FindObjectsOfType<SwordController>())
        {
            if (swordController.hitbox != null)
            {
                Destroy(swordController.hitbox);
                swordController.hitbox = null;
            }
        }
    }

    void MoveAllPlayersToSpawn()
    {
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView view = p.GetComponent<PhotonView>();
            if (view != null)
            {
                int index = view.Owner.ActorNumber - 1;
                Vector3 spawnPos = spawnPoints[Mathf.Clamp(index, 0, spawnPoints.Length - 1)].position;
                view.RPC("ForceSetPositionRPC", RpcTarget.All, spawnPos.x, spawnPos.y);
            }
        }
    }

    void DestroyAllDroppedWeapons()
    {
        foreach (var weapon in GameObject.FindGameObjectsWithTag("FallingSword"))
        {
            PhotonView view = weapon.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
            {
                PhotonNetwork.Destroy(weapon);
            }
        }
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null && !pc.hasSword)
            {
                pc.sword.SetActive(true);
            }
        }
    }
}
