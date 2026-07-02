using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

public class RunnerGameManager : MonoBehaviour
{
    public static RunnerGameManager Instance { get; private set; }
    public HealthMeterFinal healthMeterController;
    private const float HEALTH_BEGINNING_VALUE = 5f;
    private float healthScore = HEALTH_BEGINNING_VALUE;
    private float minHealthScore = 0f;
    private float maxHealthScore = 5f;
    [Header("Prefabs")]
    [Tooltip("The Running Ground prefab")]
    public GameObject groundPrefab;
    [Tooltip("The Future Tunnel prefab")]
    public GameObject tunnelPrefab;
    [Tooltip("Collectible prefabs: Biochip, Female Hormone, General Hormone")]
    public GameObject[] collectiblePrefabs;
    [Tooltip("Obstacle prefab: Virus")]
    public GameObject obstaclePrefab;
    public Connections Connections; // Reference to the Connection script
    public CharacterSlideshow characterSlideshow; // Reference to the CharacterSlideshow script
    [Header("Gameplay Settings")]


    public float initialSpeed = 12f;
    public float speedIncreaseRate = 1f;
    public float maxSpeed = 100f;
    public float currentSpeed;

    [Header("Spawning Positions")]
    public float laneWidth = 2.0f;
    public float spawnDistanceZ = 60f;
    public float groundTileLength = 4.72f;
    [Tooltip("How many ground tiles are laid ahead of the player. More tiles = ground extends (and new tiles spawn) farther into the distance. ~80 tiles reaches roughly 365 units ahead.")]
    public int initialGroundTiles = 80;
    [Tooltip("Distance the world must scroll before the next ground tile is spawned. Set equal to groundTileLength for seamless ground; set larger to create gaps between tiles.")]
    public float groundSpawnInterval = 4.72f;

    [Header("Tunnel Settings")]
    public float tunnelTileLength = 88.15f;
    public int initialTunnelTiles = 6;
    public float tunnelSpawnInterval = 88.15f;

    [Header("Obstacle & Collectible Spawning")]
    public float spawnInterval = 1.0f;
    [Tooltip("Chance (0 to 1) for a spawned object to float 1 meter above its base height.")]
    [Range(0f, 1f)]
    public float floatAboveChance = 0.5f;
    [Tooltip("Relative chance (0 to 1) of keeping a Biochip when selected. Lower values decrease spawn frequency.")]
    [Range(0f, 1f)]
    public float biochipSpawnChance = 0.4f;
    private float spawnTimer;

    [Header("Game State")]
    public bool isPlaying = false;
    public bool isCountingDown = false;
    public bool isGameOver = false;
    public TextMeshProUGUI countdownText;
    private Coroutine countdownCoroutine;
    public float score = 0f;
    public int biochipsCollected = 0;
    public int femaleHormoneCollected = 0;
    public int generalHormoneCollected = 0;
    public TextMeshProUGUI timerText;
    private float timeRemaining;
    private bool timerRunning = false;
    private float gameStartTime = 60f;

    [SerializeField] private GameObject redDot;

    [SerializeField] private GameObject displayObjeects;
    public enum TutorialAction
    {
        LaneSwitch,
        Jump,
        Slide,



    }

    [Header("Tutorial State")]
    public bool isTutorial { get; private set; } = false;
    public bool tutorialLaneSwitched { get; private set; } = false;
    public bool tutorialJumped { get; private set; } = false;
    public bool tutorialSlid { get; private set; } = false;
    private bool hasCompletedTutorialOnce = false;
    private bool tutorialCompleting = false;
    private float tutorialCompleteTimer = 0f;

