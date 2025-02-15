using System.Collections.Generic;
using UnityEngine;

public class CheckBackground : MonoBehaviour
{
    [SerializeField] private int visibleLayer;
    public HashSet<int> touchedLayers = new HashSet<int>();
    internal bool isVisible;
    

    void Start()
    {
        
    }


    void Update()
    {
        isVisible = IsNotTouchingVisibleLayer();
    }


    public bool IsNotTouchingVisibleLayer()
    {
        foreach (var hit in touchedLayers)
        {
            // Kendi objemizi kontrol dışı bırakıyoruz.
            if (hit == gameObject.layer)
                continue;

            if (hit == visibleLayer)
                return true;
        }
        return false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.gameObject);
        touchedLayers.Add(collision.gameObject.layer);
    }


    void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log(collision.gameObject);
        if(touchedLayers.Contains(collision.gameObject.layer))
        {
            touchedLayers.Remove(collision.gameObject.layer);
        }
    }
}
