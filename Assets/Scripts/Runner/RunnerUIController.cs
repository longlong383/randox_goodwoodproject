using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

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
    public Image healthMeterContainer;
    [Header("Speed Dial Elements")]
    public Image speedDialFillImage;
    public TextMeshProUGUI speedDialValueText;
    public float targetScoreMax = 10000f;

    [Header("Game Over Panel Elements")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalBiochipsText;
    public Button restartButton;
    // Input System
    private RunnerUIActions inputActions;

    [Header("Tutorial Elements")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        inputActions = new RunnerUIActions();
    }

    private void OnEnable()
    {
        // Enable both action maps
        inputActions.UI.Enable();
        // inputActions.Gameplay.Enable();

        // Subscribe to Start action (spacebar on start/gameover screens)
        inputActions.UI.Start.performed += OnStartPressed;
    }

    private void OnDisable()
    {
        inputActions.UI.Start.performed -= OnStartPressed;
        inputActions.UI.Disable();
        inputActions.Gameplay.Disable();
    }

    private void Start()
    {
        if (restartButton != null)
        {
            Debug.Log("restarting button adding");
            restartButton.onClick.AddListener(OnRestartButtonClicked);
            Debug.Log("restarting button added");
        }
        if (startPanel != null)
        {
            Button startBtn = startPanel.GetComponent<Button>();
            if (startBtn == null)
                startBtn = startPanel.AddComponent<Button>();
            startBtn.onClick.AddListener(OnStartButtonClicked);
        }

        SetAllPanelsOff();
        if (startPanel != null) startPanel.SetActive(true);
    }

    private void UpdateTutorialUI()
    {
        if (RunnerGameManager.Instance == null || tutorialText == null) return;

        var manager = RunnerGameManager.Instance;

        string laneStatus = manager.tutorialLaneSwitched ? "<color=green> Done</color>" : "<color=yellow> Practice</color>";
        string jumpStatus = manager.tutorialJumped ? "<color=green> Done</color>" : "<color=yellow> Practice</color>";
        string slideStatus = manager.tutorialSlid ? "<color=green> Done</color>" : "<color=yellow> Practice</color>";

        if (manager.tutorialLaneSwitched && manager.tutorialJumped && manager.tutorialSlid)
        {
            tutorialText.text = $"TUTORIAL COMPLETED!</b></color></size>\n\n" +
                                $"<color=green>Great job! Get ready for the actual game...</color>";
        }
        else
        {
            tutorialText.text = $"TUTORIAL MODE</b></color></size>\n" +
                                $"Learn the basic movement controls:</size>\n\n" +
                                $"Switch Lanes (A/D or Arrows):</b> {laneStatus}\n" +
                                $"Jump (Space):</b> {jumpStatus}\n" +
                                $"Slide (C):</b> {slideStatus}";
        }
    }

    private void Update()
    {
        if (RunnerGameManager.Instance == null) return;

        UpdateUIState();

        if ((RunnerGameManager.Instance.isPlaying || RunnerGameManager.Instance.isCountingDown) && !RunnerGameManager.Instance.isGameOver)
        {
            if (RunnerGameManager.Instance.isTutorial)
            {
                UpdateTutorialUI();
            }
            else
            {
                UpdateGameplayHUD();
            }
        }
    }

    // Called by the Input System when Space is pressed
    private void OnStartPressed(InputAction.CallbackContext context)
    {
        if (RunnerGameManager.Instance == null) return;

        bool isPlaying = RunnerGameManager.Instance.isPlaying;
        bool isGameOver = RunnerGameManager.Instance.isGameOver;
        bool isCountingDown = RunnerGameManager.Instance.isCountingDown;

        // Space works on the start screen AND the game over screen, but not during countdown
        if ((!isPlaying && !isCountingDown) || isGameOver)
        {

            RunnerGameManager.Instance.StartGame();

        }
    }

    private void SetAllPanelsOff()
    {
        if (startPanel != null) startPanel.SetActive(false);
        if (gameplayHUD != null) gameplayHUD.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void UpdateUIState()
    {
        if (RunnerGameManager.Instance == null)
        {
            SetAllPanelsOff();
            if (startPanel != null) startPanel.SetActive(true);
            return;
        }

        bool isPlaying = RunnerGameManager.Instance.isPlaying;
        bool isGameOver = RunnerGameManager.Instance.isGameOver;
        bool isCountingDown = RunnerGameManager.Instance.isCountingDown;
        bool isTutorial = RunnerGameManager.Instance.isTutorial;

        if (startPanel != null) startPanel.SetActive(!isPlaying && !isGameOver && !isCountingDown);
        
        if (gameplayHUD != null)
        {
            gameplayHUD.SetActive((isPlaying || isCountingDown) && !isGameOver);
            
            if (gameplayHUD.activeSelf)
            {
                if (tutorialPanel != null)
                {
                    tutorialPanel.SetActive(isTutorial);
                }

                Transform scorePanelTrans = gameplayHUD.transform.Find("ScorePanel");
                if (scorePanelTrans != null)
                {
                    scorePanelTrans.gameObject.SetActive(!isTutorial);
                }

                Transform timerTextTrans = gameplayHUD.transform.Find("TimerText");
                if (timerTextTrans != null)
                {
                    timerTextTrans.gameObject.SetActive(!isTutorial);
                }
            }
        }
        
        if (gameOverPanel != null) gameOverPanel.SetActive(isGameOver);
    }

    private void UpdateGameplayHUD()
    {
        var manager = RunnerGameManager.Instance;

        if (scoreText != null)
            scoreText.text = $"SCORE: {Mathf.FloorToInt(manager.score)}";

        if (biochipsText != null)
            biochipsText.text = $"{manager.biochipsCollected}";





        float scoreFraction = manager.score / targetScoreMax;
        float fillAmount = Mathf.Clamp01(scoreFraction);

        if (speedDialFillImage != null)
        {
            speedDialFillImage.fillAmount = fillAmount;
            speedDialFillImage.color = Color.Lerp(Color.cyan, Color.red, fillAmount);
        }

        if (speedDialValueText != null)
        {
            float virtualSpeed = manager.currentSpeed * 10f;
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
        Debug.Log("handled!");
        RunnerGameManager.Instance?.StartGame();
    }

    private void OnStartButtonClicked()
    {
        RunnerGameManager.Instance?.StartGame();
    }

}

