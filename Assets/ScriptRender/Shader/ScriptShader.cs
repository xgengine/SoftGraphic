using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public struct AppData
{
    public Vector4 vertex;
    public Vector4 tangent;
    public Vector4 normal;
    public Vector4 texcoord;
    public Vector4 texcoord1;
    public Vector4 color;
}

public struct DepthState
{
    public bool writeEnabled;
}

public class ScriptShader:MonoBehaviour
{
    public bool HaveDeferred = false;
    public int RenderQueue;
    public static float PI = 3.14f;
    public static Matrix4x4 M;
    public static Matrix4x4 _WorldToObject;
    public static Matrix4x4 V;
    public static Matrix4x4 ShadowPV;
    public static Matrix4x4 _WorldToShadow;
    public static Texture2D _ShadowMap;
    public static Texture2D _ScrrenSpaceShadowMap;
    public static Matrix4x4 P;
    public static Matrix4x4 PV;
    public static Texture2D _DepthMapTex;
    public static Texture2D _LightmapTex;
    public static Vector4 _LightmapTexST;
    public static Vector4 _WorldSpaceLightPos0;
    public static Color _LightColor;
    public static Vector4 _ProjectionParams;
    public static Texture2D _CameraDepthTexture;
    public static Texture2D _GBuffer0;
    public static Texture2D _GBuffer1;
    public static Texture2D _GBuffer2;
    public static Matrix4x4 _CamerInvProjection;
    public static Matrix4x4 _WorldToCamera;
    public static Matrix4x4 _CameraToWorld;
    public static Vector3 _WorldSpaceCameraPos;
    public static Texture2D _GrabTextrue;
    public BlendMode SrcBlend=BlendMode.One;
    public BlendMode DstBlend=BlendMode.Zero;
    public BlendOp blendOp = BlendOp.Add;
    public CullMode cullMode = CullMode.Back;
    public UnityEngine.Rendering.CompareFunction ZFunction= CompareFunction.Less;
    public DepthState depthState;
    public virtual List<Vector4>  vert(AppData IN)
    {

        List<Vector4> v2f = new List<Vector4>();
        Vector4 clipPos =PV* M* IN.vertex;
        v2f.Add(clipPos);
        return v2f;
    }
    public virtual void Init()
    {
        depthState.writeEnabled = true;
    }
    public virtual Color frag(List<Vector4> IN)
    {
        return Color.magenta;
    }
    public virtual List<Color> fragDeferred(List<Vector4> IN)
    {
        return null;
    }
    public virtual bool GrabPass()
    {
        return false;
    }
    public static Color Tex2D(Texture2D tex,float x,float y)
    {
        if(tex ==null)
        {
            return Color.white;
        }
        return tex.GetPixelBilinear(x,y);
    }
   
