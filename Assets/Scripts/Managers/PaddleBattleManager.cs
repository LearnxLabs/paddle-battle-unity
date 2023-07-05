using System;
using System.Collections;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Mirror.Examples.MultipleAdditiveScenes;

public enum PlayerType
{
    SPECTATOR,
    PLAYER
}

public enum GameStatus
{
    WAITING_FOR_PLAYERS,
    READY_TO_START,
    PLAYING,
    FINISHED
}

public class Match
{
    public string id;
    public GameStatus status;
    public List<Player> players;
    public List<Player> spectators;
    public Ball ball;
    // public List<Spectator>() Spectators;

    public Match(string id)
    {
        this.id = id;
        this.status = GameStatus.WAITING_FOR_PLAYERS;
        this.players = new List<Player>();
        this.spectators = new List<Player>();
    }

    public Match() { }

    public void StartMatch()
    {
        status = GameStatus.PLAYING;
        players.ForEach(p => p.StartCountdown());
        spectators.ForEach(s => s.StartCountdown());
    }

    public void FinishMatch(Player player)
    {
        status = GameStatus.FINISHED;
    }

    public void AddPlayerToMatch(Player player)
    {
        players.Add(player);
        if (players.Count == 2)
        {
            PaddleBattleManager.Instance.StartMatch(this);
        }
    }

    public void RemovePlayerFromMatch(Player player)
    {
        Debug.Log("Removing player from Match: " + player.nickname);
        players.Remove(player);
        Debug.Log("Players in match: " + players.FindAll(x => x.type == PlayerType.PLAYER).Count);

        if (player.type == PlayerType.SPECTATOR) return;

        if ((status == GameStatus.PLAYING || status == GameStatus.READY_TO_START)  && player.type == PlayerType.PLAYER && GetPlayers().Count > 0)
        {
            Player remainingPlayer = players.Find(x => x.type == PlayerType.PLAYER);
            if (remainingPlayer)
            {
                PaddleBattleManager.Instance.FinishMatch(remainingPlayer, true);
            }
        }
    }

    public void AddSpectatorToMatch(Player spectator)
    {
        spectators.Add(spectator);
    }

    public void RemoveSpectatorFromMatch(Player spectator)
    {
        spectators.Remove(spectator);
        Debug.Log("Spectators in match: " + spectators.FindAll(x => x.type == PlayerType.SPECTATOR).Count);
    }

    public List<Player> GetPlayers()
    {
        return players.FindAll(p => p.type == PlayerType.PLAYER);
    }
}

