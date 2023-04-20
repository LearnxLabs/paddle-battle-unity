using Mirror;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button PlayButton;
    [SerializeField] private Button SpectateButton;
    [SerializeField] private GameObject LobbyAnimations;
    [SerializeField] private GameObject GameUI;

    [SerializeField] private TextMeshPro PlayersText;
    [SerializeField] private TextMeshPro SpectatorsText;

    private void Start()
    {

        PlayButton.onClick.AddListener(() =>
        {
            Topia.Instance.SetPlayerType(PlayerType.PLAYER);
            NetworkManager.singleton.StartClient();
        });

        SpectateButton.onClick.AddListener(() =>
        {
            Topia.Instance.SetPlayerType(PlayerType.SPECTATOR);
            NetworkManager.singleton.StartClient();
        });
    }

    public void HideButtons()
    {
        PlayButton.gameObject.SetActive(false);
        //SpectateButton.gameObject.SetActive(false);
        LobbyAnimations.SetActive(false);
        GameUI.SetActive(true);
    }

    public void ShowButtons()
    {
        PlayButton.gameObject.SetActive(true);
        //SpectateButton.gameObject.SetActive(true);
        LobbyAnimations.SetActive(true);
        GameUI.SetActive(false);
    }
}
