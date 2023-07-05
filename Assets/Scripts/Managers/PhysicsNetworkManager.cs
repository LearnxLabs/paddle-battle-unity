using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    [AddComponentMenu("")]
    public class PhysicsNetworkManager : NetworkManager
    {
        [Scene]
        public string gameScene;

        // This is set true after server loads all subscene instances
        bool subscenesLoaded;

        // subscenes are added to this list as they're loaded
        public readonly Dictionary<string, Scene> matchScenes = new Dictionary<string, Scene>();

        // Sequential index used in round-robin deployment of players into instances and score positioning
        int clientIndex;

        public static new PhysicsNetworkManager singleton { get; private set; }

        /// <summary>
        /// Runs on both Server and Client
        /// Networking is NOT initialized when this fires
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            singleton = this;
        }

        #region Server System Callbacks

        /// <summary>
        /// Called on the server when a client adds a new player with NetworkClient.AddPlayer.
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            StartCoroutine(OnServerAddPlayerDelayed(conn));
        }

        // This delay is mostly for the host player that loads too fast for the
        // server to have subscenes async loaded from OnStartServer ahead of it.
        IEnumerator OnServerAddPlayerDelayed(NetworkConnectionToClient conn)
        {
            // wait for server to async load all subscenes for game instances
            while (!subscenesLoaded)
                yield return null;

            // Send Scene message to client to additively load the game scene
            conn.Send(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.LoadAdditive });

            // Wait for end of frame before adding the player to ensure Scene Message goes first
            yield return new WaitForEndOfFrame();

            base.OnServerAddPlayer(conn);
            // Do this only on server, not on clients
            // This is what allows the NetworkSceneChecker on player and scene objects
            // to isolate matches per scene instance on server.

            clientIndex++;
        }

        #endregion

        #region Start & Stop Callbacks

        /// <summary>
        /// This is invoked when a server is started - including when a host is started.
        /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
        /// </summary>
        public override void OnStartServer()
        {
            NetworkServer.ReplaceHandler<AddPlayerMessage>(OnCreateCharacter);
        }

        /// <summary>
        /// This is called when a server is stopped - including when a host is stopped.
        /// </summary>
        public override void OnStopServer()
        {
            PaddleBattleManager.Instance.matches.Clear();
            NetworkServer.SendToAll(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.UnloadAdditive });
            StartCoroutine(ServerUnloadSubScenes());
            clientIndex = 0;
        }

        // Unload the subScenes and unused assets and clear the subScenes list.
        IEnumerator ServerUnloadSubScenes()
        {
            foreach (var matchScene in matchScenes)
            {
                if (matchScene.Value.IsValid())
                {
                    yield return SceneManager.UnloadSceneAsync(matchScene.Value);
                }
            }

            matchScenes.Clear();
            subscenesLoaded = false;

            yield return Resources.UnloadUnusedAssets();
        }

        // When a match is created and load a new scene
        public IEnumerator ServerLoadSubScene(string matchId, Action<Scene> callback)
        {
            Debug.Log("Loading scene: " + matchId);
            
            yield return SceneManager.LoadSceneAsync(gameScene, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
            Debug.Log("Scene loaded");
            Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            matchScenes.Add(matchId, scene);
            callback(scene);
        }

        // When a match is over unload the match scene
        public IEnumerator ServerUnloadSubScene(string matchId)
        {
            if (matchScenes[matchId] != null)
            {
                Scene matchScene = matchScenes[matchId];
                if (matchScene.IsValid())
                    yield return SceneManager.UnloadSceneAsync(matchScene);

                matchScenes.Remove(matchId);
                yield return Resources.UnloadUnusedAssets();
            }
        }

        /// <summary>
        /// This is called when a client is stopped.
        /// </summary>
        public override void OnStopClient()
        {
            // Make sure we're not in ServerOnly mode now after stopping host client
            if (mode == NetworkManagerMode.Offline)
                StartCoroutine(ClientUnloadSubScenes());
        }

        // Unload all but the active scene, which is the "container" scene
        IEnumerator ClientUnloadSubScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index) != SceneManager.GetActiveScene())
                    yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(index));
        }

        #endregion

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            NetworkClient.Send(new AddPlayerMessage());
        }

        public override void OnClientDisconnect()
        {
            GameObject.Find("LobbyUI").GetComponent<LobbyUI>().ShowButtons();
            base.OnClientDisconnect();
        }

        void OnCreateCharacter(NetworkConnectionToClient conn, AddPlayerMessage message)
        {
            GameObject gameobject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            NetworkServer.AddPlayerForConnection(conn, gameobject);
        }
    }
}
