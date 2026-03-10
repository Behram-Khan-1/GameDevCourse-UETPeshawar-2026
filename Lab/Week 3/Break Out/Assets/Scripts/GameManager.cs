using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{

    public int score = 0;
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    public void AddScore(int value)
    {
        score += value;
        scoreText.text = "Score: " + score;
    }


    public void GameOver()
    {
        gameOverPanel.SetActive(true);
        finalScoreText.text = "Final Score: " + score;
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
