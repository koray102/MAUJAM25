using System.Collections.Generic;
using UnityEngine;

public class CheckBackground : MonoBehaviour
{
    [SerializeField] private int visibleLayer;
    [SerializeField] private float visibleDelay = 1f; // Temasın algılanması için gecikme süresi (saniye cinsinden)
    private float visibleTimer = 0f;

    public HashSet<int> touchedLayers = new HashSet<int>();
    internal bool isVisible;
    

    void Start()
    {
        
    }


    void Update()
    {
        if (IsTouchingVisibleLayer())
    {
        visibleTimer += Time.deltaTime;
        if (visibleTimer >= visibleDelay)
        {
            isVisible = true;
        }
        else
        {
            isVisible = false;
        }
    }
    else
    {
        visibleTimer = 0f;
        isVisible = false;
    }
    }


    private bool IsTouchingVisibleLayer()
{
    foreach (int layer in touchedLayers)
    {
        if (layer == gameObject.layer)
            continue;
        if (layer == visibleLayer)
            return true;
    }
    return false;
}


    void OnTriggerEnter2D(Collider2D collision)
    {
        touchedLayers.Add(collision.gameObject.layer);
    }


    void OnTriggerExit2D(Collider2D collision)
    {
        if(touchedLayers.Contains(collision.gameObject.layer))
        {
            touchedLayers.Remove(collision.gameObject.layer);
        }
    }
}
