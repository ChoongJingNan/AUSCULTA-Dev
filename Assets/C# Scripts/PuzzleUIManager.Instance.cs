using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PuzzleUIManager : MonoBehaviour
{
    public static PuzzleUIManager Instance;

    [Header("UI References")]
    public GameObject overlayPanel;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text statsText;
    public TMP_Text confirmButtonText;
    public TMP_Text cancelButtonText;

    [HideInInspector] public int selectedIndex = 0; // 0 = Confirm, 1 = Cancel
    private EarphoneInspect activeInspect;

    private void Awake()
    {
        Instance = this;
        if (overlayPanel != null) overlayPanel.SetActive(false);
    }

    public void ShowOverlay(EarphoneData data, EarphoneInspect inspect)
    {
        activeInspect = inspect;
        selectedIndex = 0;
        UpdateVisuals(data);
        if (overlayPanel != null) overlayPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetSelectedIndex(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, 1);
        // Highlight logic can go here (e.g., color tinting Confirm vs Cancel text)
        if (selectedIndex == 0)
        {
            confirmButtonText.fontStyle = FontStyles.Underline | FontStyles.Bold;
            cancelButtonText.fontStyle = FontStyles.Normal;
        }
        else
        {
            confirmButtonText.fontStyle = FontStyles.Normal;
            cancelButtonText.fontStyle = FontStyles.Underline | FontStyles.Bold;
        }
    }

    private void UpdateVisuals(EarphoneData data)
    {
        if (nameText != null) nameText.text = data.earphoneName;
        if (descriptionText != null) descriptionText.text = data.description;

        if (statsText != null)
        {
            statsText.text = 
                $"Bass: {GetBars(data.bassBars)}\n" +
                $"Midrange: {GetBars(data.midrangeBars)}\n" +
                $"Treble: {GetBars(data.trebleBars)}\n" +
                $"Technical Performance: {data.technicalPerformance}/10";
        }
        SetSelectedIndex(0);
    }

    private string GetBars(int count)
    {
        string barStr = "";
        for (int i = 0; i < 10; i++)
        {
            barStr += (i < count) ? "█" : "░";
        }
        return $"{barStr} ({count}/10)";
    }

    public void HideOverlay()
    {
        if (overlayPanel != null) overlayPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}