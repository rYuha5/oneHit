using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LobbyUIManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject roomButtonPrefab;
    public Transform roomListParent;
    public InputField roomNameInputField;
    public GameObject lobbyPanel;
    public GameObject connectingPanel;
    public TMP_InputField nicknameInputField;
    public GameObject nicknamePanel; // 닉네임 입력 UI 패널

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    void Start()
    {
        Screen.SetResolution(960, 540, false);
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;
        InitUI();
        ConnectToPhotonServer();
    }

    #region Connection & Lobby

    void InitUI()
    {
        connectingPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        nicknamePanel.SetActive(false);
    }

    void ConnectToPhotonServer()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Connecting to Photon...");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master");
        connectingPanel.SetActive(false);
        nicknamePanel.SetActive(true); // 닉네임 입력창 표시
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        lobbyPanel.SetActive(true);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Disconnected: " + cause);
        InitUI();
    }

    #endregion

    #region Room List

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform child in roomListParent)
        {
            Destroy(child.gameObject);
        }

        cachedRoomList.Clear();

        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList) continue;

            cachedRoomList[room.Name] = room;

            GameObject button = Instantiate(roomButtonPrefab, roomListParent);
            button.GetComponentInChildren<TMP_Text>().text = room.Name;
            button.GetComponent<Button>().onClick.AddListener(() => JoinRoom(room.Name));
        }
    }

    #endregion

    #region Set NickName
    public void OnClickSetNickname()
    {
        string inputName = nicknameInputField.text;

        if (!string.IsNullOrEmpty(inputName))
        {
            PhotonNetwork.LocalPlayer.NickName = inputName;
            nicknamePanel.SetActive(false);  // 닉네임 UI 닫고
            PhotonNetwork.JoinLobby();       // 로비 접속
        }
        else
        {
            Debug.LogWarning("닉네임이 비어 있습니다.");
        }
    }
    #endregion

    #region Room Join/Create

    public void CreateRoom()
    {
        string roomName = roomNameInputField.text;
        if (string.IsNullOrEmpty(roomName)) return;

        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.LoadLevel("GameScene");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Join Failed: " + message);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Create Room Failed: " + message);
    }

    #endregion
}
