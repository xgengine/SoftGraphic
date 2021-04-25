using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class Lit:ScriptShader
{
    public Texture2D _MaintTex;
    public Color _Color=Color.white;
    [Range(0,1)]
    public float _Smooth;
    [Range(0, 1)]
    public float _Metalic;
    public Cubemap _ReflectionProbe;
    public override List<Vector4> vert(AppData IN)
    {
        List<Vector4> v2f = new List<Vector4>();
        Vector4 clipPos = PV * M * IN.vertex;
        Vector4 worldPos = M.MultiplyPoint(IN.vertex);
  
        v2f.Add(clipPos);
        v2f.Add(IN.texcoord);

        var mat = M.transpose.inverse;
        mat.SetColumn(3, new Vector4(0, 0, 0, 1));
        v2f.Add(mat * IN.normal);

        v2f.Add(worldPos);
        var cameraPos = Camera.main.transform.position;
        var viewDir = (cameraPos - new Vector3(worldPos.x, worldPos.y, worldPos.z)).normalized;
        v2f.Add(new Vector4(viewDir.x,viewDir.y,viewDir.z,0));

        v2f.Add(new Vector4(IN.texcoord1.x * _LightmapTexST.x + _LightmapTexST.z, IN.texcoord1.y * _LightmapTexST.y + _LightmapTexST.w));
        v2f.Add(ComputeScreenPos(clipPos));

        return v2f;
    }
    public override Color frag(List<Vector4> IN)
    {
        Color c = _Color * Tex2D(_MaintTex,IN[1].x, IN[1].y);
        Vector3 worldNormal = (new Vector3(IN[2].x, IN[2].y, IN[2].z)).normalized;
        Vector3 worldLightDir = _WorldSpaceLightPos0;
        Vector3 worldPos = new Vector3(IN[3].x, IN[3].y, IN[3].z);

        var viewDir = (_WorldSpaceCameraPos - new Vector3(worldPos.x, worldPos.y, worldPos.z)).normalized;

        Vector4 lightmapUV = new Vector2(IN[5].x, IN[5].y);

        float atten = LightingAtten(worldPos,IN[6]);
        var oneMinusReflectivity = (1 - 0.04f) * (1 - _Metalic);        
        var diffColor =new Vector3( c.r ,c.g,c.b) * oneMinusReflectivity;
        var specColor = _Metalic * new Vector3(c.r, c.g, c.b) + (1 - _Metalic) * new Vector3(0.04f, 0.04f, 0.04f);

        return BRDF_PBS(diffColor,specColor,oneMinusReflectivity,_Smooth,worldNormal,viewDir,worldLightDir,atten);
    }
    public override List<Color> fragDeferred(List<Vector4> IN)
    {
        List<Color> outputs = new List<Color>();
        Color c = _Color * Tex2D(_MaintTex, IN[1].x, IN[1].y);
        Vector3 worldNormal = (new Vector3(IN[2].x, IN[2].y, IN[2].z)).normalized;
        Vector3 worldLightDir = _WorldSpaceLightPos0;
        Vector3 worldPos = new Vector3(IN[3].x, IN[3].y, IN[3].z);
        Vector3 viewDir = new Vector3(IN[4].x, IN[4].y, IN[4].z);
        Vector4 lightmapUV = new Vector2(IN[5].x, IN[5].y);

        var oneMinusReflectivity = (1 - 0.04f) * (1 - _Metalic);
        var diffColor = new Vector3(c.r, c.g, c.b) * oneMinusReflectivity;
        var specColor = _Metalic * new Vector3(c.r, c.g, c.b) + (1 - _Metalic) * new Vector3(0.04f, 0.04f, 0.04f);

        outputs.Add(new Color(diffColor.x, diffColor.y, diffColor.z, 1));
        outputs.Add(new Color(specColor.x, specColor.y, specColor.z, _Smooth));
        outputs.Add(new Color(worldNormal.x * 0.5f + 0.5f, worldNormal.y * 0.5f + 0.5f, worldNormal.z * 0.5f + 0.5f, 1));
        return outputs;
    }
    float LightingAtten(Vector3 worldPos, Vector4 coord)
    {
        float atten = 1;
        if (SoftGraphics.setting.ScreenSapceShadow)
        {
            if (_ScrrenSpaceShadowMap != null)
            {
                return Tex2D(_ScrrenSpaceShadowMap, coord.x / coord.w, coord.y / coord.w).r;
            }
        }
        else
        {
            if (_ShadowMap != null)
            {
                var shadowCoord = _WorldToShadow * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1);
                var dist = Tex2D(_ShadowMap, shadowCoord.x, shadowCoord.y).r;
                if (SoftGraphics.setting.RevertZ)
                {
                    if (shadowCoord.z < dist - 0.01f)
                    {
                        atten = 0.7f;

                    }
                    else
                    {
                        atten = 1;
                    }
                }
                else
                {
                    if (shadowCoord.z > dist + 0.01f)
                    {
                        atten = 0.7f;

                    }
                    else
                    {
                        atten = 1;
                    }
                }
            }
        }
        return atten;
    }


}
