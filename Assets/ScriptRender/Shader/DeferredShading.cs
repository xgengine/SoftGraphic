using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeferredShading : ScriptShader
{
    public Texture2D _MaintTex;
    public Color _Color = Color.white;
    public override List<Vector4> vert(AppData IN)
    {
        List<Vector4> v2f = new List<Vector4>();
        Vector4 clipPos = PV * M * IN.vertex;
        v2f.Add(clipPos);
        v2f.Add(IN.texcoord);
        v2f.Add(ComputeScreenPos(clipPos));
        return v2f;
    }

    public override Color frag(List<Vector4> IN)
    {
        Vector4 vpos = ComputeCameraSpacePosFromDepth(IN);
        Vector4 wPos = _CameraToWorld * vpos;
        Vector2 sPos = new Vector2(IN[1].x , IN[1].y);
        float atten = 1;
        if(_ScrrenSpaceShadowMap!=null)
        {
            atten = Tex2D(_ScrrenSpaceShadowMap, sPos.x, sPos.y).r;
        }
       
        Vector3 worldLightDir = _WorldSpaceLightPos0;
        var viewDir = (_WorldSpaceCameraPos- new Vector3(wPos.x, wPos.y, wPos.z)).normalized;

        var gbuffer0 = Tex2D(_GBuffer0, sPos.x, sPos.y);
        var gbuffer1 = Tex2D(_GBuffer1, sPos.x, sPos.y);
        var gbuffer2 = Tex2D(_GBuffer2, sPos.x, sPos.y);

        var diffColor = new Vector3(gbuffer0.r, gbuffer0.g, gbuffer0.b);
        var specColor = new Vector3(gbuffer1.r, gbuffer1.g, gbuffer1.b);
        var smooth = gbuffer1.a;
        var normal = new Vector3(gbuffer2.r * 2 - 1, gbuffer2.g * 2 - 1, gbuffer2.b * 2 - 1).normalized;
        var oneMinusReflectivity = 1-Mathf.Max(specColor.x,Mathf.Max(specColor.y,specColor.z));
        return BRDF_PBS(diffColor, specColor, oneMinusReflectivity, smooth, normal, viewDir, worldLightDir, atten);

    }

    Vector4 ComputeCameraSpacePosFromDepth(List<Vector4> IN)
    {
        float zdepth = Tex2D(_CameraDepthTexture, IN[1].x, IN[1].y).r;
        if(SoftGraphics.setting.RevertZ)
        {
            zdepth = 1 - zdepth;
        }
        Vector4 ndcPos = new Vector4(IN[2].x, IN[2].y, zdepth, 1);
        ndcPos.x = ndcPos.x * 2.0f - 1;
        ndcPos.y = ndcPos.y * 2 - 1;
        ndcPos.z = ndcPos.z * 2 - 1;
        Vector4 camPos = _CamerInvProjection * ndcPos;
        return new Vector4(camPos.x/camPos.w,camPos.y/camPos.w,camPos.z/camPos.w*-1,1);

    }
}
