using System.Collections.Generic;
using UnityEngine;

public class LampHit : MonoBehaviour
{
    public int ShurikenLayerInt;
    public GameObject BlackBg;
    public GameObject WhiteBg;
    public ParticleSystem CrashParticle;
    public SpriteRenderer sprite;
    
    private bool isCrashed;
    private HashSet<int> touchedLayers = new HashSet<int>();
    

    void Start()
    {
        BlackBg.SetActive(false);
        WhiteBg.SetActive(true);
    }


    void Update()
    {
        isCrashed = IsTouchingVisibleLayer();
    }


    private bool IsTouchingVisibleLayer()
    {
        foreach (int layer in touchedLayers)
        {
            if (layer == gameObject.layer)
                continue;
            if (layer == ShurikenLayerInt)
            {
                Crash();
                return true;
            }
        }
        return false;
    }

    private void Crash()
    {
        if(isCrashed)
            return;

        if(CrashParticle != null)
            CrashParticle.Play();

        sprite.enabled = false;
        BlackBg.SetActive(true);
        WhiteBg.SetActive(false);
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
