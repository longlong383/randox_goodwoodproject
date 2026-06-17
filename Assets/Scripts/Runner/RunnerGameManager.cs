using System.Collections.Generic;
using UnityEngine;

public class RunnerGameManager : MonoBehaviour
{
    public static RunnerGameManager Instance { get; private set; }

    [Header("Prefabs")]
    [Tooltip("The Running Ground prefab")]
    public GameObject groundPrefab;
    [Tooltip("Building prefabs: Building 1, Building 2, Building 4")]
    public GameObject[] buildingPrefabs;
    [Tooltip("Collectible prefabs: Biochip, Female Hormone, General Hormone")]
    public GameObject[] collectiblePrefabs;
    [Tooltip("Obstacle prefab: Virus")]
    public GameObject obstaclePrefab;

    [Header("Gameplay Settings")]
    public float initialSpeed = 12f;
    public float speedIncreaseRate = 0.1f;
    public float maxSpeed = 30f;
    public float currentSpeed;

    [Header("Spawning Positions")]
    public float laneWidth = 2.0f;
    public float spawnDistanceZ = 60f;
    public float groundTileLength = 4.72f;
    public int initialGroundTiles = 30;

    [Header("Building Settings")]
    public float leftBuildingX = -12f;
    public float rightBuildingX = 12f;
    public float buildingSpawnDistanceZ = 80f;

    [Header("Obstacle & Collectible Spawning")]
    public float spawnInterval = 1.0f;
    private float spawnTimer;

    [Header("Game State")]
    public bool isPlaying = false;
    public bool isGameOver = false;
    public float score = 0f;
    public int biochipsCollected = 0;
    public int femaleHormoneCollected = 0;
    public int generalHormoneCollected = 0;

    // Object Pooling lists
    private List<GameObject> groundPool = new List<GameObject>();
    private List<GameObject> buildingPool = new List<GameObject>();
    private List<GameObject> collectiblePool = new List<GameObject>();
    private List<GameObject> obstaclePool = new List<GameObject>();

    // Tracking active objects for scrolling and recycling
    private List<GameObject> activeGrounds = new List<GameObject>();
    private List<GameObject> activeBuildings = new List<GameObject>();
    private List<GameObject> activeCollectibles = new List<GameObject>();
    private List<GameObject> activeObstacles = new List<GameObject>();

