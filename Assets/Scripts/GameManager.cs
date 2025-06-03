using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public Transform[] spawnPoints;
    public Text scoreTextP1;
    public Text scoreTextP2;
    public Text resultText;
    public Text countdownText;
    public Text exitCountdownText;

    public GameObject AfterMatchPanel;
    public Button replayButton;
    public Button exitButton;

    private GameObject localPlayer;
    private PhotonView pv;

    public int roundToWin = 3;
    private int[] scores = new int[2];
    private bool isFirstRound = true;
    private bool matchOver = false;

    private bool replayConfirmed = false;
    private double replayRequestTime = 0;
    private bool localReplayRequested = false;

    public GameObject arrowPrefab; // 인스펙터에서 할당
    private GameObject localArrow; // 인스턴스화된 화살표

    void Awake()
    {
        Instance = this;
        pv = GetComponent<PhotonView>();
    }

    void Start()
    {
        resultText.text = "";
        exitCountdownText.text = "";

        if (PhotonNetwork.IsConnected)
            SpawnImmediately();

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            StartCoroutine(WaitForSecondPlayer());
        else
            StartInitialCountdown();

        AfterMatchPanel?.SetActive(false);
        replayButton.onClick.AddListener(OnClickReplay);
        exitButton.onClick.AddListener(OnClickExit);
    }

    void SpawnImmediately()
    {
        Vector3 spawnPos = GetSpawnPosition(PhotonNetwork.LocalPlayer);
        localPlayer = PhotonNetwork.Instantiate("knight", spawnPos, Quaternion.identity);
        if (arrowPrefab != null)
        {
            localArrow = Instantiate(arrowPrefab);
            Arrow follower = localArrow.GetComponent<Arrow>();
            if (follower != null)
                follower.target = localPlayer.transform;
        }

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

    Vector3 GetSpawnPosition(Player player)
    {
        Player[] sorted = PhotonNetwork.PlayerList;
        int index = System.Array.IndexOf(sorted, player);
        return spawnPoints[Mathf.Clamp(index, 0, spawnPoints.Length - 1)].position;
    }

    [PunRPC]
    void StartCountdownRPC(double startTime)
    {
        StartCoroutine(CountdownRouine(startTime));
    }

    IEnumerator CountdownRouine(double startTime)
    {
        var controller = localPlayer.GetComponent<PlayerController>();
        var rb = localPlayer.GetComponent<Rigidbody2D>();

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        rb.gravityScale = 0;
        controller.isFrozen = true;
        controller.enabled = false;

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
        rb.gravityScale = 1;
        controller.isFrozen = false;
        controller.enabled = true;
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

        string winnerName = PhotonNetwork.PlayerList[winnerId].NickName;

        if (scores[winnerId] >= roundToWin)
        {
            matchOver = true;
            pv.RPC("ShowRoundResult", RpcTarget.All, $"{winnerName} WINS THE MATCH!");
            EndMatch();
        }
        else
        {
            pv.RPC("ShowRoundResult", RpcTarget.All, $"{winnerName} wins the round!");
        }

        StartCoroutine(NextRoundAfterDelay());
    }

    IEnumerator NextRoundAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        foreach (var player in FindObjectsOfType<PlayerController>())
            player.ResetForNextRound();

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
        Player[] players = PhotonNetwork.PlayerList;

        if (players.Length >= 2)
        {
            scoreTextP1.text = $"{players[0].NickName}: {scores[0]}";
            scoreTextP2.text = $"{players[1].NickName}: {scores[1]}";
        }
        else if (players.Length == 1)
        {
            scoreTextP1.text = $"{players[0].NickName}: {scores[0]}";
            scoreTextP2.text = "대기 중...";
        }
    }

    [PunRPC] void ShowRoundResult(string msg) => resultText.text = msg;
    [PunRPC] void ClearResultText() => resultText.text = "";

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
                rb.gravityScale = 0;
                controller.spumPrefab?.PlayAnimation(PlayerState.IDLE, 0);
            }
            else
            {
                rb.isKinematic = false;
                rb.gravityScale = 1;
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
                Vector3 spawnPos = GetSpawnPosition(view.Owner);
                var rb = p.GetComponent<Rigidbody2D>();
                rb.velocity = Vector2.zero;
                rb.isKinematic = true;
                rb.gravityScale = 0;
                view.RPC("ForceSetPositionRPC", RpcTarget.All, spawnPos.x, spawnPos.y);
            }
        }
    }

    void DestroyAllDroppedWeapons()
    {
        foreach (var weapon in GameObject.FindGameObjectsWithTag("FallingSword"))
        {
            var view = weapon.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
                PhotonNetwork.Destroy(weapon);
        }
    }

    void EndMatch()
    {
        if (!matchOver) return;

        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            player.isFrozen = true;
            player.rb.velocity = Vector2.zero;
        }

        StartCoroutine(ShowAfterMatchPanelWithDelay(2f));
    }

    IEnumerator ShowAfterMatchPanelWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        resultText.text = "";
        AfterMatchPanel?.SetActive(true);
        StartCoroutine(AutoExitIfNoResponse());
    }

    void OnClickReplay()
    {
        AfterMatchPanel.SetActive(false);
        countdownText.gameObject.SetActive(false); // 입력 대기 텍스트 끄기

        FreezeAllPlayers(false);
        DisableAllHitboxes();

        string key = $"ReplayReady_{PhotonNetwork.LocalPlayer.ActorNumber}";
        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { key, true } });

        localReplayRequested = true;
        replayRequestTime = PhotonNetwork.Time;
        StartCoroutine(WaitForOtherReplayOrExit());
    }

    void OnClickExit()
    {
        AfterMatchPanel.SetActive(false);
        countdownText.gameObject.SetActive(false); // 입력 대기 텍스트 끄기
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.LoadLevel("LobbyScene");
    }

    IEnumerator WaitForOtherReplayOrExit()
    {
        float timeout = 5f;

        while (PhotonNetwork.Time < replayRequestTime + timeout)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && AllPlayersConfirmedReplay())
            {
                ExecuteReplay();
                yield break;
            }

            if (localReplayRequested)
            {
                float remaining = (float)(replayRequestTime + timeout - PhotonNetwork.Time);
                exitCountdownText.text = $"상대 입력까지 대기.. {Mathf.CeilToInt(remaining)}초";
            }

            yield return null;
        }

        exitCountdownText.text = "";
        OnClickExit();
    }

    bool AllPlayersConfirmedReplay()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            string key = $"ReplayReady_{player.ActorNumber}";
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key) ||
                !(bool)PhotonNetwork.CurrentRoom.CustomProperties[key])
                return false;
        }
        return true;
    }

    void ExecuteReplay()
    {
        replayConfirmed = false;
        matchOver = false;
        isFirstRound = true;
        resultText.text = "";
        countdownText.text = "";
        exitCountdownText.text = "";
        AfterMatchPanel?.SetActive(false);
        scores[0] = 0;
        scores[1] = 0;

        var keys = new ExitGames.Client.Photon.Hashtable();
        foreach (var p in PhotonNetwork.PlayerList)
            keys[$"ReplayReady_{p.ActorNumber}"] = false;
        PhotonNetwork.CurrentRoom.SetCustomProperties(keys);

        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            player.ResetForNextRound();
            player.isFrozen = false;
        }

        FreezeAllPlayers(false);
        DisableAllHitboxes();
        UpdateScoreUI();
        StartInitialCountdown();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        string key = $"ReplayReady_{newPlayer.ActorNumber}";
        bool replayDone = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key) &&
                          (bool)PhotonNetwork.CurrentRoom.CustomProperties[key];

        if (matchOver && !replayDone && PhotonNetwork.LocalPlayer.ActorNumber == newPlayer.ActorNumber)
            AfterMatchPanel?.SetActive(true);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!matchOver && PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            StartCoroutine(AutoLeaveDueToPlayerExit());
        }
    }

    IEnumerator AutoLeaveDueToPlayerExit()
    {
        float countdown = 5f;

        while (countdown > 0f)
        {
            exitCountdownText.text = $"상대가 나갔습니다. {Mathf.CeilToInt(countdown)}초 후 로비로 이동합니다...";
            yield return new WaitForSeconds(1f);
            countdown -= 1f;
        }

        PhotonNetwork.LeaveRoom();
    }

    IEnumerator AutoExitIfNoResponse()
    {
        float waitTime = 5f;
        float elapsed = 0f;

        countdownText.gameObject.SetActive(true);

        while (elapsed < waitTime)
        {
            if (!AfterMatchPanel.activeSelf) // 사용자가 Replay 또는 Exit을 눌러 패널이 닫힌 경우
            {
                countdownText.gameObject.SetActive(false);
                yield break;
            }

            float remaining = waitTime - elapsed;
            countdownText.text = $"입력 대기 중... {Mathf.CeilToInt(remaining)}";
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }

        countdownText.gameObject.SetActive(false);
        PhotonNetwork.LeaveRoom();
    }
}
