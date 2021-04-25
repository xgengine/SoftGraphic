using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlitTexture :ScriptShader
{
    public Texture2D _MaintTex;
    public Color _Color;
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
        Vector4 c = _Color * Tex2D(_MaintTex,IN[1].x, IN[1].y);
        return c;
    }
}