    private float nextGroundZ;
    private float nextLeftBuildingZ;
    private float nextRightBuildingZ;

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
        StartGame();
    }

    public void StartGame()
    {
        // Clean up any existing active objects from previous run
        ClearActiveObjects();

        currentSpeed = initialSpeed;
        score = 0f;
        biochipsCollected = 0;
        femaleHormoneCollected = 0;
        generalHormoneCollected = 0;
        isPlaying = true;
        isGameOver = false;
        spawnTimer = 0f;

        // Initialize Spawning Z heights
        nextGroundZ = -12f; // Start ground tiles slightly behind the camera/player
        nextLeftBuildingZ = -20f;
        nextRightBuildingZ = -20f;

        // Initial ground generation
        for (int i = 0; i < initialGroundTiles; i++)
        {
            SpawnGroundTile();
        }

        // Initial buildings generation to fill up the sides
        for (int i = 0; i < 8; i++)
        {
            SpawnBuildingSide(true);
            SpawnBuildingSide(false);
        }
    }

    private void Update()
    {
        if (!isPlaying || isGameOver) return;

        // Gradually increase game speed to increase difficulty
        currentSpeed = Mathf.Min(maxSpeed, currentSpeed + speedIncreaseRate * Time.deltaTime);

        // Increase score based on distance run
        score += currentSpeed * Time.deltaTime;

        // Spawn collectibles and obstacles periodically
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnRandomLaneObject();
        }

        // Scroll active objects and recycle them when they pass behind the player
        ScrollAndRecycle();
    }

    private void ScrollAndRecycle()
    {
        float scrollStep = currentSpeed * Time.deltaTime;

        // 1. Scroll and recycle Ground
        for (int i = activeGrounds.Count - 1; i >= 0; i--)
        {
            GameObject ground = activeGrounds[i];
            ground.transform.Translate(Vector3.back * scrollStep, Space.World);

            // If ground segment has passed completely behind camera (Z < -15)
            if (ground.transform.position.z < -15f)
            {
                activeGrounds.RemoveAt(i);
                // Recycle to front
                ground.transform.position = new Vector3(0f, 0f, nextGroundZ);
                activeGrounds.Add(ground);
                nextGroundZ += groundTileLength;
            }
        }

        // 2. Scroll and recycle Buildings
        for (int i = activeBuildings.Count - 1; i >= 0; i--)
        {
            GameObject bld = activeBuildings[i];
            bld.transform.Translate(Vector3.back * scrollStep, Space.World);

            if (bld.transform.position.z < -30f)
            {
                activeBuildings.RemoveAt(i);
                bld.SetActive(false);
                buildingPool.Add(bld);

                // Spawn a new building on its respective side
                bool isLeft = bld.transform.position.x < 0;
                SpawnBuildingSide(isLeft);
            }
        }

        // 3. Scroll and recycle Collectibles
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

        // 4. Scroll and recycle Obstacles
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

    private void SpawnGroundTile()
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

        ground.transform.position = new Vector3(0f, 0f, nextGroundZ);
        activeGrounds.Add(ground);
        nextGroundZ += groundTileLength;
    }

    private void SpawnBuildingSide(bool isLeft)
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;

        float targetX = isLeft ? leftBuildingX : rightBuildingX;
        float refZ = isLeft ? nextLeftBuildingZ : nextRightBuildingZ;

        GameObject bld = null;
        int prefabIndex = Random.Range(0, buildingPrefabs.Length);
        GameObject chosenPrefab = buildingPrefabs[prefabIndex];

        // Try to retrieve from pool of same prefab name
        for (int i = 0; i < buildingPool.Count; i++)
        {
            if (buildingPool[i].name.StartsWith(chosenPrefab.name))
            {
                bld = buildingPool[i];
                buildingPool.RemoveAt(i);
                bld.SetActive(true);
                break;
            }
        }

        if (bld == null)
        {
            bld = Instantiate(chosenPrefab, transform);
            // Apply original rotations and scales based on prefab type
            if (chosenPrefab.name.Contains("Building 1"))
            {
                bld.transform.rotation = Quaternion.Euler(270f, 0f, 0f);
                bld.transform.localScale = new Vector3(3500f, 3500f, 3500f);
            }
            else if (chosenPrefab.name.Contains("Building 2"))
            {
                bld.transform.rotation = Quaternion.Euler(270f, 0f, 0f);
                bld.transform.localScale = new Vector3(2500f, 2500f, 2500f);
            }
            else if (chosenPrefab.name.Contains("Building 4"))
            {
                bld.transform.rotation = Quaternion.Euler(270f, 90f, 0f);
                bld.transform.localScale = new Vector3(6000f, 6000f, 6000f);
            }
        }

        // Set building position. Height Y = 0.
        bld.transform.position = new Vector3(targetX, 0f, refZ);
        if (chosenPrefab.name.Contains("Building 2"))
        {
            bld.transform.position = new Vector3(targetX, 8f, refZ);
        }
        activeBuildings.Add(bld);

        // Approximate length of buildings to offset the next spawn Z
        float buildingLength = 20f; // Default spacing
        if (chosenPrefab.name.Contains("Building 1")) buildingLength = 15f;
        else if (chosenPrefab.name.Contains("Building 2")) buildingLength = 20f;
        else if (chosenPrefab.name.Contains("Building 4")) buildingLength = 10f;

        if (isLeft) nextLeftBuildingZ += buildingLength;
        else nextRightBuildingZ += buildingLength;
    }

    private void SpawnRandomLaneObject()
    {
        // Select a random lane (-1 for Left, 0 for Center, 1 for Right)
        int laneIndex = Random.Range(-1, 2);
        float targetX = laneIndex * laneWidth;

        // Choose whether to spawn obstacle or collectible (e.g. 40% obstacle, 60% collectible)
        if (Random.value < 0.4f)
        {
            SpawnObstacle(targetX);
        }
        else
        {
            SpawnCollectible(targetX);
        }
    }

    private void SpawnObstacle(float xPos)
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
        obs.transform.position = new Vector3(xPos, 0.8f, spawnDistanceZ);
        activeObstacles.Add(obs);
    }

    private void SpawnCollectible(float xPos)
    {
        if (collectiblePrefabs == null || collectiblePrefabs.Length == 0) return;

        GameObject coll = null;
        int index = Random.Range(0, collectiblePrefabs.Length);
        GameObject chosenPrefab = collectiblePrefabs[index];

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
                coll.transform.rotation = Quaternion.Euler(270f, 90f, 0f);
                coll.transform.localScale = Vector3.one;

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
        coll.transform.position = new Vector3(xPos, spawnY, spawnDistanceZ);
        activeCollectibles.Add(coll);
    }

    public void OnCollectibleCollected(Collectible collectible)
    {
        string name = collectible.gameObject.name;

        if (name.Contains("Biochip"))
        {
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
    }

    public void OnObstacleHit()
    {
        // Trigger Game Over
        isGameOver = true;
        isPlaying = false;
        currentSpeed = 0f;
    }

    private void ClearActiveObjects()
    {
        foreach (var obj in activeGrounds)
        {
            obj.SetActive(false);
            groundPool.Add(obj);
        }
        activeGrounds.Clear();

        foreach (var obj in activeBuildings)
        {
            obj.SetActive(false);
            buildingPool.Add(obj);
        }
        activeBuildings.Clear();

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

    // Standard high-quality IMGUI HUD for 100% reliable out-of-the-box user feedback
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;

        // 1. Score display
        GUI.Label(new Rect(20, 20, 400, 40), $"Score: {Mathf.FloorToInt(score)}", style);

        // 2. Collectibles list
        GUIStyle colStyle = new GUIStyle(style);
        colStyle.fontSize = 18;
        colStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(20, 60, 400, 30), $"Biochips: {biochipsCollected}", colStyle);
        
        colStyle.normal.textColor = Color.magenta;
        GUI.Label(new Rect(20, 90, 400, 30), $"Female Hormone Kits: {femaleHormoneCollected}", colStyle);
        
        colStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(20, 120, 400, 30), $"General Hormone Kits: {generalHormoneCollected}", colStyle);

        // 3. Game Over window
        if (isGameOver)
        {
            // Dim background
            Texture2D blackTexture = new Texture2D(1, 1);
            blackTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.6f));
            blackTexture.Apply();
            GUI.skin.box.normal.background = blackTexture;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

            // Window
            Rect windowRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 120, 300, 240);
            GUI.Box(windowRect, "GAME OVER", GUI.skin.box);

            GUIStyle titleStyle = new GUIStyle(style);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(windowRect.x, windowRect.y + 20, windowRect.width, 40), "GAME OVER", titleStyle);

            GUIStyle resultStyle = new Rect(windowRect.x, windowRect.y + 70, windowRect.width, 30).Contains(Event.current.mousePosition) ? colStyle : colStyle;
            resultStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(windowRect.x, windowRect.y + 70, windowRect.width, 30), $"Final Score: {Mathf.FloorToInt(score)}", resultStyle);
            GUI.Label(new Rect(windowRect.x, windowRect.y + 100, windowRect.width, 30), $"Biochips collected: {biochipsCollected}", resultStyle);

            if (GUI.Button(new Rect(windowRect.x + 50, windowRect.y + 160, 200, 45), "TAP TO RESTART"))
            {
                StartGame();
            }
        }
    }
}