    public static Color TexCube(Cubemap cubemap,Vector3 dir)
    {
       
        //cubemap.Apply();
        dir = dir.normalized;
        float mag = Mathf.Max(Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z)));

        if( Mathf.Abs(mag-Mathf.Abs(dir.x))<0.0001f)
        {
            if (dir.x>0)
            {
               
                return cubemap.GetPixel(CubemapFace.PositiveX, (int)((1 - (dir.z/mag + 1) * 0.5f) * cubemap.width), (int)(((dir.y/mag + 1) * 0.5f) * cubemap.width));
            }
            else
            {
                return cubemap.GetPixel(CubemapFace.NegativeX, (int)((dir.z/mag + 1) * 0.5f * cubemap.width), (int)(((dir.y/mag + 1) * 0.5f) * cubemap.width));

            }
        }
        else if( Mathf.Abs( mag-Mathf.Abs(dir.y))<0.000001f)
        {
            if(dir.y>0)
            {
                return cubemap.GetPixel(CubemapFace.NegativeY, (int)(((dir.x/mag + 1) * 0.5f) * cubemap.width), (int)((1 - (dir.z/mag + 1) * 0.5f) * cubemap.width));
               
            }
            else
            {
                return cubemap.GetPixel(CubemapFace.PositiveY, (int)((dir.x/mag+ 1) * 0.5f * cubemap.width), (int)((dir.z/mag + 1) * 0.5f * cubemap.width));
            }
        }
        else
        {
            if (dir.z>0)
            {
               
                return cubemap.GetPixel(CubemapFace.PositiveZ, (int)((dir.x/mag+ 1) * 0.5f * cubemap.width), (int)((dir.y/mag + 1) * 0.5f * cubemap.width));
            }
            else
            {
               
                return cubemap.GetPixel(CubemapFace.NegativeZ, (int)((1 - (dir.x/mag + 1) * 0.5f) * cubemap.width), (int)((dir.y/mag + 1) * 0.5f * cubemap.width));
            }
           
        }
    }
    public void SetProperty(string name, object target)
    {
        var p = this.GetType().GetField(name);
        p.SetValue(this, target);
    }
    public Vector4 ComputeScreenPos(Vector4 clipPos)
    {

        Vector4 o = clipPos * 0.5f;
        o = new Vector4(o.x + o.w, o.y*_ProjectionParams.x + o.w, clipPos.z, clipPos.w);
        //Vector4 o = new Vector4((clipPos.x / clipPos.w * 0.5f + 0.5f) * clipPos.w, (clipPos.y / clipPos.w * 0.5f + 0.5f) * clipPos.w, clipPos.z, clipPos.w);
        return o;
    }
    public Vector4 ComputeGrabScreenPos(Vector4 clipPos)
    {
        float scale = 1;
        if(SoftGraphics.setting.GraphicType== SoftGraphicType.DirectLike)
        {
            scale = -1;
        }
        Vector4 o = clipPos * 0.5f;
        o = new Vector4(o.x + o.w, o.y * scale + o.w, clipPos.z, clipPos.w);
        return o;
    }

    protected Color BRDF_PBS(Vector3 diffColor, Vector3 specColor, float oneMinusReflectivity, float smooth, Vector3 normal, Vector3 viewDir, Vector3 lightDir, float atten)
    {
        Vector3 halfDir = (viewDir + lightDir).normalized;
        var nl = Mathf.Max(0, Vector3.Dot(normal, lightDir));
        var nv = Mathf.Abs(Vector3.Dot(normal, viewDir));
        var nh = Mathf.Max(0, Vector3.Dot(halfDir, normal));
        var lh = Mathf.Max(0, Vector3.Dot(halfDir, lightDir));
        var lv = Mathf.Clamp(Vector3.Dot(lightDir, viewDir), 0, 1);

        var roughness = (1 - smooth) * (1 - smooth);
        var D = Trowbridge_Reitz_GGX(roughness, nh);
        var k = (roughness + 1) * (roughness + 1) / 8;
        var V = GemetrySmith(nv, nl, k);
        var F = specColor + (new Vector3(1, 1, 1) - specColor) * Mathf.Pow(1 - lh, 5);
        var specularTerm = D * V * F / Mathf.Max(0, 4 * nv * nl);

        var col = (diffColor + specularTerm * PI) * nl;
        col = col * atten;
        return new Color(col.x, col.y, col.z, 1);
    }
    protected float Trowbridge_Reitz_GGX(float roughness, float H)
    {
        H = Mathf.Max(0, H);
        var up = roughness * roughness;
        var H2 = H * H;
        var down = 3.14f * Mathf.Pow((H2 * (up - 1) + 1), 2);
        return up / down;
    }
    protected float GemetrySchlickGGX(float v, float k)
    {
        var up = v;
        float down = v * (1 - k) + k;
        return up / down;
    }
    protected float GemetrySmith(float V, float L, float k)
    {
        return GemetrySchlickGGX(V, k) * GemetrySchlickGGX(L, k);
    }
}
