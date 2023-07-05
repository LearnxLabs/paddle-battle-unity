using UnityEngine;

public class Topia : MonoBehaviour
{
    [SerializeField] private string playerNickname;
    public static Topia Instance { get; private set; }

    public string WorldName { get; set; } = "Topia";
    public string PlayerNickname { get; set; } = "Topi";
    public string VisitorId { get; set; } = "1";

    public PlayerType PlayerType { get; set; }
    public bool OnPrivateZone { get; set; } = false;

    private void Awake()
    {
        Instance = this;
        if (playerNickname.Length > 0)
        {
            SetPlayerNickname(playerNickname);
        }
        DontDestroyOnLoad(gameObject);
    }

    public void SetPlayerNickname(string nickname)
    {
        PlayerNickname = nickname;
    }

    public void SetVisitorId(string id)
    {
        VisitorId = id;
    } 

    public void SetWorldName(string matchId)
    {
        Debug.Log($"Setting world name to: {matchId}");
        WorldName = matchId;
    }

    public void SetPlayerType(PlayerType playerType)
    {
        PlayerType = playerType;
    }
}
