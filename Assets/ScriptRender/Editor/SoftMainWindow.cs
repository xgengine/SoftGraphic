using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SoftMainWindow : EditorWindow
{
    Editor assetEditor;
    ScriptRenderPepeline renderPepeline;
    public SRPRenderAsset renderAsset;
    [MenuItem("Window/SoftMainWindow")]
    static void DoWindow()
    {
        GetWindow<SoftMainWindow>();
    }
    private void OnEnable()
    {
        renderPepeline = renderAsset.CreatePipeline();
    } 
    private void OnGUI()
    {
        GUIRender(renderAsset, renderPepeline);

    }
    Vector2 srollPos = Vector2.zero;
    void GUIRender(SRPRenderAsset asset, ScriptRenderPepeline renderPepeline)
    {
       
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
        srollPos = EditorGUILayout.BeginScrollView(srollPos);
        using (new GUILayout.VerticalScope("box"))
        {
           
          
            foreach(var item in renderPepeline.rtPool.cache)
            {
                EditorGUILayout.LabelField(item.Key);
                var rt = item.Value;
                if (rt.colorBuffer !=null)
                {
                    DrawBuffer(rt.colorBuffer, asset.GraphicType == SoftGraphicType.OpenGL);
                }
                if (rt.depthBuffer != null)
                {
                    DrawBuffer(rt.depthBuffer, asset.GraphicType == SoftGraphicType.OpenGL);
                }
            }
        }
        EditorGUILayout.EndScrollView();
       
    }
    void DrawBuffer(Texture2D tex,bool isGL)
    {
        GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetAspectRect((float)tex.width/ tex.height), tex,new Rect(0,0,1,isGL?1: -1));
    }

}
