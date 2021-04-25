using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlitColorDispersion:ScriptShader
{
    public Texture2D _MaintTex;
    public float _Scale = 0.1f;
    public override List<Vector4> vert(AppData IN)
    {
        List<Vector4> v2f = new List<Vector4>();
        Vector4 clipPos = PV * M * IN.vertex;
        v2f.Add(clipPos);
        v2f.Add(IN.texcoord);
        return v2f;
    }
    public override Color frag(List<Vector4> IN)
    {
        Vector2 uv0 = new Vector2(IN[1].x - _Scale, IN[1].y);
        Vector2 uv1 = new Vector2(IN[1].x + _Scale, IN[1].y);
        Color c;

        c.r = Tex2D(_MaintTex, uv0.x, uv0.y).r;
        c.g = Tex2D(_MaintTex, IN[1].x, IN[1].y).g;       
        c.b = Tex2D(_MaintTex, uv1.x, uv1.y).b;
        c.a = 1;
        return c;
    }
}