public class PaddleBattleManager : NetworkBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    public static PaddleBattleManager Instance { get; private set; }

    public int ScoreToWin = 5;

    public readonly SyncList<Match> matches = new SyncList<Match>();

    private void Awake()
    {
        Instance = this;
    }

    public void CreateMatchAndAddPlayer(string matchId, GameObject gameObject)
    {
        StartCoroutine(PhysicsNetworkManager.singleton.ServerLoadSubScene(matchId, (Scene scene) =>
        {
            GameObject.Find("MatchName").name = matchId;
            SceneManager.MoveGameObjectToScene(gameObject, scene);
        }));
    }

    public void StartMatch(Match match)
    {
        Debug.Log("Starting Match: " + match.id);
        match.StartMatch();
        StartCoroutine(Countdown(match));
    }

    [Server]
    public void FinishMatch(Player player, bool useDelay = true)
    {
        Debug.Log($"Match finished, {player.nickname} won!");
        Match match = matches.Find(x => x.id == player.matchId);
        match.FinishMatch(player);
        StartCoroutine(DeleteMatch(match, useDelay ? 10 : 0));
    }

    public IEnumerator DeleteMatch(Match match, int delay)
    {
        /// Wait X seconds to delete the match and unload the scene
        yield return new WaitForSeconds(delay);
        matches.Remove(match);
        StartCoroutine(PhysicsNetworkManager.singleton.ServerUnloadSubScene(match.id));
    }

    public IEnumerator Countdown(Match match)
    {
        yield return new WaitForSeconds(3);
        match.status = GameStatus.PLAYING;
        StartCoroutine(SpawnBall(match.id, 1));
    }

    public IEnumerator SpawnBall(string matchId, int direction)
    {
        yield return new WaitForSeconds(2);
        Debug.Log("Spawning BALL");
        GameObject ball = Instantiate(ballPrefab, new Vector3(0, 3F, 0), Quaternion.identity);
        ball.GetComponent<Ball>().AddVelocity(direction);
        NetworkServer.Spawn(ball);
        SceneManager.MoveGameObjectToScene(ball, PhysicsNetworkManager.singleton.matchScenes[matchId]);
    }

    public void AddPoint(Player player, bool bounced)
    {
        if (bounced)
        {
            player.AddPoint();
            if (player.score >= ScoreToWin)
            {
                FinishMatch(player);
                ShowWinnerUI(player);
                return;
            }
            int direction = player.index == 1 ? 1 : -1;
            Match match = matches.Find(x => x.id == player.matchId);
            if (match.status == GameStatus.PLAYING) {
                StartCoroutine(SpawnBall(player.matchId, direction));
            }
        }
        else
        {
            Player oponent = GetPlayer(player.matchId, player.index * - 1 + 1);
            oponent.AddPoint();
            if (oponent.score >= ScoreToWin)
            {
                FinishMatch(oponent);
                ShowWinnerUI(oponent);
                return;
            }
            int direction = oponent.index == 1 ? 1 : -1;
            Match match = matches.Find(x => x.id == player.matchId);
            if (match.status == GameStatus.PLAYING)
            {
                StartCoroutine(SpawnBall(oponent.matchId, direction));
            }
        }
    }

    public void ShowWinnerUI(Player winner)
    {
        Match match = matches.Find(x => x.id == winner.matchId);
        match.players.ForEach(p => p.ShowWinnerUI(winner.nickname, winner.topiaId));
        match.spectators.ForEach(s => s.ShowWinnerUI(winner.nickname, winner.topiaId));
    }

    [Server]
    public void AddPlayer(Player player)
    {
        Match match = matches.Find(x => x.id == player.matchId);
        if (match != null)
        {
            match.AddPlayerToMatch(player);
            Debug.Log("Adding player to existing match");
        }
        else
        {
            Match newMatch = new Match(player.matchId);
            newMatch.AddPlayerToMatch(player);
            matches.Add(newMatch);
            Debug.Log("Creating new match and adding player to it");
        }
    }

    [Server]
    public void AddSpectator(Player spectator)
    {
        Match match = matches.Find(x => x.id == spectator.matchId);
        if (match != null)
        {
            match.AddSpectatorToMatch(spectator);
            Debug.Log("Adding spectator to existing match");
        }
        else
        {
            Match newMatch = new Match(spectator.matchId);
            newMatch.AddSpectatorToMatch(spectator);
            matches.Add(newMatch);
            Debug.Log("Creating new match and adding spectator to it");
        }
    }

    [Server]
    public void RemovePlayer(Player player)
    {
        Match match = matches.Find(x => x.id == player.matchId);
        if (match != null)
        {
            match.RemovePlayerFromMatch(player);
        }
    }

    [Server]
    public void RemoveSpectator(Player spectator)
    {
        Match match = matches.Find(x => x.id == spectator.matchId);
        if (match != null)
        {
            match.RemoveSpectatorFromMatch(spectator);
        }
    }

    [Server]
    public void RemovePlayerWithConnectionId(int connectionId)
    {
        Debug.Log("Removing player with connection id: " + connectionId);
        foreach (var _match in matches)
        {
            Player playerToRemove = _match.players.Find(x => x.connectionId == connectionId);

            foreach(Player player in _match.players)
            {
                Debug.Log(player.nickname + " :: " + player.connectionId);
            }
            if (playerToRemove)
            {
                _match.RemovePlayerFromMatch(playerToRemove);
            }
            else
            {
                Debug.Log("No players found to remove");
            }
        }
    }

    public Player GetPlayer(string matchId, int playerIndex)
    {
        return matches.Find(x => x.id == matchId).players.Find(x => x.index == playerIndex);
    }

    public List<Player> GetPlayersConnected(string matchId) {
        return matches.Find(x => x.id == matchId).players;
    }

    public int GetPlayerCount(string matchId)
    {
        Match match = matches.Find(x => x.id == matchId);
        if (match != null)
        {
            Debug.Log("Players in match: " + matches.Find(x => x.id == matchId).players.Count);
            return matches.Find(x => x.id == matchId).players.Count;
        }
        else return 0;
    }

    [Server]
    public void PlayerNameChanged(int index, string nickname, string matchId)
    {
        Match match = matches.Find(x => x.id == matchId);
        if (match != null)
        {
            match.spectators.ForEach(s => s.UpdateSpectatorUINickname(index, nickname));
        }
    }

    [Server]
    public void PlayerScoreChanged(int index, int score, string matchId)
    {
        Match match = matches.Find(x => x.id == matchId);
        if (match != null)
        {
            match.spectators.ForEach(s => s.UpdateSpectatorUIScore(index, score));
        }
    }

    [Command]
    public void CmdLogInServer(string msg)
    {
        Debug.Log(msg);
    }
}