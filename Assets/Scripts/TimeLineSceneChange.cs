using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeLineSceneChange : MonoBehaviour
{
    public string scene;
    // Start is called before the first frame update
    void OnEnable()
    {
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
    
    void Click(string scene)
    {
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
}
