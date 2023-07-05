
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public TextMeshProUGUI p1Name;
    public TextMeshProUGUI p1Score;
    public TextMeshProUGUI p2Name;
    public TextMeshProUGUI p2Score;

    public TextMeshProUGUI winnerNickname;
    public Button menuButton;

    public Animator animator;
    public void SetPlayerName(int index, string name)
    {
        if (index == 0)
        {
            p1Name.text = name;
        }
        if (index == 1)
        {
            p2Name.text = name;
        }
    }

    public void SetScore(int index, int score)
    {
        if (index == 0)
        {
            p1Score.text = "" + score;
        }
        if (index == 1)
        {
            p2Score.text = "" + score;
        }
    }

    public void CountDown()
    {
        animator.Play("Countdown");
    }

    public void ShowWinnerUI(string winner)
    {
        winnerNickname.text = $"{winner} won!";
        animator.Play("Winner");
    }

    public void ResetUI()
    {
        animator.Play("Default");
    }
}

// TODO:
// 1. Reset Spectator UI when the match finishes
// 2. Fix issue when the match finish and users don't see the main menu UI
// 3. Add persistent Leaderboard