using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class SimpleSpeculer:ScriptShader
{

    CullMode cullMode = CullMode.Back;
    public Texture2D _MaintTex;
    public Color _Color=Color.white;
    [Range(0,1)]
    public float _Speculer;
    public Color _SpecColor = Color.white;
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


        v2f.Add(_WorldSpaceLightPos0);
        v2f.Add(worldPos);
        var cameraPos = Camera.main.transform.position;
        var viewDir = (cameraPos - new Vector3(worldPos.x, worldPos.y, worldPos.z)).normalized;
        v2f.Add(new Vector4(viewDir.x,viewDir.y,viewDir.z,0));

        return v2f;
    }

    public override Color frag(List<Vector4> IN)
    {
        Color c = _Color * Tex2D(_MaintTex,IN[1].x, IN[1].y);
        Vector3 worldNormal = (new Vector3(IN[2].x, IN[2].y, IN[2].z)).normalized;
        Vector3 worldLightDir = _WorldSpaceLightPos0;// (new Vector3(IN[3].x, IN[3].y, IN[3].z)).normalized;
        Vector3 worldPos = new Vector3(IN[4].x, IN[4].y, IN[4].z);
        Vector3 viewDir = new Vector3(IN[5].x, IN[5].y, IN[5].z);

        Vector3 Hvl = (viewDir + worldLightDir).normalized;

        float diffuse = Vector3.Dot(worldNormal, worldLightDir);
        float spec = Mathf.Pow(Mathf.Max(0, Vector3.Dot(Hvl, worldNormal)),100*_Speculer);

       
        c = c*diffuse+ spec*_SpecColor;
        c = new Color(c.r,c.g,c.b,1);
        return c;
    }
}
