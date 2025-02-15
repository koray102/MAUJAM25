using System.Threading;
using System;
using UnityEngine;
using System.Collections;

public class GameManagerScript : MonoBehaviour
{
    public int Level = 0;
    public Material TransitionMat;


    private float maskAmount = 1f;
    private float minTransition = -0.1f;
    private float maxTransition = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TransitionMat.SetFloat("_MaskAmount", maskAmount);
        StartCoroutine(SmoothLerp(minTransition, 0.6f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SonrakiSeviye()
    {
        StartCoroutine(SmoothLerp(maxTransition, 1f));
    }

    public void SeviyeTekrari()
    {
        StartCoroutine(SmoothLerp(maxTransition, 1f));
    }

    IEnumerator SmoothLerp(float targetValue, float speed)
    {
        float startValue = maskAmount;
        // Toplam ge�i� s�resi = |target - start| / speed
        float duration = Mathf.Abs(targetValue - startValue) / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // elapsed/duration, 0 ile 1 aras�nda gidip ge�i� oran�n� belirler.
            maskAmount = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            Debug.Log("Current Value: " + maskAmount);
            elapsed += Time.deltaTime;

            TransitionMat.SetFloat("_MaskAmount", maskAmount);

            yield return null; // Bir sonraki frame'e ge�
        }

        // Son ad�mda de�eri tam hedefe e�itliyoruz.
        maskAmount = targetValue;
        Debug.Log("Hedefe ula��ld�!");
    }
}
