using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelComplete : MonoBehaviour
{
    public int currentLevel;

    public void CompleteLevel()
    {
        int nextLevel = currentLevel + 1;

        if (nextLevel > PlayerPrefs.GetInt("UnlockedLevel", 1))
        {
            PlayerPrefs.SetInt("UnlockedLevel", nextLevel);
        }

        SceneManager.LoadScene("LevelSelection");
    }
}