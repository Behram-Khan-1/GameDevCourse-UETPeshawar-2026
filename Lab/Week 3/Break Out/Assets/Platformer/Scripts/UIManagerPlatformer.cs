using UnityEngine;

public class UIManagerPlatformer : MonoBehaviour
{
    public static UIManagerPlatformer Instance;
    public GameObject gameOverPanel;
    void Start()
    {
        Instance = this;
    }

    public void ShowGameOverScreen()
    {
        Debug.Log("Game Over! Displaying Game Over Screen.");
        // Here you would typically enable a UI panel or load a game over scene
        gameOverPanel.SetActive(true);
    }
}
