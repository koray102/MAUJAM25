using System.Threading;
using System;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class GameManagerScript : MonoBehaviour
{

    private Scene currentScene;
    public String afterSceneName;


    public Material TransitionMat;

    private float maskAmount = 1f;
    private float minTransition = -0.11f;
    private float maxTransition = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TransitionMat.SetFloat("_MaskAmount", maskAmount);
        StartCoroutine(SmoothLerp(minTransition, 1f));
        currentScene = SceneManager.GetActiveScene();
    }

    // Update is called once per frame
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.M)) 
        {
            SonrakiSeviye();
        }

    }

    public void SonrakiSeviye()
    {
        StartCoroutine(SmoothLerp(maxTransition, 1f));
        Invoke("SonrakiSeviyeGecis", 2f);
    }
    private void SonrakiSeviyeGecis()
    {
        SceneManager.LoadScene(afterSceneName);
    }

    public void SeviyeTekrari()
    {
        StartCoroutine(SmoothLerp(maxTransition, 1f));

        Invoke("SeviyeTekrariGecis", 2f);
    }
    private void SeviyeTekrariGecis()
    {
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    IEnumerator SmoothLerp(float targetValue, float speed)
    {
        float startValue = maskAmount;
        // Toplam geçiþ süresi = |target - start| / speed
        float duration = Mathf.Abs(targetValue - startValue) / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // elapsed/duration, 0 ile 1 arasýnda gidip geçiþ oranýný belirler.
            maskAmount = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            
            elapsed += Time.deltaTime;

            TransitionMat.SetFloat("_MaskAmount", maskAmount);

            yield return null; // Bir sonraki frame'e geç
        }

        // Son adýmda deðeri tam hedefe eþitliyoruz.
        maskAmount = targetValue;
        Debug.Log("Hedefe ulaþýldý!");
    }
}
