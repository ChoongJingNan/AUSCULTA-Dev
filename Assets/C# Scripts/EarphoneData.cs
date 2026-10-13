using UnityEngine;

[CreateAssetMenu(fileName = "NewEarphone", menuName = "Backrooms/Earphone Data")]
public class EarphoneData : ScriptableObject
{
    public string earphoneName = "Model X";
    [TextArea(3, 5)] public string description = "A mysterious pair of studio monitors found in the backrooms.";
    [Range(0, 10)] public int bassBars = 5;
    [Range(0, 10)] public int midrangeBars = 4;
    [Range(0, 10)] public int trebleBars = 7;
    [Range(0, 10)] public int technicalPerformance = 6;
}