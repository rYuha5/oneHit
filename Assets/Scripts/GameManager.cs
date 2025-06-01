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

        if (scores[winnerId] >= roundToWin)
        {
            matchOver = true;
            pv.RPC("ShowRoundResult", RpcTarget.All, $"PLAYER {winnerId + 1} WINS THE MATCH!");
            EndMatch();
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
        scoreTextP1.text = $"P1: {scores[0]}";
        scoreTextP2.text = $"P2: {scores[1]}";
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
    }

    void OnClickReplay()
    {
        AfterMatchPanel.SetActive(false);
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
                exitCountdownText.text = $"자동 나가기까지 {Mathf.CeilToInt(remaining)}초...";
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
}
