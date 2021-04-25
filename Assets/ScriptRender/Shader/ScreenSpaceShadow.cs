using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenSpaceShadow :ScriptShader
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
        Vector4 wpos = _CameraToWorld * vpos;
        var shadowCoord = _WorldToShadow * new Vector4(wpos.x, wpos.y, wpos.z, 1);
        var dist = Tex2D(_ShadowMap, shadowCoord.x, shadowCoord.y).r;
        float atten = 1;
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
        return Color.white* atten;

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
