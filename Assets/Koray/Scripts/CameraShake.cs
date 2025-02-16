using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // Bu metodu istediğiniz an çağırarak kamerayı sarsabilirsiniz.
    // duration: sarsıntı süresi, magnitude: sarsıntı yoğunluğu
    public IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
