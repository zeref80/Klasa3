using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGSoundScript : MonoBehaviour {


    public GameObject go = GameObject.Find("Audio");
  //  public AudioSource asour;
    private static BGSoundScript instance = null;
    public static BGSoundScript Instance
    {
        get { return instance; }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            instance = this;
        }

        DontDestroyOnLoad(this.gameObject);
     //   if (scene.name == "Chess")
     //   {
    //        asour.volume = 0.1f;
        //}


    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(SceneManager.GetActiveScene().name);
        //  Scene scene = SceneManager.GetActiveScene();

        if (SceneManager.GetActiveScene().name == "Chess")
        {
            go.GetComponent<AudioSource>().volume = 0.1f;
        }
        else 
        {
            go.GetComponent<AudioSource>().volume = 1f;
        }

    }
}
