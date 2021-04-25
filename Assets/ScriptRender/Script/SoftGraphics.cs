using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Rendering;

public class ScriptRenderTexture
{
    public string name="Temp";
    public int width;
    public int height;
    public Texture2D colorBuffer;
    public Texture2D depthBuffer;
    public bool isFramBuffer=false;
    public ScriptRenderTexture(int width, int height,int depth =32,TextureFormat format = TextureFormat.ARGB32)
    {
        this.width = width;
        this.height = height;
        colorBuffer = new Texture2D(this.width, this.height,format,false);
        if(depth>0)
        {
            depthBuffer = new Texture2D(this.width, this.height, TextureFormat.RFloat, false);
        }
    }
    public ScriptRenderTexture(int width,int height)
    {
        this.width = width;
        this.height = height;
        depthBuffer = new Texture2D(this.width, this.height, TextureFormat.RFloat, false);
    }
    public void Release()
    {
        Object.DestroyImmediate(colorBuffer);
        Object.DestroyImmediate(depthBuffer);
    }
    public void Apply()
    {
        if(colorBuffer !=null)
        {
            colorBuffer.Apply();
        }
        if(depthBuffer !=null)
        {
            depthBuffer.Apply();
        }
    }

}

public class SoftGraphics
{
    public static Matrix4x4 P;
    public static Matrix4x4 V;
    public static Matrix4x4 PV;
    public static bool directPV;
    public static Rect      pixelRect;
    public static Matrix4x4  viewportMatrix;
    public static ScriptRenderTexture frameBuffer;
    static ScriptRenderTexture[] curentRTs;
    public static SRPRenderAsset setting;
    public static RTPool rtPool;
    public static void SetGraphicSettingAndFrameBuffer(SRPRenderAsset assetSetting,ScriptRenderTexture buffer)
    {
        frameBuffer =buffer;
        setting = assetSetting;
        if (setting.GraphicType == SoftGraphicType.OpenGL)
        {
            setting.RevertZ = false;
        }
        if (setting.RenderingPath == RenderPath.Deferred)
        {
            setting.ScreenSapceShadow = true;
        }
    }
    public static void SetDebug(RTPool pool)
    {
        rtPool = pool;
    }
    public static void Init()
    {

    }
    public static void SetRenderTarget(ScriptRenderTexture rt)
    {
        curentRTs = new ScriptRenderTexture[1];
        curentRTs[0] = rt;
        SetViewportMatrix();
    }
    static void SetViewportMatrix()
    {
        var rt = curentRTs[0];
        if (IsOpenGLDeviceType())
        {
            viewportMatrix = new Matrix4x4(
                new Vector4(rt.width * 0.5f, 0, 0, 0),
                new Vector4(0, rt.height * 0.5f, 0, 0),
                new Vector4(0, 0, 0.5f, 0),
                new Vector4(rt.width * 0.5f, rt.height * 0.5f, 0.5f, 1));
        }
        else
        {
            viewportMatrix = new Matrix4x4(
               new Vector4(rt.width * 0.5f, 0, 0, 0),
               new Vector4(0, rt.height * (-0.5f), 0, 0),
               new Vector4(0, 0, 1, 0),
               new Vector4(rt.width * 0.5f, rt.height * 0.5f, 0, 1));
        }
    }
    public static void SetRenderTargets(ScriptRenderTexture[] rt)
    {
        curentRTs = new ScriptRenderTexture[rt.Length];
        for (int i = 0; i < rt.Length; i++)
        {
            curentRTs[i] = rt[i];
        }
        SetViewportMatrix();
    }
    public static bool IsOpenGLDeviceType()
    {
        return setting.GraphicType == SoftGraphicType.OpenGL;
    }
    public static void SetProjectionMatrix(Matrix4x4 mat)
    {
        P = mat;
    }
    public static void SetPVMatrix(Matrix4x4 pv)
    {
        PV = pv;
        directPV = true;
    }
    public static Matrix4x4 GetSoftGPUProjectionMatrix(Matrix4x4 mat,bool isRenderToRT=false)
    {
        //return GL.GetGPUProjectionMatrix(mat, false);
        Matrix4x4 gpuMatrix;
        if (IsOpenGLDeviceType())
        {
            gpuMatrix = mat;
        }
        else
        {
            gpuMatrix = mat;
            if(setting.RevertZ)
            {
                //正交
                if (gpuMatrix.m32 == 0)
                {
                    gpuMatrix.m22 = (-0.5f) * mat.m22;
                    gpuMatrix.m23 = (0.5f) * (1 - mat.m23);

                }
                else
                {
                    gpuMatrix.m22 = (-0.5f) * (1 + mat.m22);
                    gpuMatrix.m23 = (-0.5f) * mat.m23;

                }
            }
            else
            {
                if(gpuMatrix.m32==0)
                {
                    gpuMatrix.m22 = 0.5f * mat.m22;
                    gpuMatrix.m23 = -0.5f * (mat.m23 +1);
                }
                else
                {
                    gpuMatrix.m22 = (mat.m22 - 1) * 0.5f;
                    gpuMatrix.m23 = 0.5f * mat.m23;
                }
            }
            if(isRenderToRT)
            {
                gpuMatrix.m11 = gpuMatrix.m11 * -1;
            }

        }
        return gpuMatrix;
    }
    public static void Clear(ScriptRenderTexture rt, bool clearDepth, bool clearColr, Color c)
    {
        if (clearDepth)
        {
            if (rt.depthBuffer != null)
            {
                float depth = 1;
                if (setting.RevertZ)
                {
                    depth = 0;
                }
                for (int i = 0; i < rt.depthBuffer.width; i++)
                {
                    for (int j = 0; j < rt.depthBuffer.height; j++)
                    {
                        rt.depthBuffer.SetPixel(i, j, new Color(depth, 0, 0, 1));
                    }
                }
            }
        }
        if (clearColr)
        {
            if (rt.colorBuffer != null)
            {
                for (int i = 0; i < rt.colorBuffer.width; i++)
                {
                    for (int j = 0; j < rt.colorBuffer.height; j++)
                    {
                        rt.colorBuffer.SetPixel(i, j, c);
                    }
                }
            }
        }
    }
    public static void ClearRenderTarget(bool clearDepth,bool clearColr,Color c)
    {
        for(int k=0;k<curentRTs.Length;k++)
        {
            Clear(curentRTs[k], clearDepth, clearColr, c);
        }
    }
    public static void SetPerFrameData()
    {
        if(directPV==false)
        {
            ScriptShader.PV = P*V;
            ScriptShader.P = P;
            ScriptShader.V = V;
        }
        else
        {
            ScriptShader.PV = PV;

        }
        directPV = false;
        if(!IsOpenGLDeviceType())
        {
            ScriptShader._ProjectionParams.x= curentRTs[0].isFramBuffer? 1 : -1;
        }
        else
        {
            ScriptShader._ProjectionParams.x= 1;
        }
    }
    public static void SetViewMatrix(Matrix4x4 mat)
    {
        V = mat;
    }
    public static void DrawRender(RenderObject renderObject,bool drawGrab =false)
    {
        ScriptShader shader = renderObject.render.GetComponent<ScriptShader>();
        if ( shader== null)
        {
            shader =renderObject.render.gameObject.AddComponent<ScriptShader>();
        }
        if(renderObject.render.lightmapIndex>=0)
        {
            var lightmapdata = LightmapSettings.lightmaps[renderObject.render.lightmapIndex];
            ScriptShader._LightmapTex = lightmapdata.lightmapColor;
            ScriptShader._LightmapTexST = renderObject.render.lightmapScaleOffset;
        }
        else
        {
            ScriptShader._LightmapTex = null;
        }
        if(shader.GrabPass()&&drawGrab)
        {
            var grabTexture= rtPool.Get("GrabTexture", curentRTs[0].width, curentRTs[0].height,0);
            if(curentRTs[0]!= frameBuffer)
            {
                grabTexture.isFramBuffer = true;
                Blit(curentRTs[0].colorBuffer, grabTexture, setting.BlitCopy);
            }
            else
            {
                grabTexture.colorBuffer.SetPixels32(frameBuffer.colorBuffer.GetPixels32());
            }
            grabTexture.Apply();
            ScriptShader._GrabTextrue = grabTexture.colorBuffer;
        }
        DrawMesh(renderObject.mesh, shader, renderObject.render.localToWorldMatrix);
    }
    public static void DrawMesh(Mesh mesh,ScriptShader shader,Matrix4x4 M)
    {
        shader.Init();
        ScriptShader.M = M;

        var verticesArray = mesh.vertices;
        var tangentsArray = mesh.tangents;
        var normalsArray = mesh.normals;
        var colorsArray = mesh.colors;
        var uvArray = mesh.uv;
        var uvArray1 = mesh.uv2;
        var uvArray2 = mesh.uv3;
        var trianglesArray = mesh.triangles;
        List<List<Vector4>> fragments = new List<List<Vector4>>();
        //顶点
        for (int i = 0; i < verticesArray.Length; i++)
        {
            AppData IN;
            IN.vertex = new Vector4(verticesArray[i].x, verticesArray[i].y, verticesArray[i].z, 1);
            IN.normal = normalsArray.Length > i ? normalsArray[i] : Vector3.zero;
            IN.tangent = tangentsArray.Length > i ? tangentsArray[i] : Vector4.zero;
            IN.color = colorsArray.Length > i ? colorsArray[i] : Color.white;
            IN.texcoord = uvArray.Length > i ? uvArray[i] : Vector2.zero;
            IN.texcoord1= uvArray1.Length > i ? uvArray1[i] : Vector2.zero;
            List<Vector4> v2f = shader.vert(IN);
            fragments.Add(v2f);
        }
        //裁减 
        Clip(trianglesArray, fragments, out List<int> clipedTriangles, out List<List<Vector4>> clipedFragments,shader);
        //透视除法 屏幕坐标
        for (int i = 0; i < clipedFragments.Count; i++)
        {
            Vector4 clipPos = clipedFragments[i][0];
            float clipPosW = clipPos.w;
            Vector4 ndcPos = new Vector4(clipPos.x / clipPos.w, clipPos.y / clipPos.w, clipPos.z / clipPos.w, 1);
            Vector4 screenPos = viewportMatrix * ndcPos;

            clipedFragments[i][0] = new Vector4(screenPos.x, screenPos.y, screenPos.z, 1 / clipPosW);
            if(setting.PerpctiveCorrection)
            {
                for (int j = 1; j < clipedFragments[i].Count; j++)
                {
                    clipedFragments[i][j] = clipedFragments[i][j] / clipPosW;
                }
            }

        }

        List<List<Vector4>> allLerpFraments = new List<List<Vector4>>();
        //插值
        for (int i = 0; i < clipedTriangles.Count; i += 3)
        {
            var v0 = clipedFragments[clipedTriangles[i + 0]];
            var v1 = clipedFragments[clipedTriangles[i + 1]];
            var v2 = clipedFragments[clipedTriangles[i + 2]];
            //背面剔除
            var pos0 = new Vector3(v0[0].x, v0[0].y , 0);
            var pos1 = new Vector3(v1[0].x, v1[0].y , 0);
            var pos2 = new Vector3(v2[0].x, v2[0].y , 0);
            var face = Vector3.Cross(pos2 - pos0, pos1 - pos0);
            if (setting.GraphicType == SoftGraphicType.DirectLike)
            {
                if(ScriptShader._ProjectionParams.x<0)
                {
                    if ((shader.cullMode == CullMode.Back && face.z < 0) || shader.cullMode == CullMode.Front && face.z > 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if ((shader.cullMode == CullMode.Back && face.z > 0) || shader.cullMode == CullMode.Front && face.z <0)
                    {
                        continue;
                    }
                }
            }
            else
            {
                if ((shader.cullMode == CullMode.Back && face.z < 0) || shader.cullMode == CullMode.Front && face.z > 0)
                {
                    continue;
                }
            }
            var lerpFragments = LerpFraments(v0, v1, v2);
            allLerpFraments.AddRange(lerpFragments);
      
        }
        for (int i = 0; i < allLerpFraments.Count; i++)
        {
            int x = (int)allLerpFraments[i][0].x;
            int y = (int)allLerpFraments[i][0].y;
            float depth = allLerpFraments[i][0].z;

            if(shader.ZFunction!= CompareFunction.Always)
            {
                if (curentRTs[0].depthBuffer != null)
                {
                    float curentDepth = curentRTs[0].depthBuffer.GetPixel(x, y).r;
                    // z Test
                    if (setting.RevertZ && depth < curentDepth)
                    {
                        continue;
                    }
                    if (!setting.RevertZ && depth > curentDepth)
                    {
                        continue;
                    }
                }
            }
            Color[] cs = new Color[curentRTs.Length];
            if(shader.HaveDeferred && setting.RenderingPath== RenderPath.Deferred)
            {
                cs = shader.fragDeferred(allLerpFraments[i]).ToArray();

            }
            else
            {
                cs[0] = shader.frag(allLerpFraments[i]);
            }

            if(shader.depthState.writeEnabled && curentRTs[0].depthBuffer !=null)
            {
                curentRTs[0].depthBuffer.SetPixel(x, y, new Color(depth, 0, 0, 1));
            }
            for(int k =0;k<curentRTs.Length;k++)
            {
                if(curentRTs[k].colorBuffer !=null)
                {
                    Color dest = curentRTs[k].colorBuffer.GetPixel(x, y);
                    //像素操作
                    Color finalColor = ColorOperation(cs[k], dest, shader.SrcBlend, shader.DstBlend, shader.blendOp);
                    curentRTs[k].colorBuffer.SetPixel(x, y, finalColor);
                }
            }
        }
        //Debug.Log(mesh.name+" "+cpcout);
    }
    public static void Blit(Texture2D source, ScriptRenderTexture dest, GameObject mat)
    {
        Matrix4x4 pv;
        if (IsOpenGLDeviceType())
        {
            pv = new Matrix4x4(
            new Vector4(2, 0, 0, 0),
            new Vector4(0, 2, 0, 0),
            new Vector4(0, 0, -0.02f, 0),
            new Vector4(-1, -1, -0.98f, 1));
        }
        else
        {
            if (dest.isFramBuffer)
            {
                pv = new Matrix4x4(
                new Vector4(2, 0, 0, 0),
                new Vector4(0, 2, 0, 0),
                new Vector4(0, 0, 0.01f, 0),
                new Vector4(-1, -1, 0.99f, 1));
            }
            else
            {
                pv = new Matrix4x4(
                new Vector4(2, 0, 0, 0),
                new Vector4(0, -2, 0, 0),
                new Vector4(0, 0, 0.01f, 0),
                new Vector4(-1, 1, 0.99f, 1));
            }
        }
        var tempRTs = curentRTs;
        {
            SetRenderTarget(dest);
            SetPVMatrix(pv);
            SetPerFrameData();
            var s = mat.GetComponent<ScriptShader>();
            if (source != null)
            {
                source.Apply();
                s.SetProperty("_MaintTex", source);
            }
            DrawMesh(GetScreenQuad(), s, Matrix4x4.identity);
            dest.Apply();
        }
        curentRTs = tempRTs;
        PV = P * V;
    }
    static Mesh GetScreenQuad()
    {
        Mesh mesh = new Mesh();
        mesh.name = "BlitCopyQuad";
        Vector3[] vertex = new Vector3[]
        {
            new Vector3(0,0,0.5f),
            new Vector3(0,1,0.5f),
            new Vector3(1,1,0.5f),
            new Vector3(1,0,0.5f),
        };
        int[] triangle = new int[]
        {
            0,1,2,
            0,2,3,
        };
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(0,1),
            new Vector2(1,1),
            new Vector2(1,0),
        };
        mesh.vertices = vertex;
        mesh.triangles = triangle;
        mesh.uv = uv;
        return mesh;
    }
    static Color ColorOperation(Color src, Color dest, BlendMode srcBlend, BlendMode destBlend, BlendOp blendOp)
    {
        Color finalColor;
        Color srcColor = src;
        if (srcBlend == BlendMode.One)
        {
            srcColor = src;
        }
        else if (srcBlend == BlendMode.SrcAlpha)
        {
            srcColor = src * src.a;
        }
        Color destColor = Color.clear;
        if (destBlend == BlendMode.Zero)
        {
            destColor = Color.clear;
        }
        else if (destBlend == BlendMode.One)
        {
            destColor = dest;
        }
        else if (destBlend == BlendMode.OneMinusSrcAlpha)
        {
            destColor = dest * (1 - src.a);
        }
        finalColor = srcColor + destColor;
        return finalColor;

    }
    static void Clip(int[] inputTriangles,List<List<Vector4>> inputFragments,out List<int> clipedTriangles,out List<List<Vector4>> clipedFragments,ScriptShader shader)
    {
        clipedFragments = new List<List<Vector4>>();
        clipedTriangles = new List<int>();
        if(!setting.Clip)
        {
            clipedFragments.AddRange(inputFragments);
            clipedTriangles.AddRange(inputTriangles);
            return;
        }

        for (int i = 0; i < inputTriangles.Length; i += 3)
        {
            var v0 = inputFragments[inputTriangles[i]];
            var v1 = inputFragments[inputTriangles[i + 1]];
            var v2 = inputFragments[inputTriangles[i + 2]];

            List<List<Vector4>> output;
            if(IsOpenGLDeviceType())
            {
                output = GLClip(v0, v1, v2);
            }
            else
            {
                output = DXClip(v0, v1, v2);
            }
            //重组三角形
            int index = clipedFragments.Count;
            for (int j = 0; j < output.Count; j++)
            {
                List<Vector4> data = new List<Vector4>();
                data.AddRange(output[j]);
                clipedFragments.Add(data);
            }
            for (int n = 0; n < output.Count - 2; n++)
            {
                clipedTriangles.Add(index + 0);
                clipedTriangles.Add(index + n + 1);
                clipedTriangles.Add(index + n + 2);
            }
        }
    }
    static List<List<Vector4>> GLClip(List<Vector4> v0,List<Vector4> v1,List<Vector4> v2)
    {
        List<List<Vector4>> output = new List<List<Vector4>>();
        List<List<Vector4>> input = new List<List<Vector4>>();
        // 三个点都不在视椎体，剔除
        if (
            (v0[0].x > v0[0].w && v1[0].x > v1[0].w && v2[0].x > v2[0].w)
            || (-v0[0].x > v0[0].w && -v1[0].x > v1[0].w && -v2[0].x > v2[0].w)
            || (v0[0].y > v0[0].w && v1[0].y > v1[0].w && v2[0].y > v2[0].w)
            || (-v0[0].y > v0[0].w && -v1[0].y > v1[0].w && -v2[0].y > v2[0].w)
            || (v0[0].z > v0[0].w && v1[0].z > v1[0].w && v2[0].z > v2[0].w)
            || (-v0[0].z > v0[0].w && -v1[0].z > v1[0].w && -v2[0].z > v2[0].w))
        {
            return output;
        }
        input.Add(v0);
        input.Add(v1);
        input.Add(v2);
        // min w剪裁 防止透视除法出错
        float MINW = 0.001f;
        for (int k = 0; k < input.Count; k++)
        {
            var one = input[k];
            var two = input[(k + 1) % input.Count];
            var pDot = one[0].w > MINW ? 1 : -1;
            var cDot = two[0].w > MINW ? 1 : -1;
            if (pDot > 0)
            {
                output.Add(one);
            }
            if (pDot * cDot < 0)
            {
                float lerp = (one[0].w - MINW) / (one[0].w - two[0].w);
                List<Vector4> allLerps = new List<Vector4>();
                for (int j = 0; j < one.Count; j++)
                {
                    allLerps.Add(Vector4.Lerp(one[j], two[j], lerp));
                }
                output.Add(allLerps);
            }
        }
        input.Clear();
        input.AddRange(output);
        output.Clear();
        // z近剪裁面
        for (int k = 0; k <input.Count; k++)
        {
            var one = input[k];
            var two = input[(k + 1) % input.Count];
            if ((-one[0].z) <= one[0].w)
            {
                output.Add(one);
            }
            if ((one[0].w + one[0].z) * (two[0].w + two[0].z) < 0)
            {
                float lerp = -(one[0].w + one[0].z) / (two[0].w - one[0].w + two[0].z - one[0].z);
                List<Vector4> allLerps = new List<Vector4>();
                for (int j = 0; j < one.Count; j++)
                {
                    allLerps.Add(Vector4.Lerp(one[j], two[j], lerp));
                }

                output.Add(allLerps);
            }
        }
        return output;
    }
    static List<List<Vector4>> DXClip(List<Vector4> v0, List<Vector4> v1, List<Vector4> v2)
    {
        List<List<Vector4>> output = new List<List<Vector4>>();
        List<List<Vector4>> input = new List<List<Vector4>>();
        // 三个点都不在视椎体，剔除
        if (
            (v0[0].x > v0[0].w && v1[0].x > v1[0].w && v2[0].x > v2[0].w)
            || (-v0[0].x > v0[0].w && -v1[0].x > v1[0].w && -v2[0].x > v2[0].w)
            || (v0[0].y > v0[0].w && v1[0].y > v1[0].w && v2[0].y > v2[0].w)
            || (-v0[0].y > v0[0].w && -v1[0].y > v1[0].w && -v2[0].y > v2[0].w)
            || (v0[0].z > v0[0].w && v1[0].z > v1[0].w && v2[0].z > v2[0].w)
            || (v0[0].z *v0[0].w<0&& v1[0].z *v1[0].w<0 && v2[0].z * v2[0].w<0))
        {
            return output;
        }
        input.Add(v0);
        input.Add(v1);
        input.Add(v2);
        // Min w剪裁 防止透视除法出错
        float MINW = 0.001f;
        for (int k = 0; k < input.Count; k++)
        {
            var one = input[k];
            var two = input[(k + 1) % input.Count];
            var pDot = one[0].w > MINW ? 1 : -1;
            var cDot = two[0].w > MINW ? 1 : -1;
            if (pDot > 0)
            {
                output.Add(one);
            }
            if (pDot * cDot < 0)
            {
                float lerp = (one[0].w - MINW) / (one[0].w - two[0].w);
                List<Vector4> allLerps = new List<Vector4>();
                for (int j = 0; j < one.Count; j++)
                {
                    allLerps.Add(Vector4.Lerp(one[j], two[j], lerp));
                }
                output.Add(allLerps);
            }
        }
        input.Clear();
        input.AddRange(output);
        output.Clear();
        // z 1  剪裁
        for (int k = 0; k < input.Count; k++)
        {
            var one = input[k];
            var two = input[(k + 1) % input.Count];
            if (one[0].z <= one[0].w)
            {
                output.Add(one);
            }
            if ((one[0].z - one[0].w) * (two[0].z - two[0].w) < 0)
            {
                float lerp = Mathf.Abs(one[0].z - one[0].w) / (Mathf.Abs(two[0].z - two[0].w) + Mathf.Abs(one[0].z - one[0].w));
                List<Vector4> allLerps = new List<Vector4>();
                for (int j = 0; j < one.Count; j++)
                {
                    allLerps.Add(Vector4.Lerp(one[j], two[j], lerp));
                }

                output.Add(allLerps);
            }
        }
        input.Clear();
        input.AddRange(output);
        output.Clear();
        //z 0 剪裁
        for (int k = 0; k < input.Count; k++)
        {
            var one = input[k];
            var two = input[(k + 1) % input.Count];
            if (one[0].z * one[0].w >= 0)
            {
                output.Add(one);
            }
            if ((one[0].z * one[0].w) * (two[0].z * two[0].w) < 0)
            {
                float lerp = Mathf.Abs(one[0].z - 0) / (Mathf.Abs(two[0].z - 0) + Mathf.Abs(one[0].z - 0));
                List<Vector4> allLerps = new List<Vector4>();
                for (int j = 0; j < one.Count; j++)
                {
                    allLerps.Add(Vector4.Lerp(one[j], two[j], lerp));
                }

                output.Add(allLerps);
            }
        }
        return output;
    }
    static List<List<Vector4>> LerpFraments(List<Vector4> v0, List<Vector4> v1, List<Vector4> v2)
    {
        int xMin = (int)Mathf.Min(v0[0].x, v1[0].x, v2[0].x);
        xMin = Mathf.Max(xMin, 0);
        int xMax = (int)Mathf.Max(v0[0].x, v1[0].x, v2[0].x);
        xMax = Mathf.Min(curentRTs[0].width, xMax);
        int yMin = (int)Mathf.Min(v0[0].y, v1[0].y, v2[0].y,0);
        yMin = Mathf.Max(yMin, 0);
        int yMax = (int)Mathf.Max(v0[0].y, v1[0].y, v2[0].y);
        yMax = Mathf.Min(yMax, curentRTs[0].height);
        List<List<Vector4>> fragments = new List<List<Vector4>>();
        for (int c = xMin; c < xMax + 1; c++)
        {
            for (int r = yMin; r < yMax + 1; r++)
            {
                if (!PointInTrangle(v0[0], v1[0], v2[0], c + 0.5f, r + 0.5f))
                {
                    continue;
                }
                Vector3 lerpValue = BarycentricCoord(v0[0], v1[0], v2[0], c + 0.5f, r + 0.5f);

                float W = 1.0f / (lerpValue.x * v0[0].w + lerpValue.y * v1[0].w + lerpValue.z * v2[0].w);
                List<Vector4> fragData = new List<Vector4>();
                float z = lerpValue.x * v0[0].z + lerpValue.y * v1[0].z + lerpValue.z * v2[0].z;
                fragData.Add(new Vector4(c, r, z, 1));
                for (int i = 1; i < v0.Count; i++)
                {
                    if(setting.PerpctiveCorrection)
                    {
                        Vector4 d = (v0[i] * lerpValue.x + v1[i] * lerpValue.y + v2[i] * lerpValue.z) * W;
                        fragData.Add(d);
                    }
                    else
                    {
                        Vector4 d = (v0[i] * lerpValue.x + v1[i] * lerpValue.y + v2[i] * lerpValue.z);
                        fragData.Add(d);
                    }
                  
                }
                fragments.Add(fragData);
            }
        }
        return fragments;
    }
    static bool PointInTrangle(Vector4 v0, Vector4 v1, Vector4 v2, float x, float y)
    {
        var AP = new Vector3(x - v0.x, y - v0.y, 0);
        var AB = new Vector3(v1.x - v0.x, v1.y - v0.y, 0);
        var BP = new Vector3(x - v1.x, y - v1.y, 0);
        var BC = new Vector3(v2.x - v1.x, v2.y - v1.y, 0);
        var CP = new Vector3(x - v2.x, y - v2.y, 0);
        var CA = new Vector3(v0.x - v2.x, v0.y - v2.y, 0);
        var AP_AB = Vector3.Cross(AP, AB);
        var BP_BC = Vector3.Cross(BP, BC);
        var CP_CA = Vector3.Cross(CP, CA);
        return Vector3.Dot(AP_AB, BP_BC) >= 0 && Vector3.Dot(BP_BC, CP_CA) >= 0;
    }
    static bool isLeftPoint(Vector4 a, Vector4 b, float x, float y)
    {
        float s = (a.x - x) * (b.y - y) - (a.y - y) * (b.x - x);
        return s < 0 ? false : true;
    }
    static Vector3 BarycentricCoord(Vector4 v0, Vector4 v1, Vector4 v2, float x, float y)
    {
        float area = TriangleArea(v0.x, v0.y, v1.x, v1.y, v2.x, v2.y);
        float v0Area = TriangleArea(v1.x, v1.y, v2.x, v2.y, x, y);
        float v1Area = TriangleArea(v0.x, v0.y, v2.x, v2.y, x, y);
        float wx = v0Area / area;
        float wy = v1Area / area;
        float wz = 1 - wx - wy;
        return new Vector3(wx, wy, wz);
    }
    static float TriangleArea(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        return Mathf.Abs(x1 * y2 + x2 * y3 + x3 * y1 - x3 * y2 - x1 * y3 - x2 * y1);
    }
}
