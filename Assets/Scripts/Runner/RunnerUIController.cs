using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RunnerUIController : MonoBehaviour
{
    public static RunnerUIController Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject gameplayHUD;
    public GameObject gameOverPanel;

    [Header("Gameplay HUD Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI biochipsText;
    public TextMeshProUGUI femaleHormoneText;
    public TextMeshProUGUI generalHormoneText;

    [Header("Speed Dial Elements")]
    public Image speedDialFillImage;
    public TextMeshProUGUI speedDialValueText;
    public float targetScoreMax = 10000f; // Score at which speed dial is at 100%

    [Header("Game Over Panel Elements")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalBiochipsText;
    public Button restartButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }

        // Make start panel click-to-start
        if (startPanel != null)
        {
            Button startBtn = startPanel.GetComponent<Button>();
            if (startBtn == null)
            {
                startBtn = startPanel.AddComponent<Button>();
            }
            startBtn.onClick.AddListener(OnStartButtonClicked);
        }

        // Initialize state
        UpdateUIState();
    }

    private void Update()
    {
        if (RunnerGameManager.Instance == null) return;

        UpdateUIState();

        if (RunnerGameManager.Instance.isPlaying && !RunnerGameManager.Instance.isGameOver)
        {
            UpdateGameplayHUD();
        }
    }

    private void UpdateUIState()
    {
        if (RunnerGameManager.Instance == null) return;

        bool isPlaying = RunnerGameManager.Instance.isPlaying;
        bool isGameOver = RunnerGameManager.Instance.isGameOver;

        // Show start panel if we haven't started playing yet
        if (startPanel != null)
        {
            startPanel.SetActive(!isPlaying && !isGameOver);
        }

        // Show HUD only during active play
        if (gameplayHUD != null)
        {
            gameplayHUD.SetActive(isPlaying && !isGameOver);
        }

        // Show game over panel when dead
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(isGameOver);
        }
    }

    private void UpdateGameplayHUD()
    {
        var manager = RunnerGameManager.Instance;

        // 1. Text Readouts
        if (scoreText != null)
            scoreText.text = $"SCORE: {Mathf.FloorToInt(manager.score)}";

        if (biochipsText != null)
            biochipsText.text = $"{manager.biochipsCollected}";

        if (femaleHormoneText != null)
            femaleHormoneText.text = $"{manager.femaleHormoneCollected}";

        if (generalHormoneText != null)
            generalHormoneText.text = $"{manager.generalHormoneCollected}";

        // 2. Dynamic Speed Dial Gauge
        // Fill amount increases as score goes up, wrapping/looping or clamping
        float scoreFraction = manager.score / targetScoreMax;
        float fillAmount = Mathf.Clamp01(scoreFraction);

        if (speedDialFillImage != null)
        {
            speedDialFillImage.fillAmount = fillAmount;
            
            // Dynamic color shift (Neon Cyan to Hot Red as speed/score maxes out)
            speedDialFillImage.color = Color.Lerp(Color.cyan, Color.red, fillAmount);
        }

        if (speedDialValueText != null)
        {
            // Speed indicator in MPH or standard Speed units
            float virtualSpeed = manager.currentSpeed * 10f; // e.g. 120 to 300
            speedDialValueText.text = $"{Mathf.FloorToInt(virtualSpeed)} km/h";
        }
    }

    public void ShowGameOver()
    {
        var manager = RunnerGameManager.Instance;
        if (manager == null) return;

        if (finalScoreText != null)
            finalScoreText.text = $"FINAL SCORE: {Mathf.FloorToInt(manager.score)}";

        if (finalBiochipsText != null)
            finalBiochipsText.text = $"Biochips Collected: {manager.biochipsCollected}";
    }

    private void OnRestartButtonClicked()
    {
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.StartGame();
        }
    }

    private void OnStartButtonClicked()
    {
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.StartGame();
        }
    }
}
