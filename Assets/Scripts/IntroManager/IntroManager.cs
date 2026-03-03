using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "MainMenu";

    private void Start()
    {
        // Nếu đã xem rồi → skip luôn
        if (PlayerPrefs.GetInt("IntroPlayed", 0) == 1)
        {
            LoadNextScene();
            return;
        }

        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void Update()
    {
        // Nhấn phím bất kỳ để skip
        if (Input.anyKeyDown)
        {
            LoadNextScene();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        PlayerPrefs.SetInt("IntroPlayed", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(nextSceneName);
    }
}