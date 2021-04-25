using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SkyboxCubed : ScriptShader
{
    public Color _Tint = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [Range(0, 360)]
    public float _Rotation = 0;
    [Range(0,8)]
    public float _Exposure = 1.0f;
    public Cubemap _Tex;

    public override void Init()
    {
        base.Init();
        depthState.writeEnabled = false;
    }
    Vector4 RotateAroundYInDgrees(Vector4 vertex,float degress)
    {
        float alpha = degress * PI / 180.0f;
        float sina = Mathf.Sin(alpha);
        float cosa = Mathf.Cos(alpha);
        float x = vertex.x * cosa - sina * vertex.x;
        float z = vertex.z * sina + cosa * vertex.z;
        return new Vector4(x, z, vertex.y,1); 
    }

    public override List<Vector4> vert(AppData IN)
    {
        List<Vector4> v2f = new List<Vector4>();
        Matrix4x4 nv = new Matrix4x4(V.GetColumn(0), V.GetColumn(1), V.GetColumn(2), new Vector4(0, 0, 0, 1));
        var v = IN.vertex;
        var pos = P * nv * v;
        if(SoftGraphics.setting.RevertZ)
        {
            v2f.Add(new Vector4(pos.x, pos.y, 0, pos.w));
        }
        else
        {
            v2f.Add(new Vector4(pos.x, pos.y, pos.w, pos.w));
            
        }
        v2f.Add(v);
        return v2f;
    }

    public override Color frag(List<Vector4> IN)
    {
        var tex = TexCube(_Tex, new Vector3(IN[1].x, -IN[1].y, IN[1].z));
        var c = tex * _Tint*_Exposure;
        return new Color(c.r,c.g,c.b,1);
        //return new Color(1, 0, 0, 1);
            
    }
}


