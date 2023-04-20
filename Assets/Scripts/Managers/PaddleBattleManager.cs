using System;
using System.Collections;
using UnityEngine;
using Mirror;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.ComponentModel;
using Mirror.Examples.MultipleAdditiveScenes;

public enum PlayerType
{
    PLAYER,
    SPECTATOR
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
    public Ball ball;
    // public List<Spectator>() Spectators;

    public Match(string id)
    {
        this.id = id;
        this.status = GameStatus.WAITING_FOR_PLAYERS;
        this.players = new List<Player>();
    }

    public Match() { }

    public void StartMatch()
    {
        status = GameStatus.PLAYING;
        GetPlayers().ForEach(p => p.StartCountdown());
    }

    public void FinishMatch(Player player)
    {
        players.ForEach(p =>
        {
            p.ShowWinnerUI(player.nickname, player.topiaId);
        });
        status = GameStatus.FINISHED;
    }

    public void AddPlayerToMatch(Player player)
    {
        players.Add(player);
        if (players.FindAll(x => x.type == PlayerType.PLAYER).Count == 2)
        {
            PaddleBattleManager.Instance.StartMatch(this);
        }
    }

    public void RemovePlayerFromMatch(Player player)
    {
        Debug.Log("Removing player from Match: " + player.nickname);
        players.Remove(player);
        Debug.Log("Players in match: " + players.FindAll(x => x.type == PlayerType.PLAYER).Count);
        Debug.Log("Spectators in match: " + players.FindAll(x => x.type == PlayerType.SPECTATOR).Count);

        if (player.type == PlayerType.SPECTATOR) return;

        if (status == GameStatus.PLAYING && player.type == PlayerType.PLAYER && GetPlayers().Count > 0)
        {
            Player remainingPlayer = players.Find(x => x.type == PlayerType.PLAYER);
            if (remainingPlayer)
            {
                PaddleBattleManager.Instance.FinishMatch(remainingPlayer, true);
            }
        }
    }

    public List<Player> GetPlayers()
    {
        return players.FindAll(p => p.type == PlayerType.PLAYER);
    }

    public List<Player> GetSpectators()
    {
        return players.FindAll(p => p.type == PlayerType.SPECTATOR);
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

    public void ShowWinnerUI(Player player)
    {
        GameObject.Find("GameUI").GetComponent<GameUI>().ShowWinnerUI(player.nickname);
        Match match = matches.Find(x => x.id == player.matchId);
        match.players.ForEach(player => player.ShowWinnerUI(player.nickname, player.topiaId));
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
    public void RemovePlayer(Player player)
    {
        Match match = matches.Find(x => x.id == player.matchId);
        if (match != null)
        {
            match.RemovePlayerFromMatch(player);
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
}