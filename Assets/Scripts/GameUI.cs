using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    public TextMeshProUGUI p1Name;
    public TextMeshProUGUI p1Score;
    public TextMeshProUGUI p2Name;
    public TextMeshProUGUI p2Score;

    public TextMeshProUGUI winnerNickname;

    public Animator animator;

    public void SetPlayerName(int index, string name)
    {
        if (index == 0)
        {
            p1Name.text = name;
        }
        else
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
        else
        {
            p2Score.text = "" + score;
        }
    }

    public void ShowWinnerUI(string winner)
    {
        winnerNickname.text = winner;
        animator.Play("Winner");
    }
}