    public void RegisterTutorialAction(TutorialAction action)
    {
        if (!isTutorial || isGameOver || tutorialCompleting) return;

        switch (action)
        {
            case TutorialAction.LaneSwitch:
                tutorialLaneSwitched = true;
                break;
            case TutorialAction.Jump:
                tutorialJumped = true;
                break;
            case TutorialAction.Slide:
                tutorialSlid = true;
                break;
        }

        if (tutorialLaneSwitched && tutorialJumped && tutorialSlid)
        {
            tutorialCompleteTimer = 15f;
            StartCoroutine(CompleteTutorialAfterDelay());
        }
    }
    private IEnumerator CompleteTutorialAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        tutorialCompleting = true;
    }
    // Object Pooling lists
    private List<GameObject> groundPool = new List<GameObject>();
    private List<GameObject> tunnelPool = new List<GameObject>();
    private List<GameObject> collectiblePool = new List<GameObject>();
    private List<GameObject> obstaclePool = new List<GameObject>();

    // Tracking active objects for scrolling and recycling
    private List<GameObject> activeGrounds = new List<GameObject>();
    private List<GameObject> activeTunnels = new List<GameObject>();
    private List<GameObject> activeCollectibles = new List<GameObject>();
    private List<GameObject> activeObstacles = new List<GameObject>();

    private float nextGroundZ;

    // Distance traveled since the last ground tile was spawned (drives interval spawning)
    private float groundDistanceAccumulator;
    private float tunnelDistanceAccumulator;

    [SerializeField] private GameObject playerPrefab; // Reference to the Player prefab


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
        if (redDot == null)
        {
            Debug.LogError("Red Dot reference is not set in the RunnerGameManager. Please assign it in the Inspector.");
        }
        //StartGame();
    }

    public void StartGame()
    {
        characterSlideshow.StopAndHide();
        if (!hasCompletedTutorialOnce)
        {
            StartTutorial();
        }
        else
        {
            StartActualGame();
        }
    }

    public void StartTutorial()
    {
        isTutorial = true;
        tutorialLaneSwitched = false;
        tutorialJumped = false;
        tutorialSlid = false;

        playerPrefab.SetActive(true); // Activate the player prefab
        ClearActiveObjects();
        currentSpeed = initialSpeed; // Start moving immediately at comfortable speed
        score = 0f;
        biochipsCollected = 0;
        femaleHormoneCollected = 0;
        generalHormoneCollected = 0;
        isPlaying = true; // Playing immediately
        isCountingDown = false;
        isGameOver = false;
        spawnTimer = 0f;
        groundDistanceAccumulator = 0f;
        tunnelDistanceAccumulator = 0f;

        // Initial ground generation
        nextGroundZ = -12f;
        float groundZ = nextGroundZ;
        for (int i = 0; i < initialGroundTiles; i++)
        {
            SpawnGroundTile(groundZ);
            groundZ += groundTileLength;
        }

        // Initial tunnel generation
        if (tunnelPrefab != null)
        {
            float tunnelZ = 50.4f - tunnelTileLength;
            for (int i = 0; i < initialTunnelTiles; i++)
            {
                SpawnTunnelTile(tunnelZ);
                tunnelZ += tunnelTileLength;
            }
        }

        // Reset player
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            foreach (Transform child in playerObj.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    PlayerController pc = child.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        pc.ResetPlayer();
                    }

                    // Trigger Start animation
                    Animator animator = child.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetTrigger("Start");
                    }
                    break;
                }
            }
        }
    }

    public void StartActualGame()
    {
        isTutorial = false;
        playerPrefab.SetActive(true); // Activate the player prefab when the game starts
        // Try to auto-find countdown text if not set
        if (countdownText == null)
        {
            var canvas = GameObject.Find("HUD Canvas");
            if (canvas != null)
            {
                var child = canvas.transform.Find("CountdownText");
                if (child != null)
                {
                    countdownText = child.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        // Clean up any existing active objects from previous run
        ClearActiveObjects();
        currentSpeed = 0f; // No movement during countdown
        score = 0f;
        biochipsCollected = 0;
        femaleHormoneCollected = 0;
        generalHormoneCollected = 0;
        isPlaying = false; // Not playing until countdown ends
        isCountingDown = true;
        isGameOver = false;
        spawnTimer = 0f;
        groundDistanceAccumulator = 0f;
        tunnelDistanceAccumulator = 0f;
        healthScore = HEALTH_BEGINNING_VALUE; // Reset health score to starting value
        // Initialize Spawning Z heights
        nextGroundZ = -12f; // Start ground tiles slightly behind the camera/player

        // Initial ground generation: lay tiles end-to-end starting behind the player
        float groundZ = nextGroundZ;
        for (int i = 0; i < initialGroundTiles; i++)
        {
            SpawnGroundTile(groundZ);
            groundZ += groundTileLength;
        }
        if (healthMeterController != null)
        {
            Debug.Log("made ith ere");
            healthMeterController.Activate();
            healthMeterController.SetValue(HEALTH_BEGINNING_VALUE);
        }

        // Initial tunnel generation: lay tunnel tiles starting from behind the player
        if (tunnelPrefab != null)
        {
            float tunnelZ = 50.4f - tunnelTileLength;
            for (int i = 0; i < initialTunnelTiles; i++)
            {
                SpawnTunnelTile(tunnelZ);
                tunnelZ += tunnelTileLength;
            }
        }

        // Pre-spawn some initial collectibles/obstacles closer to the player so they appear sooner
        PreSpawnInitialObjects();

        // Reset the active child player to starting/idle position
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            foreach (Transform child in playerObj.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    PlayerController pc = child.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        pc.ResetPlayer();
                    }
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning("Player GameObject not found in the scene.");
        }

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    private void PreSpawnInitialObjects()
    {
        // Pre-spawn initial collectibles/obstacles at 15m, 30m, and 45m ahead of player
        float[] initialZs = { 15f, 30f, 45f };
        foreach (float z in initialZs)
        {
            SpawnRandomLaneObject(z);
        }
    }

    private IEnumerator CountdownCoroutine()
    {
        isCountingDown = true;
        isPlaying = false;

        string[] countdownSteps = { "3", "2", "1", "GO!" };

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.color = Color.white;
        }

        float originalRedDotY = -0.09f;
        redDot.transform.position = new Vector3(redDot.transform.position.x, 0.1f, redDot.transform.position.z);
        foreach (string step in countdownSteps)
        {
            if (countdownText != null)
            {
                countdownText.text = step;

                // Pop animation effect: scale from 1.5 to 1.0
                float duration = 0.8f; // duration of each number
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / duration;

                    // Simple pop: scale starts at 1.5, rapidly goes to 1.0, then stays at 1.0
                    float scale = Mathf.Lerp(1.5f, 1.0f, Mathf.Min(1f, progress * 4f));
                    countdownText.transform.localScale = new Vector3(scale, scale, 1f);

                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }
        }
        redDot.transform.position = new Vector3(redDot.transform.position.x, originalRedDotY, redDot.transform.position.z);
        // Countdown complete!
        isCountingDown = false;
        isPlaying = true;
        currentSpeed = initialSpeed;

        // Trigger 'Start' animation on the active child player now that countdown is complete
        GameObject activePlayerObj = GameObject.Find("Player");
        if (activePlayerObj != null)
        {
            foreach (Transform child in activePlayerObj.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    Animator animator = child.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetTrigger("Start");
                        Debug.Log($"Triggered 'Start' on active child player {child.name} after countdown complete");
                    }
                    break;
                }
            }
        }

        StartTimer();

        // Keep "GO!" on screen for a short moment, then fade out
        if (countdownText != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                countdownText.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, elapsed / 0.6f));
                yield return null;
            }
            countdownText.text = "";
            countdownText.color = Color.white;
            countdownText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isPlaying || isGameOver) return;

        if (isTutorial)
        {
            currentSpeed = initialSpeed;
            score = 0f;

            if (tutorialCompleting)
            {
                displayObjeects.SetActive(true);
                playerPrefab.SetActive(false); // Hide the player during tutorial completion
                tutorialCompleteTimer -= Time.deltaTime;
                if (tutorialCompleteTimer <= 0f)
                {
                    SpawnRandomLaneObject();
                    displayObjeects.SetActive(false);
                    playerPrefab.SetActive(true); // Show the player again when starting the actual game
                    isTutorial = false;
                    hasCompletedTutorialOnce = true;
                    tutorialCompleting = false;
                    StartActualGame();
                    return;
                }
            }

            ScrollAndRecycle();
            return;
        }

        // Gradually increase game speed to increase difficulty
        currentSpeed = Mathf.Min(maxSpeed, currentSpeed + speedIncreaseRate * Time.deltaTime);

        // Increase score based on distance run
        score += currentSpeed * Time.deltaTime;

        // Spawn collectibles and obstacles periodically
        spawnTimer += Time.deltaTime;
        spawnInterval = Mathf.Min(1f, 0.3f + ((maxSpeed - currentSpeed) / maxSpeed) * (7 / 6)); // Adjust spawn interval based on speed
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnRandomLaneObject();
        }
        Debug.Log($"Current Speed: {currentSpeed}, Score: {score}, Health: {healthScore}");
        // Scroll active objects and recycle them when they pass behind the player
        ScrollAndRecycle();
    }

    private void ScrollAndRecycle()
    {
        float scrollStep = currentSpeed * Time.deltaTime;

        // 1. Scroll Ground; return tiles to the pool once they pass behind the camera
        for (int i = activeGrounds.Count - 1; i >= 0; i--)
        {
            GameObject ground = activeGrounds[i];
            ground.transform.Translate(Vector3.back * scrollStep, Space.World);

            // If ground segment has passed completely behind camera (Z < -15)
            if (ground.transform.position.z < -15f)
            {
                activeGrounds.RemoveAt(i);
                ground.SetActive(false);
                groundPool.Add(ground);
            }
        }

        // Spawn new ground tiles at fixed distance intervals as the world scrolls
        groundDistanceAccumulator += scrollStep;
        while (groundDistanceAccumulator >= groundSpawnInterval)
        {
            groundDistanceAccumulator -= groundSpawnInterval;
            SpawnGroundTileAhead();
        }

        // --- SCROLL TUNNELS ---
        if (tunnelPrefab != null)
        {
            for (int i = activeTunnels.Count - 1; i >= 0; i--)
            {
                GameObject tunnel = activeTunnels[i];
                tunnel.transform.Translate(Vector3.back * scrollStep, Space.World);

                // If tunnel segment has passed completely behind camera (Z < -25)
                if (tunnel.transform.position.z < -25f)
                {
                    activeTunnels.RemoveAt(i);
                    tunnel.SetActive(false);
                    tunnelPool.Add(tunnel);
                }
            }

            // Spawn new tunnel tiles at fixed distance intervals as the world scrolls
            tunnelDistanceAccumulator += scrollStep;
            while (tunnelDistanceAccumulator >= tunnelSpawnInterval)
            {
                tunnelDistanceAccumulator -= tunnelSpawnInterval;
                SpawnTunnelTileAhead();
            }
        }

        // 2. Scroll and recycle Collectibles
        for (int i = activeCollectibles.Count - 1; i >= 0; i--)
        {
            GameObject coll = activeCollectibles[i];
            coll.transform.Translate(Vector3.back * scrollStep, Space.World);

            if (coll.transform.position.z < -12f) // Behind the player
            {
                activeCollectibles.RemoveAt(i);
                coll.SetActive(false);
                collectiblePool.Add(coll);
            }
        }

        // 3. Scroll and recycle Obstacles
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obs = activeObstacles[i];
            obs.transform.Translate(Vector3.back * scrollStep, Space.World);

            if (obs.transform.position.z < -12f) // Behind the player
            {
                activeObstacles.RemoveAt(i);
                obs.SetActive(false);
                obstaclePool.Add(obs);
            }
        }
    }

    private void SpawnTunnelTile(float zPosition)
    {
        GameObject tunnel;
        if (tunnelPool.Count > 0)
        {
            tunnel = tunnelPool[0];
            tunnelPool.RemoveAt(0);
            tunnel.SetActive(true);
        }
        else
        {
            tunnel = Instantiate(tunnelPrefab, transform);
            tunnel.transform.rotation = Quaternion.identity;
            tunnel.transform.localScale = new Vector3(3f, 3f, 3f);
        }

        tunnel.transform.position = new Vector3(0f, -0.1f, zPosition);
        activeTunnels.Add(tunnel);
    }

    private void SpawnTunnelTileAhead()
    {
        float spawnZ;
        if (activeTunnels.Count == 0)
        {
            spawnZ = 50.4f - tunnelTileLength;
        }
        else
        {
            float frontmostZ = float.NegativeInfinity;
            foreach (var t in activeTunnels)
            {
                if (t.transform.position.z > frontmostZ)
                {
                    frontmostZ = t.transform.position.z;
                }
            }
            spawnZ = frontmostZ + tunnelTileLength;
        }

        SpawnTunnelTile(spawnZ);
    }

    private void SpawnGroundTile(float zPosition)
    {
        GameObject ground;
        if (groundPool.Count > 0)
        {
            ground = groundPool[0];
            groundPool.RemoveAt(0);
            ground.SetActive(true);
        }
        else
        {
            ground = Instantiate(groundPrefab, transform);
            // Enforce correct scale/rotation recorded from scene
            ground.transform.rotation = Quaternion.Euler(270f, 90f, 0f);
        }

        ground.transform.position = new Vector3(0f, 0f, zPosition);
        activeGrounds.Add(ground);
    }

    // Spawns the next ground tile directly ahead of the current front-most tile,
    // keeping the ground seamless regardless of how far the world has scrolled.
    private void SpawnGroundTileAhead()
    {
        float spawnZ;
        if (activeGrounds.Count == 0)
        {
            spawnZ = nextGroundZ;
        }
        else
        {
            float frontmostZ = float.NegativeInfinity;
            foreach (var g in activeGrounds)
            {
                if (g.transform.position.z > frontmostZ)
                {
                    frontmostZ = g.transform.position.z;
                }
            }
            spawnZ = frontmostZ + groundTileLength;
        }

        SpawnGroundTile(spawnZ);
    }

    private void SpawnRandomLaneObject(float zPos = -1f)
    {
        // Select a random lane (-1 for Left, 0 for Center, 1 for Right)
        int laneIndex = Random.Range(-1, 2);
        float targetX = laneIndex * laneWidth;

        // Choose whether to spawn obstacle or collectible (e.g. 40% obstacle, 60% collectible)
        if (Random.value < 0.4f)
        {
            SpawnObstacle(targetX, zPos);
        }
        else
        {
            SpawnCollectible(targetX, zPos);
        }
    }

    private void SpawnObstacle(float xPos, float zPos = -1f)
    {
        if (obstaclePrefab == null) return;

        GameObject obs = null;
        if (obstaclePool.Count > 0)
        {
            obs = obstaclePool[0];
            obstaclePool.RemoveAt(0);
            obs.SetActive(true);
        }
        else
        {
            obs = Instantiate(obstaclePrefab, transform);
            obs.transform.rotation = Quaternion.Euler(270.02f, 0f, 0f);
            obs.transform.localScale = new Vector3(35f, 35f, 35f);

            // Add sphere collider for triggering hits dynamically
            SphereCollider sc = obs.GetComponent<SphereCollider>();
            if (sc == null) sc = obs.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.015f; // Scales nicely with 35x scale to fit the sphere visual

            // Add Obstacle gameplay component
            if (obs.GetComponent<Obstacle>() == null)
            {
                obs.AddComponent<Obstacle>();
            }
        }

        // Virus Y placement should sit at obstacle height
        float targetY = 0.8f;
        if (Random.value < floatAboveChance)
        {
            targetY += 1.0f;
        }
        float finalZ = zPos < 0f ? spawnDistanceZ : zPos;
        obs.transform.position = new Vector3(xPos, targetY, finalZ);
        activeObstacles.Add(obs);
    }

    private void SpawnCollectible(float xPos, float zPos = -1f)
    {
        if (collectiblePrefabs == null || collectiblePrefabs.Length == 0) return;

        // Hormone kits (General and Female) are disabled from spawning
        List<GameObject> spawnablePrefabs = new List<GameObject>();
        foreach (var prefab in collectiblePrefabs)
        {
            if (prefab != null && !prefab.name.Contains("Hormone Kit"))
            {
                spawnablePrefabs.Add(prefab);
            }
        }

        if (spawnablePrefabs.Count == 0) return;

        GameObject coll = null;
        int index = Random.Range(0, spawnablePrefabs.Count);
        GameObject chosenPrefab = spawnablePrefabs[index];

        // Decrease spawning frequency of the Biochip prefab
        if (chosenPrefab != null && chosenPrefab.name.Contains("Biochip"))
        {
            if (Random.value > biochipSpawnChance)
            {
                List<GameObject> alternatives = new List<GameObject>();
                foreach (var prefab in spawnablePrefabs)
                {
                    if (prefab != null && !prefab.name.Contains("Biochip"))
                    {
                        alternatives.Add(prefab);
                    }
                }

                if (alternatives.Count > 0)
                {
                    chosenPrefab = alternatives[Random.Range(0, alternatives.Count)];
                }
            }
        }

        // Retrieve from pool of same prefab name
        for (int i = 0; i < collectiblePool.Count; i++)
        {
            if (collectiblePool[i].name.StartsWith(chosenPrefab.name))
            {
                coll = collectiblePool[i];
                collectiblePool.RemoveAt(i);
                coll.SetActive(true);
                break;
            }
        }

        if (coll == null)
        {
            coll = Instantiate(chosenPrefab, transform);

            // Apply correct prefab properties
            if (chosenPrefab.name.Contains("Biochip"))
            {
                coll.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
                coll.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

                BoxCollider bc = coll.GetComponent<BoxCollider>();
                if (bc == null) bc = coll.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(0.5f, 0.5f, 0.5f);
            }
            else // Hormone Kits
            {
                coll.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                coll.transform.localScale = new Vector3(300f, 300f, 300f);

                BoxCollider bc = coll.GetComponent<BoxCollider>();
                if (bc == null) bc = coll.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(0.001f, 0.001f, 0.001f); // Fits the 300x scale visual
            }

            // Add Collectible component
            if (coll.GetComponent<Collectible>() == null)
            {
                coll.AddComponent<Collectible>();
            }
        }

        // Place collectible slightly floating
        float spawnY = chosenPrefab.name.Contains("Biochip") ? 0.6f : 0.4f;
        if (Random.value < floatAboveChance)
        {
            spawnY += 2.0f;
        }
        float finalZ = zPos < 0f ? spawnDistanceZ : zPos;
        coll.transform.position = new Vector3(xPos, spawnY, finalZ);
        activeCollectibles.Add(coll);
    }

    public void OnCollectibleCollected(Collectible collectible)
    {
        string name = collectible.gameObject.name;

        if (name.Contains("Biochip"))
        {
            healthScore = Mathf.Clamp(healthScore + 1f, minHealthScore, maxHealthScore);

            biochipsCollected++;
            score += 100f;
        }
        else if (name.Contains("Female Hormone Kit"))
        {
            femaleHormoneCollected++;
            score += 250f;
        }
        else if (name.Contains("General Hormone Kit"))
        {
            generalHormoneCollected++;
            score += 250f;
        }

        // Remove from active and return to pool
        if (activeCollectibles.Contains(collectible.gameObject))
        {
            activeCollectibles.Remove(collectible.gameObject);
            collectible.gameObject.SetActive(false);
            collectiblePool.Add(collectible.gameObject);
        }
        healthMeterController.SetValue(healthScore);
    }

    public void OnObstacleHit()
    {
        // Trigger Game Over
        healthScore = Mathf.Clamp(healthScore - 1f, minHealthScore, maxHealthScore);
        healthMeterController.SetValue(healthScore);
        if (healthScore <= 0f)
        {
            EndGame();
        }
        currentSpeed = initialSpeed; // Reset speed to initial value on hit
    }

    private void ClearActiveObjects()
    {
        foreach (var obj in activeGrounds)
        {
            obj.SetActive(false);
            groundPool.Add(obj);
        }
        activeGrounds.Clear();

        foreach (var obj in activeTunnels)
        {
            obj.SetActive(false);
            tunnelPool.Add(obj);
        }
        activeTunnels.Clear();

        foreach (var obj in activeCollectibles)
        {
            obj.SetActive(false);
            collectiblePool.Add(obj);
        }
        activeCollectibles.Clear();

        foreach (var obj in activeObstacles)
        {
            obj.SetActive(false);
            obstaclePool.Add(obj);
        }
        activeObstacles.Clear();
    }
    public void StartTimer()
    {
        timeRemaining = gameStartTime; // Reset timer to 15 seconds at the start of the game
        timerRunning = true;
        StartCoroutine(UpdateTimerDisplay());
    }

    private IEnumerator UpdateTimerDisplay()
    {
        while (timerRunning)
        {
            UpdateDisplay();

            yield return new WaitForSeconds(1f);

            timeRemaining -= 1f;
            // Debug.Log("Time remaining: " + timeRemaining);
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                UpdateDisplay();
                timerRunning = false;
                OnTimerEnd();
            }
        }
    }

    private void UpdateDisplay()
    {
        timerText.text = "Time Left: " + Mathf.CeilToInt(timeRemaining).ToString();
    }

    private void OnTimerEnd()
    {
        Debug.Log("Time's up!");
        EndGame();
        // Add your game-over logic here
    }
    public void EndGame(bool save = true)
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(false);
        }
        playerPrefab.SetActive(false); // Deactivate the player prefab when the game ends
        isCountingDown = false;
        isGameOver = true;
        isPlaying = false;
        currentSpeed = 0f;
        timeRemaining = gameStartTime; // Reset timer for next game
        timerRunning = false;
        if (RunnerUIController.Instance != null)
        {
            RunnerUIController.Instance.ShowGameOver();
            redDot.transform.position = new Vector3(redDot.transform.position.x, 0.04f, redDot.transform.position.z);
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                foreach (Transform child in playerObj.transform)
                {
                    if (child.gameObject.activeSelf)
                    {
                        PlayerController pc = child.GetComponent<PlayerController>();
                        if (pc != null)
                        {
                            pc.ResetPlayer();
                        }

                        // Trigger Start animation
                        Animator animator = child.GetComponent<Animator>();
                        if (animator != null)
                        {
                            animator.SetTrigger("Restart");
                        }
                        child.transform.Rotate(0, 180, 0);
                        break;
                    }
                }
            }
            //playerPrefab.SetActive(false); // Deactivate the player prefab when the game ends
        }
        if (healthMeterController != null)
        {
            healthMeterController.Deactivate();
        }
        if (save)
        {
            Connections.SendWebSocketMessage(score);
        }
        characterSlideshow.ShowAndStart();
    }
    public void SlideshowBegin()
    {
        if (characterSlideshow != null)
        {
            characterSlideshow.ShowAndStart();
        }
    }
    public void SlideshowEnd()
    {
        if (characterSlideshow != null)
        {
            characterSlideshow.StopAndHide();
        }
    }
}

