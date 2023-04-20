using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using Mirror.Examples.MultipleAdditiveScenes;

public class Player : NetworkBehaviour
{
    public static Player LocalInstance { get; set; }

    // Defaults
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject paddle;
    [SerializeField] private GameObject nicknameCanvas;
    [SerializeField] private GameObject scoreCanvas;
    [SerializeField] private GameObject winnerCanvas;
    [SerializeField] private GameObject countdownCanvas;
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Button menuButton;

    [SerializeField] private Vector3 initialPos;
    [SerializeField] private Vector3 initialRot;

    [SerializeField] private Vector2 posSpeed;
    [SerializeField] private Vector3 rotSpeed;

    [SyncVar(hook = nameof(OnNicknameChanged))]
    public string nickname;

    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score = 0;

    [SyncVar(hook = nameof(OnIndexChanged))]
    public int index;

    [SyncVar]
    public string topiaId;

    [SyncVar(hook = nameof(OnWorldNameChanged))]
    public string matchId;

    [SyncVar]
    public uint connectionId;

    [SyncVar]
    public PlayerType type;

    private GameUI GameUI;

    public void Awake()
    {
        LocalInstance = this;
        GameObject.Find("LobbyUI").GetComponent<LobbyUI>().HideButtons();
    }

    public override void OnStartClient()
    {
        if (type == PlayerType.PLAYER)
        {
            paddle.SetActive(true);
        }
        if (!isLocalPlayer) return;
        CmdCreateOrJoinMatch(Topia.Instance.WorldName);
        CmdSetUpPlayer(Topia.Instance.PlayerNickname, Topia.Instance.PlayerType, Topia.Instance.WorldName, Topia.Instance.PlayerId, netId);

        if (Topia.Instance.PlayerType != PlayerType.SPECTATOR)
        {
            paddle.SetActive(true);
            if (isLocalPlayer)
            {
                countdownCanvas.SetActive(true);
                cam.enabled = true;
                scoreText.enabled = true;
            }
        }

        GetComponent<AudioSource>().Play();
        menuButton.onClick.AddListener(() => {
            StartCoroutine(Disconnect(0));
        });
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        PaddleBattleManager.Instance.RemovePlayer(this);
    }

    private void Update()
    {
        if (!isLocalPlayer || type != PlayerType.PLAYER) return;
        UpdatePaddle();

        if (Input.GetKeyDown(KeyCode.X))
        {
            CmdLogInServer("Local: " + connectionId.ToString());
            CmdLogInServer("Server: " + connectionToServer.connectionId.ToString());
        }
    }

    private void UpdatePaddle()
    {
        Vector2 screenPos = cam.ScreenToViewportPoint(Input.mousePosition);
        Vector3 mousePos = new Vector3((Mathf.Clamp(screenPos.x, 0, 1) - 0.5f), (Mathf.Clamp(screenPos.y, 0, 1) - 0.5f), 0f);

        paddle.transform.localPosition = initialPos + new Vector3(mousePos.x * posSpeed.x, mousePos.y * posSpeed.y, 0);
        paddle.transform.localRotation = Quaternion.Euler(initialRot + new Vector3(mousePos.y * rotSpeed.x - 10, mousePos.x * rotSpeed.y, mousePos.x * rotSpeed.z));
    }

    [ClientRpc]
    public void ShowWinnerUI(string _nickname, string _topiaId)
    {
        CmdLogInServer("Showing Winner UI");
        if (!isLocalPlayer) return;
        if (nickname == _nickname && topiaId == _topiaId)
        {
            winnerText.text = "You won!";
        }
        else
        {
            winnerText.text = $"{_nickname} won!";
        }
        winnerCanvas.GetComponent<Animator>().Play("PlayerWinnerUI");
        Disconnect(5);
    }

    [ClientRpc]
    public void StartCountdown()
    {
        if (!isLocalPlayer) return;
        countdownCanvas.GetComponent<Animator>().Play("PlayerCountdown");
    }

    [Command]
    public void CmdLogInServer(string msg)
    {
        Debug.Log(msg);
    }

    [Command]
    public void CmdSetUpPlayer(string _nickname, PlayerType _type, string _matchId, string _topiaId, uint _connectionId)
    {
        nickname = _nickname;
        matchId = _matchId;
        topiaId = _topiaId;
        connectionId = _connectionId;
        index = PaddleBattleManager.Instance.GetPlayerCount(_matchId);
        type = _type;

        if (_type == PlayerType.PLAYER)
        {
            paddle.SetActive(true);
            if (index > 1)
            {
                type = PlayerType.SPECTATOR;
                cam.enabled = false;
                paddle.SetActive(false);
            }
        }

        if (_type == PlayerType.SPECTATOR)
        {
            cam.enabled = false;
            paddle.SetActive(false);
        }

        Debug.Log(
            " --- Setting New Player ---" + "\n" +
            $"Nickname: {nickname}" + "\n" +
            $"Type: {type}" + "\n" +
            $"Match: {matchId}" + "\n" +
            $"Index: {index}" + "\n" +
            $"ConnectionID: {connectionId}"
        );

        PaddleBattleManager.Instance.AddPlayer(this);
    }

    [Command]
    public void CmdRemovePlayer()
    {
        PaddleBattleManager.Instance.RemovePlayer(this);
    }

    [Server]
    public void AddPoint()
    {
        Debug.Log($"Point added to {nickname}");
        score++;
    }

    void OnNicknameChanged(string oldNickname, string newNickname)
    {
        Debug.Log("Nickname: " + newNickname);
        nicknameText.text = newNickname + "\n" + score;
        GameUI = GameObject.Find("GameUI").GetComponent<GameUI>();
        GameUI.SetPlayerName(index, nickname);
    }

    void OnWorldNameChanged(string _old, string _new)
    {
        Debug.Log("Worldname: " + _new);
    }

    void OnTypeChanged(PlayerType _oldType, PlayerType _newType)
    {
        Debug.Log("Worldname: " + _newType);
        if (_newType == PlayerType.SPECTATOR)
        {
            cam.enabled = false;
            paddle.SetActive(false);

        }
    }

    void OnScoreChanged(int oldScore, int newScore)
    {
        Debug.Log("Score: " + newScore);
        string[] array = new string[newScore];
        Array.Fill(array, ".");
        // scoreText.text = String.Join("", array);
        scoreText.text = "" + newScore;
        nicknameText.text = nickname + "\n" + score;
        GameUI.SetScore(index, newScore);
    }

    void OnIndexChanged(int oldIndex, int newIndex)
    {
        Debug.Log("Player Index: " + newIndex);
        CmdLogInServer("Player Index: " + newIndex);
        if (newIndex == 1)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    [Command]
    public void CmdCreateOrJoinMatch(string matchId)
    {
        bool matchExist = PhysicsNetworkManager.singleton.matchScenes.ContainsKey(matchId);
        if (matchExist)
        {
            SceneManager.MoveGameObjectToScene(gameObject, PhysicsNetworkManager.singleton.matchScenes[matchId]);
        }
        else
        {
            PaddleBattleManager.Instance.CreateMatchAndAddPlayer(matchId, gameObject);
        }
    }

    public IEnumerator Disconnect(int delay)
    {
        CmdRemovePlayer();
        yield return new WaitForSeconds(delay);
        GameObject.Find("LobbyUI").GetComponent<LobbyUI>().ShowButtons();
        CmdLogInServer("Disconnecting player " + nickname);
        NetworkClient.Disconnect();
    }
}