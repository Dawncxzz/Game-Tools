using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector;
#endif

public class ShowOverDraw : MonoBehaviour
{
#if UNITY_EDITOR
    public int ignoreCount = 5;
    public float pixel = 0.1f;
    public float defaultPixel = 0.05f;

    private Camera originCamera;
    private Camera copyCamera;

    public void Check()
    {
        if (copyCamera == null)
        {
            originCamera = Camera.main;
            copyCamera = Instantiate(Camera.main);
            copyCamera.CopyFrom(Camera.main);
            DestroyImmediate(copyCamera.GetComponent<ShowOverDraw>());
        }
        originCamera.gameObject.SetActive(false);
        copyCamera.gameObject.SetActive(true);
        copyCamera.GetComponent<UniversalAdditionalCameraData>().SetRenderer(3);
        copyCamera.clearFlags = CameraClearFlags.Color;
        copyCamera.backgroundColor = Color.black;
    }

    public void DoReset()
    {
        originCamera.gameObject.SetActive(true);
        if(copyCamera != null)
            DestroyImmediate(copyCamera.gameObject);
    }

    public void ShowOverdraw() 
    {
        Shader.EnableKeyword("_CHECKSCENE");
        Shader.SetGlobalFloat("_Pixel", pixel);
        OverdrawBlitRenderPass.sceneOverDraw = true;
        Check();
    }
#endif
}
#if UNITY_EDITOR

[CustomEditor(typeof(ShowOverDraw))]
public class ShowOverDraw_Editor : Editor
{

    private ShowOverDraw sm;
    public void OnEnable()
    {
        sm = (ShowOverDraw)target;
        Undo.RegisterCompleteObjectUndo(sm, "ShowOverDraw_Editor");
    }

    public override void OnInspectorGUI()
    {
        sm.ignoreCount = EditorGUILayout.IntField("ignoreCount:", sm.ignoreCount);
        sm.pixel = EditorGUILayout.FloatField("pixel:", sm.pixel);
        sm.defaultPixel = EditorGUILayout.FloatField("defaultPixel:", sm.defaultPixel);
        EditorGUILayout.LabelField("dc: " + (UnityEditor.UnityStats.batches - sm.ignoreCount));
        EditorGUILayout.LabelField("ov: " + OverdrawBlitRenderPass.overdrawAvg.ToString("f2"));
        if (GUILayout.Button("查看SceneOverDraw"))
        {
            sm.ShowOverdraw();
        }
        if (GUILayout.Button("查看EffectOverDraw"))
        {
            Shader.DisableKeyword("_CHECKSCENE");
            Shader.SetGlobalFloat("_Pixel", sm.defaultPixel);
            OverdrawBlitRenderPass.sceneOverDraw = false;
            sm.Check();
        }
        if (GUILayout.Button("还原"))
        {
            sm.DoReset();
        }
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(sm);
        }

    }
#endif

}
