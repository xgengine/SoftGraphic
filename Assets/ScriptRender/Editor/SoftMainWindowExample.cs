using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class SoftMainWindowExample : EditorWindow
{
    public SRPRenderAsset renderAsset1;
    public SRPRenderAsset renderAsset2;
    public SRPRenderAsset[] renderAssets;
    ScriptRenderPepeline[] renderPepelines;
    List<List<Matrix4x4>> matrixs = new List<List<Matrix4x4>>();
    Editor[] assetEditor;
    [MenuItem("Window/SoftMainWindowTwo")]
    static void DoWindow()
    {
        GetWindow<SoftMainWindowExample>();
    }
    private void OnEnable()
    {
        renderAssets = new SRPRenderAsset[2];
        renderAssets[0] = renderAsset1;
        renderAssets[1] = renderAsset2;
        renderPepelines = new ScriptRenderPepeline[2];
        assetEditor = new Editor[2];
        for(int i=0;i<renderAssets.Length;i++)
        {
            renderPepelines[i] = renderAssets[i].CreatePipeline();
            matrixs.Add(new List<Matrix4x4>());
        }
    }   
    private void OnGUI()
    {
        srollPos = EditorGUILayout.BeginScrollView(srollPos);
        EditorGUILayout.BeginVertical();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUIRender(renderAssets[0], renderPepelines[0], ref assetEditor[0],0);
            EditorGUILayout.Space();
            GUIRender(renderAssets[1], renderPepelines[1], ref assetEditor[1],1);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }
    Vector2 srollPos = Vector2.zero;
    void GUIRender(SRPRenderAsset asset,ScriptRenderPepeline renderPepeline,ref Editor assetEditor,int index)
    {
        EditorGUILayout.BeginVertical();
        asset = (SRPRenderAsset)EditorGUILayout.ObjectField(asset, typeof(SRPRenderAsset), false);
        if (asset != null)
        {
            Editor.CreateCachedEditor(asset, typeof(Editor), ref assetEditor);
            assetEditor.OnInspectorGUI();
        }
        if (GUILayout.Button("Render", EditorStyles.miniButton))
        {
            renderPepeline.RenderScene();
        }
        using (new GUILayout.VerticalScope("box"))
        {
            
            foreach (var item in renderPepeline.rtPool.cache)
            {
                EditorGUILayout.LabelField(item.Key);
                var rt = item.Value;
                if (rt.colorBuffer != null)
                {
                    DrawBuffer(rt.colorBuffer, asset.GraphicType == SoftGraphicType.OpenGL);
                }
                if (rt.depthBuffer != null)
                {
                    DrawBuffer(rt.depthBuffer, asset.GraphicType == SoftGraphicType.OpenGL);
                }
            }
        }
        EditorGUILayout.EndVertical();
       
    }
    private void OnInspectorUpdate()
    {
        this.Repaint();
    }
    void DrawBuffer(Texture2D tex,bool isGL)
    {
    
        GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetAspectRect((float)tex.width/ tex.height), tex,new Rect(0,0,1,isGL?1: -1));
    }
    void GUIMatrix(Matrix4x4 matrix)
    {
        using (new GUILayout.VerticalScope())
        {
            for (int i = 0; i < 4; i++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    for (int j = 0; j < 4; j++)
                    {
                        EditorGUILayout.LabelField(matrix[i, j].ToString("f3"), GUILayout.Width(60));
                    }
                }

            }
        }
           
    }

}
