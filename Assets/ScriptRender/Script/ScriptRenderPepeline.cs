using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Rendering;
public class RenderObject
{
    public Mesh mesh;
    public MeshRenderer render;
    public ScriptShader shader;
}
public class ScriptRenderPepeline
{
    public ScriptRenderTexture   mainRendRT;
    public ScriptRenderTexture   frameBuffer;
    public ScriptRenderTexture[] GBuffers;
    public RTPool rtPool;
    public SRPRenderAsset setting;
    public ScriptRenderPepeline(SRPRenderAsset setting)
    {
        this.setting = setting;
        rtPool = new RTPool();
    }
    protected void PrepareRT(Camera camera)
    {
        rtPool.ReleaeAll();
        frameBuffer = rtPool.Get("FrameBuffer",camera.pixelWidth, camera.pixelHeight, (setting.OpenImageProcess || camera.allowHDR)?0:32);
        frameBuffer.isFramBuffer = true;
    }
    public void RenderScene()
    {
        Camera camera = Camera.main;
        PrepareRT(camera);
        SoftGraphics.SetDebug(rtPool);
        SoftGraphics.SetGraphicSettingAndFrameBuffer(setting,frameBuffer);           
        var allRenderObjects = GetAllRenderObject();
        var light = GameObject.FindObjectOfType<Light>();
        var worlddir = light.gameObject.transform.forward * -1;
        ScriptShader._WorldSpaceLightPos0 = new Vector4(worlddir.x, worlddir.y, worlddir.z, 1);
        ScriptShader._LightColor = light.color * light.intensity;
        ScriptShader._WorldSpaceCameraPos = camera.transform.position;

        if(setting.RenderingPath== RenderPath.Deferred)
        {
            GBuffers = new ScriptRenderTexture[3];
            GBuffers[0] = rtPool.Get("GBuffer0", camera.pixelWidth, camera.pixelHeight);
            GBuffers[1] = rtPool.Get("GBuffer1", camera.pixelWidth, camera.pixelHeight,0);
            GBuffers[2] = rtPool.Get("GBuffer2", camera.pixelWidth, camera.pixelHeight,0);

            SoftGraphics.SetRenderTargets(GBuffers);
            SoftGraphics.ClearRenderTarget(true, true, new Color32(0, 0, 0, 1));
            SoftGraphics.SetViewMatrix(camera.worldToCameraMatrix);
            SoftGraphics.SetProjectionMatrix(SoftGraphics.GetSoftGPUProjectionMatrix(camera.projectionMatrix, true));
            SoftGraphics.SetPerFrameData();
            DrawAllRenderObject(allRenderObjects, 0, 2500,DrawFilterMode.Deferred);
            ScriptShader._CamerInvProjection = camera.projectionMatrix.inverse;
            ScriptShader._CameraToWorld = camera.worldToCameraMatrix.inverse;
            ScriptShader._CameraToWorld[0, 2] *= -1;
            ScriptShader._CameraToWorld[1, 2] *= -1;
            ScriptShader._CameraToWorld[2, 2] *= -1;
            GBuffers[0].depthBuffer.Apply();
            for(int i=0;i<GBuffers.Length;i++)
            {
                GBuffers[i].Apply();
            }
            ScriptShader._GBuffer0 = GBuffers[0].colorBuffer;
            ScriptShader._GBuffer1 = GBuffers[1].colorBuffer;
            ScriptShader._GBuffer2 = GBuffers[2].colorBuffer;
            ScriptShader._CameraDepthTexture = GBuffers[0].depthBuffer;
            //ToDo： 渲染前向渲染物体的深度
        }
        //shadow
        if (light.shadows != LightShadows.None)
        {
            //screen space shadow need render depth
            if(setting.ScreenSapceShadow && setting.RenderingPath!=RenderPath.Deferred)
            {
                var depthBuffer = rtPool.GetDepthRT("Depth",camera.pixelWidth, camera.pixelHeight);
                SoftGraphics.SetRenderTarget(depthBuffer);
                SoftGraphics.ClearRenderTarget(true, true, new Color32(0, 0, 0,1));
                SoftGraphics.SetViewMatrix(camera.worldToCameraMatrix);
                SoftGraphics.SetProjectionMatrix(SoftGraphics.GetSoftGPUProjectionMatrix(camera.projectionMatrix, true));
                SoftGraphics.SetPerFrameData();
                DrawAllRenderObject(allRenderObjects, 0, 2500);
                ScriptShader._CamerInvProjection = camera.projectionMatrix.inverse;
                ScriptShader._CameraToWorld = camera.worldToCameraMatrix.inverse;
                ScriptShader._CameraToWorld[0, 2] *= -1;
                ScriptShader._CameraToWorld[1, 2] *= -1;
                ScriptShader._CameraToWorld[2, 2] *= -1;
                depthBuffer.Apply();
                ScriptShader._CameraDepthTexture = depthBuffer.depthBuffer;
            }

            var b = GetSceneBounds(allRenderObjects);
            CalculateShadowCameraMatrix(light, b, out Matrix4x4 pMatrix, out Matrix4x4 vMatrix);
            var shadowMapRt = rtPool.GetDepthRT("ShadowMap",512, 512);
            SoftGraphics.SetRenderTarget(shadowMapRt);
            SoftGraphics.ClearRenderTarget(true, true, new Color32(0, 0, 0, 0));
            SoftGraphics.SetViewMatrix(vMatrix);
            SoftGraphics.SetProjectionMatrix(pMatrix);
            SoftGraphics.SetPerFrameData();
            ScriptShader._WorldToShadow = CalculateWorldToShadowMatrix(pMatrix * vMatrix);
            ScriptShader._ShadowMap = shadowMapRt.depthBuffer;
            DrawAllRenderObject(allRenderObjects, 0, 5000);

            if(setting.ScreenSapceShadow)
            {
                var temp = rtPool.Get("ScreenSpaceShadowMap",camera.pixelWidth, camera.pixelHeight,0);
                SoftGraphics.Blit(null, temp, setting.INScreenSpaceShadow);
                ScriptShader._ScrrenSpaceShadowMap = temp.colorBuffer;
            }
            else
            {
                ScriptShader._ScrrenSpaceShadowMap = null;
            }
        }
        else
        {
            ScriptShader._ScrrenSpaceShadowMap = null;
            ScriptShader._ShadowMap = null;
        }

        //main render   
        bool isRendToRT = false;

        if (camera.allowHDR || setting.OpenImageProcess)
        {
            mainRendRT = rtPool.Get("MainRendRT",camera.pixelWidth, camera.pixelHeight);
            isRendToRT = true;
        }
        else
        {
            mainRendRT = frameBuffer;
        }
        if (setting.RenderingPath == RenderPath.Deferred)
        {
            SoftGraphics.Blit(null, mainRendRT, setting.DeferredShading);
            //copy Depth 拷贝深度
            var copyed = rtPool.Get("depthCopy",GBuffers[0].depthBuffer.width, GBuffers[0].depthBuffer.height,0,TextureFormat.RFloat);
            copyed.isFramBuffer = mainRendRT.isFramBuffer;
            SoftGraphics.Blit(GBuffers[0].depthBuffer, copyed, setting.BlitCopy);
            mainRendRT.depthBuffer=copyed.colorBuffer;
            
        }
        SoftGraphics.SetRenderTarget(mainRendRT);
        if(setting.RenderingPath != RenderPath.Deferred)
        {
            SoftGraphics.ClearRenderTarget(true, true, new Color32(49, 77, 121, 255));
        }
        SoftGraphics.SetViewMatrix(camera.worldToCameraMatrix);
        SoftGraphics.SetProjectionMatrix(SoftGraphics.GetSoftGPUProjectionMatrix(camera.projectionMatrix, isRendToRT));
        SoftGraphics.SetPerFrameData();
        DrawAllRenderObject(allRenderObjects, 0, 2500,setting.RenderingPath== RenderPath.Deferred ?DrawFilterMode.NoDeferred:DrawFilterMode.All);
        if (setting.SkeyBox)
        {
            DrawSkyBox();
        }
        DrawAllRenderObject(allRenderObjects, 2501, 5000, setting.RenderingPath == RenderPath.Deferred ? DrawFilterMode.NoDeferred : DrawFilterMode.All,true);
        //Post Blit
        if (camera.allowHDR && setting.OpenImageProcess)
        {
            var buffer = rtPool.Get("Post Temp",camera.pixelWidth, camera.pixelHeight,0);
            SoftGraphics.Blit(mainRendRT.colorBuffer, buffer, setting.BlitColorDispersion);
            SoftGraphics.Blit(buffer.colorBuffer, frameBuffer, setting.BlitCopy);
        }
        else if(!camera.allowHDR&& setting.OpenImageProcess)
        {
            SoftGraphics.Blit(mainRendRT.colorBuffer, frameBuffer, setting.BlitColorDispersion);
        }
        else if(camera.allowHDR && !setting.OpenImageProcess)
        {
            SoftGraphics.Blit(mainRendRT.colorBuffer, frameBuffer, setting.BlitCopy);
        }
        rtPool.ApplyAll();
    }
    protected  Bounds GetSceneBounds(List<RenderObject> allRenderObjects)
    {
        if(allRenderObjects.Count>0)
        {
            Bounds bounds = allRenderObjects[0].render.bounds;
            foreach(var ro in allRenderObjects)
            {
                bounds.Encapsulate(ro.render.bounds);
            }
            return bounds;
        }
        return new Bounds(Vector3.zero, Vector3.one);
    }
    public bool IsOpenGLGraphic()
    {
        return this.setting.GraphicType == SoftGraphicType.OpenGL;
    }
    protected void CalculateShadowCameraMatrix(Light light,Bounds sceneBounds, out Matrix4x4 pMatrix,out Matrix4x4 vMatrix)
    {
        float lightSpaceXMin = float.MinValue;
        float lightSpaceXMax = float.MaxValue;
        float lightSpaceYMin = float.MinValue;
        float lightSpaceYMax = float.MaxValue;
        float lightSpaceZMin = float.MinValue;
        float lightSpaceZMax = float.MaxValue;

        List<Vector3> aabb = GetAABB(sceneBounds);
        for(int i=0;i<aabb.Count;i++)
        {
            var localLightPos= light.transform.worldToLocalMatrix.MultiplyPoint(aabb[i]);
            lightSpaceXMin = Mathf.Max(localLightPos.x, lightSpaceXMin);
            lightSpaceYMin = Mathf.Max(localLightPos.y, lightSpaceYMin);
            lightSpaceZMin = Mathf.Max(localLightPos.z, lightSpaceZMin);
            lightSpaceXMax = Mathf.Min(localLightPos.x, lightSpaceXMax);
            lightSpaceYMax = Mathf.Min(localLightPos.y, lightSpaceYMax);
            lightSpaceZMax = Mathf.Min(localLightPos.z, lightSpaceZMax);
        }

        Vector3 postion = new Vector3((lightSpaceXMax + lightSpaceXMin) * 0.5f, (lightSpaceYMax + lightSpaceYMin) * 0.5f, lightSpaceZMax);
        float size = Mathf.Max(lightSpaceXMin - lightSpaceXMax, lightSpaceYMin - lightSpaceYMax)*0.5f;
        GameObject shadowCamera = new GameObject("shadow");
        shadowCamera.transform.position = light.transform.position;
        shadowCamera.transform.rotation = light.transform.rotation;
        shadowCamera.transform.position = shadowCamera.transform.position+ light.transform.right*postion.x+ light.transform.up * postion.y+light.transform.forward * postion.z;
        var c =shadowCamera.AddComponent<Camera>();
        c.orthographic = true;
        c.orthographicSize = size;
        c.nearClipPlane = -1;
        c.farClipPlane = lightSpaceZMin - lightSpaceZMax+1;

        pMatrix = SoftGraphics.GetSoftGPUProjectionMatrix(c.projectionMatrix,true);
        vMatrix = c.worldToCameraMatrix;
        Object.DestroyImmediate(shadowCamera);
    }
    protected Matrix4x4 CalculateWorldToShadowMatrix(Matrix4x4 PV)
    {
        if(SoftGraphics.IsOpenGLDeviceType())
        {
            return new Matrix4x4(
                new Vector4(0.5f, 0, 0, 0),
                new Vector4(0, 0.5f, 0, 0),
                new Vector4(0, 0, 0.5f, 0),
                new Vector4(0.5f, 0.5f, 0.5f, 1)
            ) * PV;
        }
        else
        {
            return new Matrix4x4(
                new Vector4(0.5f, 0, 0, 0),
                new Vector4(0, -0.5f, 0, 0),
                new Vector4(0, 0,1, 0),
                new Vector4(0.5f, 0.5f, 0, 1)
          ) * PV;
        }
        
    }
    private List<Vector3> GetAABB(Bounds bounds)
    {
        List<Vector3> aabb = new List<Vector3>();
        aabb.Add(bounds.center + new Vector3(bounds.extents.x,  bounds.extents.y, bounds.extents.z));
        aabb.Add(bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z));
        aabb.Add(bounds.center + new Vector3(bounds.extents.x,  bounds.extents.y, -bounds.extents.z));
        aabb.Add(bounds.center + new Vector3(-bounds.extents.x,  bounds.extents.y, -bounds.extents.z));
        aabb.Add(bounds.center + new Vector3(bounds.extents.x,  -bounds.extents.y, bounds.extents.z));
        aabb.Add(bounds.center + new Vector3(-bounds.extents.x,  -bounds.extents.y, bounds.extents.z));
        aabb.Add(bounds.center + new Vector3(bounds.extents.x,  -bounds.extents.y, -bounds.extents.z));
        aabb.Add(bounds.center + new Vector3(-bounds.extents.x,  -bounds.extents.y, -bounds.extents.z));
        return aabb;

    }
    private void DrawAllRenderObject(List<RenderObject> allRenderObjects,int minRenderQueue,int maxRenderQueue, DrawFilterMode filterMode =DrawFilterMode.All,bool rendGrab=false)
    {
        foreach (var ro in allRenderObjects)
        {
            if(ro.shader.RenderQueue>=minRenderQueue && ro.shader.RenderQueue<=maxRenderQueue )
            {
                if(filterMode ==DrawFilterMode.All)
                {
                    SoftGraphics.DrawRender(ro,rendGrab);
                }
                else if(filterMode ==DrawFilterMode.Deferred && ro.shader.HaveDeferred)
                {
                    SoftGraphics.DrawRender(ro,rendGrab);
                }
                else if(filterMode == DrawFilterMode.NoDeferred && ro.shader.HaveDeferred==false)
                {
                    SoftGraphics.DrawRender(ro,rendGrab);
                }
            }
          
        }
    }
    private List<RenderObject> GetAllRenderObject()
    {
        List<RenderObject> ros = new List<RenderObject>();
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        Scene scene = SceneManager.GetSceneAt(0);
        var gameObjectRoots = scene.GetRootGameObjects();
        foreach (var gameObject in gameObjectRoots)
        {
            if (gameObject.activeSelf)
            {
                meshFilters.AddRange(gameObject.GetComponentsInChildren<MeshFilter>());
            }
           
        }
        foreach(var meshFilter in meshFilters)
        {
            RenderObject ro = new RenderObject();
            ro.mesh = meshFilter.sharedMesh;
            ro.render = meshFilter.GetComponent<MeshRenderer>();
            ro.shader = meshFilter.GetComponent<ScriptShader>();
          
            if(ro.mesh !=null && ro.render !=null)
            {
                if (ro.shader==null)
                {
                   ro.shader = meshFilter.gameObject.AddComponent<ScriptShader>();
                }
                ros.Add(ro);
            }
        }
        ros.Sort((p, q) => q.shader.RenderQueue.CompareTo(q.shader.RenderQueue));
        return ros;
    }
    private void DrawSkyBox()
    {
        Mesh box = new Mesh();
        Vector3[] vertices =
        {
            new Vector3(1,1,1),
            new Vector3(1,-1,1),
            new Vector3(-1,-1,1),
            new Vector3(-1,1,1),
            new Vector3(1,1,-1),
            new Vector3(1,-1,-1),
            new Vector3(-1,-1,-1),
            new Vector3(-1,1,-1),
        };
        int[] triangles =
        {
            0,1,3,
            1,2,3,
            4,7,5,
            5,7,6,
            4,5,0,
            5,1,0,
            2,6,3,
            3,6,7,
            2,1,6,
            6,1,5,
            0,7,4,
            0,3,7

        };
      
        box.vertices = vertices;
        box.triangles = triangles;

        Matrix4x4 mat = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0),new Vector4( Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z, 1));
        box.name = "skybox";
        SoftGraphics.DrawMesh(box, this.setting.SkyBox.GetComponent<ScriptShader>(), mat); 
    }
    public enum DrawFilterMode
    {
        All,
        Deferred,
        NoDeferred
    }
}
public class RTPool
{
    public Dictionary<string, ScriptRenderTexture> cache = new Dictionary<string, ScriptRenderTexture>();
    public ScriptRenderTexture Get(string name, int width, int height, int depth = 32, TextureFormat format = TextureFormat.ARGB32)
    {
        var rt = new ScriptRenderTexture(width, height, depth, format);
        rt.name = name;
        AddToCache(rt);
        return rt;
    }
    public ScriptRenderTexture Get(int width, int height, int depth = 32, TextureFormat format = TextureFormat.ARGB32)
    {
        var rt = new ScriptRenderTexture(width, height, depth, format);
        AddToCache(rt);
        return rt;
    }
    public ScriptRenderTexture GetDepthRT(string name, int width, int height)
    {
        var rt = new ScriptRenderTexture(width, height);
        rt.name = name;
        AddToCache(rt);
        return rt;
    }
    void AddToCache(ScriptRenderTexture rt)
    {
        int i = 0;
        while (cache.ContainsKey(rt.name))
        {
            i++;
            rt.name += i.ToString();
        }
        cache.Add(rt.name, rt);
    }
    public void ReleaeAll()
    {
        foreach (var item in cache)
        {
            item.Value.Release();
        }
        cache.Clear();
    }
    public void ApplyAll()
    {
        foreach (var item in cache)
        {
            item.Value.Apply();
        }
    }
}