using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoftGraphicType
{
    OpenGL,
    DirectLike,
}
public enum RenderPath
{
    Forword,
    Deferred
}

[CreateAssetMenu(fileName ="SRPRenderAsset",menuName ="SRPAsset")]
public class SRPRenderAsset : ScriptableObject
{
    [Header("ShaderAsset")]
    public GameObject SkyBox;
    public GameObject BlitCopy;
    public GameObject BlitColorDispersion;
    public GameObject INScreenSpaceShadow;
    public GameObject DeferredShading;
  
    [Header("RenderOption")]
    public bool OpenImageProcess;
    public bool SkeyBox = true;
    public bool Clip = true;
    public bool PerpctiveCorrection = true;
    public bool RevertZ = true;
    public bool ScreenSapceShadow=false;
    public RenderPath RenderingPath = RenderPath.Forword;
    public SoftGraphicType GraphicType = SoftGraphicType.OpenGL;

    public ScriptRenderPepeline CreatePipeline()
    {
        return new ScriptRenderPepeline(this);
    }
    public void SetState()
    {
        
    }

}
