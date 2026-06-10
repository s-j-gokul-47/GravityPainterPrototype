using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Put this on the goal tile (e.g. Tile 32). Requires a trigger collider on this object or a child
/// so the ball can enter. Assign a UI panel that is disabled until the level completes.
/// </summary>
public class FinishLine : MonoBehaviour
{
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private bool pauseGame = true;

    private bool _completed;

    private void Start()
    {
        EnsureFinishVisual();
    }

    private void EnsureFinishVisual()
    {
        FinishLineVisual visual = GetComponent<FinishLineVisual>();
        if (visual == null)
        {
            visual = gameObject.AddComponent<FinishLineVisual>();
        }

        visual.EnsureVisual();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_completed)
        {
            return;
        }

        if (other.GetComponentInParent<BallController>() == null)
        {
            return;
        }

        _completed = true;
        
        // Save the coins collected in this level to our total count!
        CoinManager.CommitSessionCoins();

        if (LevelProgress.IsProceduralScene(SceneManager.GetActiveScene()))
        {
            DifficultyManager.OnLevelCompleted();
        }
        else
        {
            LevelProgress.UnlockThrough(LevelProgress.GetActiveLevelNumber());
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        if (pauseGame)
        {
            Time.timeScale = 0f;
        }
    }

    /// <summary>Wire UI and pause behaviour after runtime placement.</summary>
    public void Configure(GameObject completePanel, bool pause = true)
    {
        levelCompletePanel = completePanel;
        pauseGame = pause;
    }

    /// <summary>Wire this to a UI Button "Restart" or "Play again".</summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Scene active = SceneManager.GetActiveScene();
        if (LevelProgress.IsProceduralScene(active))
        {
            ProceduralLevelBuilder builder = FindFirstObjectByType<ProceduralLevelBuilder>();
            if (builder != null)
            {
                _completed = false;
                if (levelCompletePanel != null)
                {
                    levelCompletePanel.SetActive(false);
                }

                builder.RebuildSameSeed();
                return;
            }
        }

        SceneManager.LoadScene(active.buildIndex);
    }

    /// <summary>Wire to a "Next" button if you add more scenes later.</summary>
    public void ResumeWithoutReload()
    {
        Time.timeScale = 1f;
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        _completed = false;
    }
}
