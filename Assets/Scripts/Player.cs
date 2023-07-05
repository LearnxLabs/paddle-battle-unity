using System.Collections;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirror.Examples.MultipleAdditiveScenes;
using System.Runtime.InteropServices;

public class Player : NetworkBehaviour
{
    public static Player LocalInstance { get; set; }

    // Defaults
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject paddle;
    [SerializeField] private GameObject nicknameCanvas;
    [SerializeField] private GameObject scoreCanvas;
    [SerializeField] private GameObject winnerCanvas;
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject PointZones;
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

    [DllImport("__Internal")]
    private static extern void GameOver(string winner);

    public void Awake()
    {
        LocalInstance = this;
        GameObject.Find("LobbyUI").GetComponent<LobbyUI>().HideButtons();
        GameObject.Find("GameUI").GetComponent<GameUI>().menuButton.onClick.AddListener(() => {
            StartCoroutine(Disconnect(0));
        });
    }

    public override void OnStartClient()
    {
        if (type == PlayerType.PLAYER)
        {
            paddle.SetActive(true);

            GameObject.Find("GameUI").GetComponent<GameUI>().SetPlayerName(index, nickname);
            GameObject.Find("GameUI").GetComponent<GameUI>().SetScore(index, score);

            if (isLocalPlayer)
            {
                GameObject.Find("GameUI").SetActive(false);
            }
        }

        if (type == PlayerType.SPECTATOR)
        {
            paddle.SetActive(false);
            paddle.GetComponent<BoxCollider>().enabled = false;
            cam.enabled = false;
            UI.SetActive(false);
            PointZones.SetActive(false);
        }

        if (!isLocalPlayer) return;
        CmdCreateOrJoinMatch(Topia.Instance.WorldName);
        CmdSetUpPlayer(Topia.Instance.PlayerNickname, Topia.Instance.PlayerType, Topia.Instance.WorldName, Topia.Instance.VisitorId, netId);

        GetComponent<AudioSource>().Play();
        menuButton.onClick.AddListener(() => {
            StartCoroutine(Disconnect(0));
        });
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (type == PlayerType.PLAYER)
        {
            PaddleBattleManager.Instance.RemovePlayer(this);
        }
        else
        {
            PaddleBattleManager.Instance.RemoveSpectator(this);
        }
    }

    private void Update()
    {
        if (!isLocalPlayer || type != PlayerType.PLAYER) return;
        UpdatePaddle();
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
        if (!isLocalPlayer) return;
        Debug.Log(nickname + " # " + _nickname);
        if (nickname == _nickname && topiaId == _topiaId)
        {
            winnerText.text = "You won!";
            Debug.Log(_nickname);
        }
        else
        {
            winnerText.text = $"{_nickname} won!";
        }

#if UNITY_WEBGL == true && UNITY_EDITOR == false
        GameOver(_nickname);
#endif

        if (type == PlayerType.PLAYER)
        {
            winnerCanvas.GetComponent<Animator>().Play("PlayerWinnerUI");
        }
        else
        {
            GameObject.Find("GameUI").GetComponent<GameUI>().ShowWinnerUI(_nickname);
        }
    }

    [ClientRpc]
    public void StartCountdown()
    {
        if (!isLocalPlayer) return;
        if (type == PlayerType.PLAYER)
        {
            countdownCanvas.SetActive(true);
            countdownCanvas.GetComponent<Animator>().Play("PlayerCountdown");
        }
        else
        {
            GameObject.Find("GameUI").GetComponent<GameUI>().CountDown();
        }
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
        type = PaddleBattleManager.Instance.GetPlayerCount(_matchId) == 2 ? PlayerType.SPECTATOR : _type;
        index = type == PlayerType.SPECTATOR ? 3 : PaddleBattleManager.Instance.GetPlayerCount(_matchId);

        cam.enabled = false;

        RpcConfigurePlayer(type);

        Debug.Log(
            " --- Setting New Player ---" + "\n" +
            $"Nickname: {nickname}" + "\n" +
            $"Type: {type}" + "\n" +
            $"Match: {matchId}" + "\n" +
            $"Index: {index}" + "\n" +
            $"ConnectionID: {connectionId}"
        );

        if (type == PlayerType.PLAYER)
        {
            paddle.SetActive(true);

            PaddleBattleManager.Instance.AddPlayer(this);
            PaddleBattleManager.Instance.PlayerNameChanged(index, nickname, matchId);
        }
        else
        {
            paddle.SetActive(false);
            paddle.GetComponent<BoxCollider>().enabled = false;
            UI.SetActive(false);
            PointZones.SetActive(false);

            PaddleBattleManager.Instance.AddSpectator(this);
        }
    }

    [ClientRpc]
    public void RpcConfigurePlayer(PlayerType _type)
    {
        if (_type == PlayerType.PLAYER)
        {
            paddle.SetActive(true);
            if (isLocalPlayer)
            {
                cam.enabled = true;
                countdownCanvas.SetActive(true);
                scoreText.enabled = true;
                UI.SetActive(true);
                PointZones.SetActive(true);
            }
        }
        else
        {
            paddle.SetActive(false);
            paddle.GetComponent<BoxCollider>().enabled = false;
            cam.enabled = false;
            UI.SetActive(false);
            PointZones.SetActive(false);
            CmdLogInServer("Reseting Spectator UI");
            GameObject.Find("GameUI").GetComponent<GameUI>().ResetUI();
            GameObject.Find("GameUI").GetComponent<GameUI>().menuButton.onClick.AddListener(() => {
                StartCoroutine(Disconnect(0));
            });
        }
    }

    [Command]
    public void CmdRemovePlayer()
    {
        if (type == PlayerType.PLAYER)
        {
            PaddleBattleManager.Instance.RemovePlayer(this);
        }
        else
        {
            PaddleBattleManager.Instance.RemoveSpectator(this);
        }
    }

    [Command]
    public void CmdSetPlayerType(PlayerType _type)
    {
        type = _type;
    }

    [Server]
    public void AddPoint()
    {
        Debug.Log($"Point added to {nickname}");
        score++;
        PaddleBattleManager.Instance.PlayerScoreChanged(index, score, matchId);
    }

    void OnNicknameChanged(string oldNickname, string newNickname)
    {
        Debug.Log("Nickname: " + newNickname);
        nicknameText.text = newNickname + "\n" + score;
    }

    void OnWorldNameChanged(string _old, string _new)
    {
        Debug.Log("Worldname: " + _new);
    }

    void OnScoreChanged(int oldScore, int newScore)
    {
        scoreText.text = "" + newScore;
        nicknameText.text = nickname + "\n" + score;
    }

    void OnIndexChanged(int oldIndex, int newIndex)
    {
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
        GameObject.Find("LobbyUI").GetComponent<LobbyUI>().ShowButtons();
        yield return new WaitForSeconds(delay);
        CmdLogInServer("Disconnecting player " + nickname);
        NetworkClient.Disconnect();
    }

    // REGION Spectator specific methods

    [ClientRpc]
    public void UpdateSpectatorUINickname(int _index, string _nickname)
    {
        if (!isLocalPlayer) return;
        CmdLogInServer("Setting player NICKNAME in Spectator UI");
        GameObject.Find("GameUI").GetComponent<GameUI>().SetPlayerName(_index, _nickname);
    }

    [ClientRpc]
    public void UpdateSpectatorUIScore(int _index, int _score)
    {
        if (!isLocalPlayer) return;
        CmdLogInServer("Setting player SCORE in Spectator UI");
        GameObject.Find("GameUI").GetComponent<GameUI>().SetScore(_index, _score);
    }
}