using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManagerPlatformer : MonoBehaviour
{
    public static GameManagerPlatformer Instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void LoadNextLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("No more levels to load!");
        }
    }

    public void GameOver()
    {
        Debug.Log("Game Over! Restarting level...");
        // Here you would typically show a game over screen and offer options to restart or go to the main menu
        UIManagerPlatformer.Instance.ShowGameOverScreen();
    }


}
