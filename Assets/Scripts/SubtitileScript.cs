using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SubtitileScript : MonoBehaviour
{
    public GameObject textBox;
    public List<float> waitTimings = new List<float> {1,1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
    public float _numOfLines;
    public List<string> linesOfSubtitles = new List<string> { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
    public List<float> lineLenghts = new List<float> { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
    public bool hasPlayed;


    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !hasPlayed)
        {
            hasPlayed = true;
            textBox.GetComponent<Text>().text = "";
            StartCoroutine(TheSequence());
        }
    }

    IEnumerator TheSequence()
    {
        for (int i = 0; i < _numOfLines; i++)
        {
            yield return new WaitForSeconds(waitTimings[i]);
            textBox.GetComponent<Text>().text = linesOfSubtitles[i];
            yield return new WaitForSeconds(lineLenghts[i]);
            textBox.GetComponent<Text>().text = "";
        }
    }
}
