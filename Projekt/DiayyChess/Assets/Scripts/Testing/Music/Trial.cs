using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Trial : MonoBehaviour
{
    public Button yourButton;
    public AudioSource audioData;



void Start()
    {

        Button btn = yourButton.GetComponent<Button>();
        btn.onClick.AddListener(TaskOnClick);
    }

    void TaskOnClick()
    {
        Debug.Log("You have clicked the button!");
        audioData = GetComponent<AudioSource>();





        audioData.Play(0);
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Awake()
    {
       // videoPlayer = videoGO.AddComponent<VideoPlayer>();
        //videoAudioSource = videoGO.AddComponent<AudioSource>();
      //  videoAudioSource.playOnAwake = false;

      //  videoPlayer.source = VideoSource.Url;
      //  videoPlayer.url = Application.streamingAssetsPath + Path.DirectorySeparatorChar + movieFileName;

      //  videoPlayer.playOnAwake = false;
     //   videoPlayer.isLooping = true;

     //   videoPlayer.renderMode = VideoRenderMode.RenderTexture;
      //  videoPlayer.targetTexture = videoRenderTexture;
       // videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;

      //  videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
      //  videoPlayer.controlledAudioTrackCount = 1;
      //  videoPlayer.EnableAudioTrack(0, true);
      //  videoPlayer.SetTargetAudioSource(0, videoAudioSource);
      //  videoAudioSource.volume = 1f;

      //  StartCoroutine(PrepareAndPlayVideo());
    }

   // IEnumerator PrepareAndPlayVideo()
    //{
     //   videoPlayer.Prepare();

      //  while (!videoPlayer.isPrepared)
       // {
        //    Debug.Log("Preparing Video");
         //   yield return null;
        //}

//        Debug.Log("Done prepping.");//

   //     videoPlayer.Play();/
    //    videoAudioSource.Play();
   // }
}
