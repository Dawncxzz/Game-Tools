using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ReplaceTex : EditorWindow
{
    private Texture2D texture2D;
    private static bool isReplace = false;

    private static ReplaceTex window;
    [MenuItem("Art Tools/��ͼ�滻")]
    public static void ShowWindow()
    {
        window = (ReplaceTex)GetWindow(typeof(ReplaceTex), false);
        window.titleContent = new GUIContent("��ͼ�滻");
        window.maxSize = new Vector2(600, 800);
        window.Show();
    }

    public void OnGUI()
    {
        texture2D = (Texture2D)EditorGUILayout.ObjectField(texture2D, typeof(Texture2D), true, GUILayout.Width(200));
        
        if (!isReplace)
        {
            if (GUILayout.Button("�滻����"))
            {
                if (!texture2D)
                {
                    Debug.LogError("��ͼ����Ϊ��");
                    return;
                }
                isReplace = !isReplace;
                Shader.SetGlobalTexture("_ReplaceTex", texture2D);
                Shader.EnableKeyword("_REPLACETEX");
            }
        }
        else
        {
            if (GUILayout.Button("��ԭ����"))
            {
                isReplace = !isReplace;
                Shader.DisableKeyword("_REPLACETEX");
            }
        }
    }
}
