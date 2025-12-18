using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string initialLevel = "MiningGameTest";
    [SerializeField] private GameObject loadingScreenCanvas; //we can add loading screen 

    private string currentActiveScene;

    private void Start()
    {
        LoadLevel(initialLevel);
    }

    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadLevelRoutine(sceneName));
    }

    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        // 1. Show Loading Screen 
        if (loadingScreenCanvas != null) loadingScreenCanvas.SetActive(true);

        // 2. Unload current level if one exists
        if (!string.IsNullOrEmpty(currentActiveScene))
        {
            yield return SceneManager.UnloadSceneAsync(currentActiveScene);
        }

        // 3. Load new level ADDITIVELY
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!loadOp.isDone)
        {
            yield return null;
        }

        // 4. Set Active Scene
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loadedScene);
        currentActiveScene = sceneName;

        // 5. Hide Loading Screen
        if (loadingScreenCanvas != null) loadingScreenCanvas.SetActive(false);
        
        Debug.Log($"SceneLoader: Transitioned to {sceneName}");
    }
}