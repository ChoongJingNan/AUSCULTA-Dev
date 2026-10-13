using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuBackgroundLoader : MonoBehaviour
{
    [Header("Default Fallback Map")]
    [Tooltip("If the player has no save file, this map will display in the background")]
    public string defaultScene = "R1_BackroomsBuild";

    private void Start()
    {
        // 1. Get the last saved scene from PlayerPrefs
        string sceneToLoad = PlayerPrefs.GetString("LastSavedScene", defaultScene);

        // 2. Load the scene in the background asynchronously
        StartCoroutine(LoadSceneInBackground(sceneToLoad));
    }

    private IEnumerator LoadSceneInBackground(string sceneName)
    {
        // Load map additively behind the UI camera
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. Disable gameplay elements (Player controller, HUD canvas, Audio) from the background scene
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid())
        {
            foreach (GameObject rootObj in loadedScene.GetRootGameObjects())
            {
                // Deactivate the player object so it doesn't move or take input
                if (rootObj.CompareTag("Player") || rootObj.GetComponentInChildren<Canvas>() != null)
                {
                    rootObj.SetActive(false);
                }
            }
        }
    }
}