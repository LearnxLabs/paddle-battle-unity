using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{


    public enum Scene
    {
        LobbyScene,
        GameScene,
    }

    public static void LoadNetwork(int targetScene)
    {
        SceneManager.LoadScene(targetScene);
    }
}