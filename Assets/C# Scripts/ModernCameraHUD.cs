using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ModernCameraHUD : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerHealth playerHealth;

    [Header("Top Left - REC Indicator")]
    public TextMeshProUGUI recText;
    public float recBlinkRate = 0.8f;

    [Header("Top Center - Recording Playtime")]
    public TextMeshProUGUI playtimeText;

    [Header("Top Right - Battery HUD")]
    public TextMeshProUGUI batteryPercentText;

    [Tooltip("Assign 4 Images (Element 0 = Cell_1, Element 3 = Cell_4). Leave empty to auto-detect from Hierarchy.")]
    public Image[] batteryCells = new Image[4];
    public bool overrideWithLowColorOnCritical = true;
    public Color lowCellColor = Color.red;

    [Tooltip("How long (in minutes) it takes for the percentage to reach 0% naturally")]
    public float batteryLifeInMinutes = 60f; 

    [Header("Bottom Left - Resolution & FPS")]
    public TextMeshProUGUI resAndFpsText;
    public float fpsUpdateInterval = 0.5f;

    [Header("Bottom Right - Date & Time")]
    public TextMeshProUGUI dateTimeText;

    private static float totalSessionElapsedTime = 0f;

    private float recBlinkTimer = 0f;
    private float displayBatteryPercent = 100f;
    private float fpsCountTimer = 0f;
    private int framesCounter = 0;
    private float currentFPS = 0f;
    
    private int lastHealth = -1; 
    private Color[] originalCellColors;

    // Direct reference or CanvasGroup for hiding/showing
    private CanvasGroup hudCanvasGroup;

    private void Start()
    {
        hudCanvasGroup = GetComponent<CanvasGroup>();
        if (hudCanvasGroup == null)
        {
            hudCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        AutoAssignCellsIfNeeded();
        CacheCellColors();

        if (playerHealth == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                    playerHealth = player.GetComponentInParent<PlayerHealth>();
                }
            }
        }

        if (playerHealth != null)
        {
            lastHealth = playerHealth.GetCurrentHealth();
        }

        UpdateResolutionAndFPS();
    }

    /// <summary>
    /// Call this from external scripts to show/hide the HUD.
    /// </summary>
    public void SetHUDActive(bool isActive)
    {
        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = isActive ? 1f : 0f;
            hudCanvasGroup.blocksRaycasts = isActive;
            hudCanvasGroup.interactable = isActive;
        }
        else
        {
            gameObject.SetActive(isActive);
        }
    }

    private void AutoAssignCellsIfNeeded()
    {
        bool needsAutoAssign = batteryCells == null || batteryCells.Length < 4;
        
        if (!needsAutoAssign)
        {
            for (int i = 0; i < batteryCells.Length; i++)
            {
                if (batteryCells[i] == null)
                {
                    needsAutoAssign = true;
                    break;
                }
            }
        }

        if (needsAutoAssign)
        {
            Transform container = transform.Find("Battery_Container");
            if (container == null)
            {
                container = transform.Find("CameraHUD_Canvas/Battery_Container");
            }

            if (container != null)
            {
                batteryCells = new Image[4];
                batteryCells[0] = container.Find("Cell_1")?.GetComponent<Image>();
                batteryCells[1] = container.Find("Cell_2")?.GetComponent<Image>();
                batteryCells[2] = container.Find("Cell_3")?.GetComponent<Image>();
                batteryCells[3] = container.Find("Cell_4")?.GetComponent<Image>();
            }
        }
    }

    private void CacheCellColors()
    {
        if (batteryCells != null && batteryCells.Length > 0)
        {
            originalCellColors = new Color[batteryCells.Length];
            for (int i = 0; i < batteryCells.Length; i++)
            {
                if (batteryCells[i] != null)
                {
                    originalCellColors[i] = batteryCells[i].color;
                }
            }
        }
    }

    private void Update()
    {
        UpdateRecBlink();
        UpdatePlaytime();
        UpdateBatteryHUD();
        UpdatePerformanceReadout();
        UpdateDateTime();
    }

    private void UpdateRecBlink()
    {
        if (recText == null) return;

        recBlinkTimer += Time.unscaledDeltaTime;
        if (recBlinkTimer >= recBlinkRate)
        {
            recText.enabled = !recText.enabled;
            recBlinkTimer = 0f;
        }
    }

    private void UpdatePlaytime()
    {
        totalSessionElapsedTime += Time.unscaledDeltaTime;

        if (playtimeText != null)
        {
            TimeSpan time = TimeSpan.FromSeconds(totalSessionElapsedTime);
            playtimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
        }
    }

    private void UpdateBatteryHUD()
    {
        if (playerHealth == null) return;

        int currentHp = playerHealth.GetCurrentHealth();

        if (lastHealth == -1)
        {
            lastHealth = currentHp;
        }

        if (currentHp > lastHealth && currentHp == playerHealth.maxHealth)
        {
            displayBatteryPercent = 100f;
        }
        else if (currentHp < lastHealth)
        {
            float hpPercentCeiling = ((float)currentHp / playerHealth.maxHealth) * 100f;
            if (displayBatteryPercent > hpPercentCeiling)
            {
                displayBatteryPercent = hpPercentCeiling;
            }
        }

        lastHealth = currentHp;

        float drainPerSecond = 100f / (batteryLifeInMinutes * 60f);
        displayBatteryPercent -= drainPerSecond * Time.unscaledDeltaTime;
        displayBatteryPercent = Mathf.Clamp(displayBatteryPercent, 0f, 100f);

        if (batteryPercentText != null)
        {
            batteryPercentText.text = $"{Mathf.FloorToInt(displayBatteryPercent)}%";
        }

        int activeCells = Mathf.Clamp(currentHp, 0, batteryCells.Length);

        for (int i = 0; i < batteryCells.Length; i++)
        {
            if (batteryCells[i] == null) continue;

            batteryCells[i].gameObject.SetActive(true);

            if (i < activeCells)
            {
                batteryCells[i].enabled = true;

                if (activeCells <= 1 && overrideWithLowColorOnCritical)
                {
                    batteryCells[i].color = lowCellColor;
                }
                else if (originalCellColors != null && i < originalCellColors.Length)
                {
                    batteryCells[i].color = originalCellColors[i];
                }
            }
            else
            {
                batteryCells[i].enabled = false;
            }
        }
    }

    private void UpdatePerformanceReadout()
    {
        fpsCountTimer += Time.unscaledDeltaTime;
        framesCounter++;

        if (fpsCountTimer >= fpsUpdateInterval)
        {
            currentFPS = framesCounter / fpsCountTimer;
            framesCounter = 0;
            fpsCountTimer = 0f;
            UpdateResolutionAndFPS();
        }
    }

    private void UpdateResolutionAndFPS()
    {
        if (resAndFpsText != null)
        {
            resAndFpsText.text = $"{Screen.width}x{Screen.height} | {Mathf.RoundToInt(currentFPS)} FPS";
        }
    }

    private void UpdateDateTime()
    {
        if (dateTimeText != null)
        {
            dateTimeText.text = DateTime.Now.ToString("MMM dd yyyy | hh:mm:ss tt").ToUpper();
        }
    }
}