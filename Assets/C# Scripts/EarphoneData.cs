using UnityEngine;

[CreateAssetMenu(fileName = "NewEarphone", menuName = "Backrooms/Earphone Data")]

public class EarphoneData : ScriptableObject
{
    [Header("Preview Audio")]
    public AudioClip previewTrack;
    [Range(0f, 1f)] public float previewVolume = 0.8f;
    public float previewStartTime = 0f;
    [Header("UI Display")]
    public Sprite earphoneSprite;
    public string earphoneName = "Model X";
    [TextArea(3, 5)] public string description = "A mysterious pair of earphones, they look like the ones from the shop.";
    
    [Header("3D Model Preview")]
    public GameObject modelPrefab; // Drag your earphone 3D model prefab here

    [Header("Audio Stats")]
    [Range(0, 10)] public int bassBars = 5;
    [Range(0, 10)] public int midrangeBars = 4;
    [Range(0, 10)] public int trebleBars = 7;
    [Range(0, 10)] public int technicalPerformance = 6;
}