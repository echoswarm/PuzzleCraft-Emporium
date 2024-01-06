using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

// Enums and classes for different control modes and grid object specifications
[System.Serializable]
public enum ControlMode
{
    Swipe,
    Buttons
}

[System.Serializable]
public class GridObject
{
    public GameObject prefab;
    public int x;
    public int y;
    public int rarity;
}

[System.Serializable]
public class PrefabRarity
{
    public GameObject prefab;
    public int rarity;
}

public class GameSetupManager : MonoBehaviour
{
    // Singleton pattern implementation
    public static GameSetupManager Instance { get; private set; }

    // Public fields for various settings and configurations
    [Header("General Settings")]
    public string touchControlButtonsName = "TouchControlButtons";
    public static ControlMode CurrentControlMode = ControlMode.Swipe;

    [Header("Managers")]
    public CoinCombinerAndClearer coinCombinerAndClearer;
    public GridManager gridManager;

    [Header("Grid Settings")]
    public List<GridObject> gridObjects;
    public int coinRowCount = 10;
    public int coinColumnCount = 5;
    public List<PrefabRarity> prefabRarities;

    [Header("Gameplay Settings")]
    [SerializeField] public int numberOfObjects = 30;
    [SerializeField] private string gameType;
    [SerializeField] public float spawnInterval = 3f;
    public bool AreCoinsMoving { get; private set; } = false;
    public static bool IsPlayerShotCompleted = true;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI gameTypeText;
    [SerializeField] private Button menuButton;

    private float spawnTimer;
    private bool isLocked = false;

    // Initialization and singleton pattern enforcement
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Initial settings configuration
    private void Start()
    {
        InitializeSettings();
    }

    // Update game logic in each frame
    private void Update()
    {
        UpdateGameLogic();
    }

