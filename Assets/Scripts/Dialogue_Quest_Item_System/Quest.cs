using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Quest File", menuName = "New Quest")]
public class Quest : ScriptableObject
{
    public int questID;
    public string questName;
    public string questGiver;
    [TextArea(10,15)]
    public string questDescription;

    [TextArea(1, 5)]
    public string questShortDescription;


    public GameObject trigger;
    public Item item;
    public int itemAmountRequired;

    public questType questTypes;

    public int[] unlockQuestIDs;

    public bool isActive;
    public bool isCompleted;
}
public enum questType
{
    Information,
    Collect
}