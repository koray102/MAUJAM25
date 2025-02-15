using System.Threading;
using System;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class GameManagerScript : MonoBehaviour
{
    public int Level = 0;
    public Material TransitionMat;

    private Scene currentScene;

    private float maskAmount = 1f;
    private float minTransition = -0.1f;
    private float maxTransition = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TransitionMat.SetFloat("_MaskAmount", maskAmount);
        StartCoroutine(SmoothLerp(minTransition, 0.6f));
        currentScene = SceneManager.GetActiveScene();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SonrakiSeviye()
    {
        StartCoroutine(SmoothLerp(maxTransition, 1f));
        Invoke("SonrakiSeviyeGecis", 2f);
    }
    private void SonrakiSeviyeGecis()
    {

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
            Debug.Log("Current Value: " + maskAmount);
            elapsed += Time.deltaTime;

            TransitionMat.SetFloat("_MaskAmount", maskAmount);

            yield return null; // Bir sonraki frame'e geç
        }

        // Son adýmda deðeri tam hedefe eþitliyoruz.
        maskAmount = targetValue;
        Debug.Log("Hedefe ulaþýldý!");
    }
}
