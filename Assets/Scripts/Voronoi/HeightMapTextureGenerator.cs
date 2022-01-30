using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMapTextureGenerator : MonoBehaviour
{
    public RenderTexture renderTexture;
    public Texture2D targetTexture2D;

    public ComputeShader CS_Heightmap;
    private ComputeBuffer heightBuffer;
    private Renderer renderer;
    public Vector2 MapResolution;

    [Range(0, 1)]
    public float persistance;
    [Range(0.001f, 2.0f)]
    public float frequency;
    [Range(0.001f, 4.0f)]
    public float lacunarity;

    [Range(-0.5f, 0.5f)]
    public float threshold;
    [Range(0.0f, 1.0f)]
    public float waterLevel;


    public float scale;
    [Range(-1.0f, 1.0f)]
    public float xOffset;
    [Range(0.0f, 1.0f)]
    public float yOffset;
    


    void Start()
    {
        renderTexture = new RenderTexture((int)MapResolution.x, (int)MapResolution.y, 24);
        targetTexture2D = new Texture2D((int)MapResolution.x, (int)MapResolution.y, TextureFormat.RGB24, false);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        renderer = GetComponent<Renderer>();
        GenerateRenderTexture();
        renderTexture.DiscardContents();



    }

    private void GenerateRenderTexture()
    {
        
        
        //renderTexture.useMipMap = false;
        
        //int kernelIndex = this.CS_Heightmap.FindKernel("HeightMapCalculation");
        CS_Heightmap.SetTexture(0, "output_texture", renderTexture);
        CS_Heightmap.SetFloat("frequency", frequency);
        CS_Heightmap.SetFloat("persistance", persistance);
        CS_Heightmap.SetFloat("lacunarity", lacunarity);
        
        CS_Heightmap.SetFloat("scale", scale);
        CS_Heightmap.SetFloat("xOffset", xOffset);
        CS_Heightmap.SetFloat("yOffset", yOffset);
        CS_Heightmap.SetFloat("threshold", threshold);
        CS_Heightmap.SetFloat("waterLevel", waterLevel);
        CS_Heightmap.SetVector("MapResolution", MapResolution);


        CS_Heightmap.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
        
        toTexture2D(renderTexture, targetTexture2D);
        
        

        renderer.material.mainTexture = targetTexture2D;
        
    }

   private void toTexture2D(RenderTexture rTex, Texture2D target)
    {
        
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        target.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        target.Apply();
        return;
    }


    void Update()
    {
        //GenerateRenderTexture();

        //int kernelIndex = this.CS_Heightmap.FindKernel("HeightMapCalculation");


    }

}