    // Re-initialize settings when a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainGame") return;
        InitializeManager();
        InitializeSettings();
    }

    // Initialize or re-initialize settings based on the game type and control mode
    public void InitializeSettings()
    {
        if (!string.IsNullOrEmpty(gameType))
        {
            SetGameType(gameType);
        }
        if (CurrentControlMode == ControlMode.Swipe)
        {
            DeactivateTouchControlButtons();
        }
    }

    // Initialize or re-initialize managers and UI elements when a new scene is loaded
    private void InitializeManager()
    {
        // Assigning managers and UI elements based on their tags
        coinCombinerAndClearer = GameObject.FindWithTag("CoinCombinerTag").GetComponent<CoinCombinerAndClearer>();
        gridManager = GameObject.Find("GridManager").GetComponent<GridManager>();
        gameTypeText = GameObject.FindWithTag("GameTypeTextTag").GetComponent<TextMeshProUGUI>();
    }

    // Updating the game logic related to object spawning
    private void UpdateGameLogic()
    {
        if (!IsPlayerShotCompleted) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnNewRow();
            spawnTimer = spawnInterval;
        }
    }

    public void VaultButtonPressed()
    {
        SceneManager.LoadScene("Vault");
    }

    // Deactivate touch control buttons when the control mode is set to swipe
    public void DeactivateTouchControlButtons()
    {
        GameObject touchControlButtons = GameObject.Find(GameSetupManager.Instance.touchControlButtonsName);
        if (touchControlButtons != null)
        {
            touchControlButtons.SetActive(false);
        }
    }

    //GetGameType
    public string GetGameType()
    {
        return gameType;
    }

    // Set the game type and update related settings
    public void SetGameType(string gameType)
    {
        Debug.Log("SetGameType called with: " + gameType);
        this.gameType = gameType;
        ClearGrid();

        //if the scene is Vault, return
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Vault")
        {
            return;
        }

        // Set the properties based on the selected game type
        if (gameType == "puzzle")
        {
            Debug.Log("Setting Puzzle Mode");
            numberOfObjects = 25;
            spawnInterval = 5000;
            spawnTimer = spawnInterval;
            PlaceManualObjectsInGrid();
            PlaceObjectsInGrid();
            gameTypeText.text = "Puzzle             Mode";
        }
        else if (gameType == "endless")
        {
            numberOfObjects = 10;
            spawnInterval = 5;
            spawnTimer = spawnInterval;
            PlaceManualObjectsInGrid();
            PlaceObjectsInGrid();
            gameTypeText.text = "Endless            Mode";      
        }
        else if (gameType == "campaign")
        {
            numberOfObjects = 30;
            spawnInterval = 10;
            spawnTimer = spawnInterval;
            PlaceManualObjectsInGrid();
            PlaceObjectsInGrid();
            gameTypeText.text = "Campaign              Mode";       
        }
        else if (gameType == "mainmenu")
        {  
            SceneManager.LoadScene("TitleScreen");
        }
        else if (gameType == "vault")
        {
            SceneManager.LoadScene("Vault");
        }
        else
        {
            Debug.Log("Game Type not set");
            gameType = "puzzle";
            numberOfObjects = 5;
            spawnInterval = 7;
            spawnTimer = spawnInterval;
            PlaceManualObjectsInGrid();
            PlaceObjectsInGrid();
            gameTypeText.text = "Puzzle";        
        }
        LevelManager.Instance.UpdateGameTypeUI(gameType);
    }

    // Different game mode initializations
    public void StartPuzzleMode()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        gameType = "puzzle";
        SetGameType(gameType);
    }

    public void StartEndlessMode()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        gameType = "endless";
        SetGameType(gameType);
    }

    public void StartCampaignMode()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        gameType = "campaign";
        SetGameType(gameType);
    }

    // Restart the current scene
    public void ReStart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Methods to increase and decrease spawn speed
    public void IncreaseSpawnSpeed()
    {
        spawnInterval -= 0.5f;        
    }

    public void DecreaseSpawnSpeed()
    {
        spawnInterval += 0.5f;
    }

    // Method to place manually set objects in the grid
    void PlaceManualObjectsInGrid()
    {
        foreach (GridObject gridObject in gridObjects)
        {
            GameObject newObject = Instantiate(gridObject.prefab);
            gridManager.PlaceObjectAtGridPosition(newObject, gridObject.x, gridObject.y);
            newObject.transform.position = gridManager.GridToWorldPosition(gridObject.x, gridObject.y);
        }
    }

    // Method to spawn a new row of objects in the grid
    void SpawnNewRow()
    {
        Debug.Log("SpawnNewRow called");
        if (isLocked)
            return;

        // Check if the bottom row is empty
        bool bottomRowEmpty = true;
        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            if (gridManager.GetObjectAtGridPosition(x, 0) != null)
            {
                bottomRowEmpty = false;
                break;
            }
        }

        // If the bottom row is not empty, reload the scene (game over)
        if (!bottomRowEmpty)
        {
            Debug.Log("Bottom row is not empty. Game Over!");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        isLocked = true;

        // Move objects down one row to make space for new objects
        for (int y = 0; y < gridManager.gridHeight - 1; y++)
        {
            for (int x = 0; x < gridManager.gridWidth; x++)
            {
                GameObject obj = gridManager.GetObjectAtGridPosition(x, y + 1);
                if (obj != null)
                {
                    gridManager.PlaceObjectAtGridPosition(obj, x, y);
                    obj.transform.position = gridManager.GridToWorldPosition(x, y);
                }
            }
        }

        // Instantiate new objects and place them in the top row
        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            GameObject selectedPrefab = ChoosePrefab();
            if (selectedPrefab != null)
            {
                GameObject newObject = Instantiate(selectedPrefab);
                gridManager.PlaceObjectAtGridPosition(newObject, x, gridManager.gridHeight - 1);
                newObject.transform.position = gridManager.GridToWorldPosition(x, gridManager.gridHeight - 1);
            }
        }

        isLocked = false;
    }

    // Clear all objects from the grid
    void ClearGrid()
    {
        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            for (int y = 0; y < gridManager.gridHeight; y++)
            {
                GameObject obj = gridManager.GetObjectAtGridPosition(x, y);
                if (obj != null)
                {
                    Destroy(obj);
                    gridManager.ClearGridPosition(x, y);
                }
            }
        }
    }

    // Place a number of objects in the grid randomly
    void PlaceObjectsInGrid()
    {
        int placedObjects = 0;
        int maxAttempts = 1000;
        int currentAttempts = 0;

        // Calculate the maximum number of rows to fill
        int maxRowsToFill = Mathf.CeilToInt((float)numberOfObjects / gridManager.gridWidth);

        // Try to place objects until the quota is reached or max attempts are exceeded
        while (placedObjects < numberOfObjects && currentAttempts < maxAttempts)
        {
            GameObject selectedPrefab = ChoosePrefab();
            if (selectedPrefab != null)
            {
                int randomX = Random.Range(0, gridManager.gridWidth);
                int randomY = Random.Range(gridManager.gridHeight - maxRowsToFill, gridManager.gridHeight);

                // If the selected grid position is empty, place a new object there
                if (gridManager.GetObjectAtGridPosition(randomX, randomY) == null)
                {
                    GameObject newObject = Instantiate(selectedPrefab);
                    gridManager.PlaceObjectAtGridPosition(newObject, randomX, randomY);
                    newObject.transform.position = gridManager.GridToWorldPosition(randomX, randomY);
                    placedObjects++;
                }
            }

            currentAttempts++;
        }

        if (currentAttempts >= maxAttempts)
        {
            // Handle the case where objects could not be placed after max attempts
            // This could involve logging an error, adjusting the parameters, or other actions depending on the game's design
            Debug.LogWarning("Failed to place all objects in the grid");

            // Clear the grid and try again
            ClearGrid();
            PlaceObjectsInGrid();
        }
    }

    // Choose a prefab based on rarity
    private GameObject ChoosePrefab()
    {
        int totalRarity = 0;
        foreach (PrefabRarity prefabRarity in prefabRarities)
        {
            totalRarity += prefabRarity.rarity;
        }

        int randomValue = Random.Range(0, totalRarity);
        int accumulatedRarity = 0;

        foreach (PrefabRarity prefabRarity in prefabRarities)
        {
            accumulatedRarity += prefabRarity.rarity;
            if (randomValue < accumulatedRarity)
            {
                return prefabRarity.prefab;
            }
        }

        return null;
    }

    // This method is not used in the script, but it could be used to choose a grid object based on rarity
    private GridObject ChooseGridObject()
    {
        int totalRarity = 0;
        foreach (GridObject gridObject in gridObjects)
        {
            totalRarity += gridObject.rarity;
        }

        int randomValue = Random.Range(0, totalRarity);
        int accumulatedRarity = 0;

        foreach (GridObject gridObject in gridObjects)
        {
            accumulatedRarity += gridObject.rarity;
            if (randomValue < accumulatedRarity)
            {
                return gridObject;
            }
        }

        Debug.LogWarning("Failed to choose a grid object");
        return null;
    }
}
