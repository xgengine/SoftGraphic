using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grab : ScriptShader
{
    public Texture2D _MaintTex;
   
    public Color _Color = Color.white;

    public override void Init()
    {
        base.Init();
        depthState.writeEnabled = false;
    }
    public override bool GrabPass()
    {
        return true;
    }
    public override List<Vector4> vert(AppData IN)
    {
        List<Vector4> v2f = new List<Vector4>();
        Vector4 clipPos = PV * M * IN.vertex;
        v2f.Add(clipPos);
        v2f.Add(IN.texcoord);
        v2f.Add(ComputeGrabScreenPos(clipPos));
        return v2f;
    }

    public override Color frag(List<Vector4> IN)
    {
        var c = Tex2D(_MaintTex, IN[1].x, IN[1].y);
        var grabTex = Tex2D(_GrabTextrue, IN[2].x / IN[2].w+c.r*0.05f, IN[2].y / IN[2].w+c.g*0.05f);
        return grabTex*_Color;
    }
}
