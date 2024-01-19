using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    [Header("Dialogue")]
    public GameObject dialoguePanel;

    public Text dialogueText;
    public Text characterName;

    public GameObject[] dialogueOptions;

    public Color playerFontColor;
    public Color npcFontColor;
    public Color narratorFontColor;

    [Header("Quest")]
    public GameObject questUI;

    public Text questTitle;
    public Text questObjective;
    public Text questProgress;

    [Header("HP panel")]
    public GameObject life1;
    public GameObject life2;
    public GameObject life3;

    [Header("Info panel")]
    public GameObject infoText;

    public void UpdateInfoText(string text, float time)
    {
        StartCoroutine(InfoRoutine(text, time));
    }

    IEnumerator InfoRoutine(string text, float time)
    {
        infoText.SetActive(true);
        infoText.GetComponent<Text>().text = text;
        yield return new WaitForSeconds(time);
        infoText.GetComponent<Text>().text = "";
        infoText.SetActive(false);
    }
}
