using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WaterPostProcess : MonoBehaviour
{
    public Volume volume;
    ColorAdjustments colorAdj;
    public GameObject pool_surface;
    // Start is called before the first frame update
    void Start()
    {
        volume.profile.TryGet<ColorAdjustments>(out colorAdj);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void randomWaterColor()
    {
        // volume
        volume.weight = UnityEngine.Random.Range(0.05f,1);
        var randG = UnityEngine.Random.Range(100, 160);
        var randB = UnityEngine.Random.Range(100, 160);
        var intensity = 1.5f;
        Color color = new Color(85/255.0f * intensity, 
                                randG/255.0f * intensity,
                                randB/255.0f * intensity);
        colorAdj.colorFilter.value = color;
        colorAdj.postExposure.value = UnityEngine.Random.Range(-0.2f, 0.2f);
        colorAdj.hueShift.value = UnityEngine.Random.Range(-5f, 5f);
        colorAdj.saturation.value = UnityEngine.Random.Range(0.0f, 90f);
        colorAdj.contrast.value = UnityEngine.Random.Range(0.0f, 20f);
        // pool surface
        Color surfaceColor = new Color(85/255.0f, 
                                       randG/255.0f,
                                       randB/255.0f,
                                       0.9f);
        Material pool_material = pool_surface.GetComponent<Renderer>().material;
        pool_material.SetColor("_MiddleWaterColor", surfaceColor);
        pool_material.SetColor("_EdgeWaterColor", surfaceColor);
        print("RANDOM COLOR");
    }
}
