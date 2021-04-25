using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//ExecuteInEditMode]
//[ImageEffectAllowedInSceneView]
public class Image : MonoBehaviour
{
    public Material mat;
    //private void onim
    private void Start()
    {
        Debug.Log( GetComponent<Camera>().worldToCameraMatrix);
        Debug.Log(GetComponent<Camera>().projectionMatrix);

    }
    //[ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture buffer0 = RenderTexture.GetTemporary(source.width, source.height, 0);
        RenderTexture buffer1 = RenderTexture.GetTemporary(source.width, source.height, 0);
       // buffer0.anisoLevel = 2;
        //buffer1.anisoLevel = 2;
       // Graphics.Blit(source, buffer0);
        Graphics.Blit(source, buffer1,mat,0);
        mat.SetTexture("_Bloom", buffer1);
        Graphics.Blit(source, destination, mat,1);
        buffer0.Release();
        buffer1.Release();
    }
}
