using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class SimpleDiffuse:ScriptShader
{

    CullMode cullMode = CullMode.Back;
    public Texture2D _MaintTex;
    public Color _Color=Color.white;
    public override List<Vector4> vert(AppData IN)
    {
        List<Vector4> v2f = new List<Vector4>();
        Vector4 clipPos = PV * M * IN.vertex;
        v2f.Add(clipPos);
        v2f.Add(IN.texcoord);
        v2f.Add(_WorldSpaceLightPos0);
        var mat = M.transpose.inverse;
        mat.SetColumn(3, new Vector4(0, 0, 0, 1));
        v2f.Add(mat*IN.normal );
        return v2f;
    }

    public override Color frag(List<Vector4> IN)
    {
        Color c = _Color * Tex2D(_MaintTex,IN[1].x, IN[1].y);
        Vector3 worldNormal = (new Vector3(IN[3].x, IN[3].y, IN[3].z)).normalized;
        Vector3 worldLightDir = (new Vector3(IN[2].x, IN[2].y, IN[2].z)).normalized;
        float diffuse = Vector3.Dot(worldNormal, worldLightDir);
        c = c * _LightColor * diffuse;
        c = new Color(c.r,c.g,c.b,1);
        return c;
    }
}
