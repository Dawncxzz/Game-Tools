using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
public class ReplaceLightmap : EditorWindow
{
    MeshRenderer meshRenderer;
    Texture2D modelMap;
    Texture2D newMap;
    LightmapData lightmapData;

    private static ReplaceLightmap window;
    [MenuItem("Art Tools/替换lightMap")]
    public static void ShowWindow()
    {
        window = (ReplaceLightmap)GetWindow(typeof(ReplaceLightmap), false);
        window.titleContent = new GUIContent("替换lightMap");
        window.maxSize = new Vector2(600, 800);
    }

    public void OnGUI()
    {
        meshRenderer = EditorGUILayout.ObjectField("模型MeshRenderer: ", meshRenderer, typeof(MeshRenderer), true) as MeshRenderer;

        modelMap = EditorGUILayout.ObjectField("模型LightMap贴图:", modelMap, typeof(Texture2D), true) as Texture2D;

        if (GUILayout.Button("替换局部lightMap"))
        {
            if (!meshRenderer || !modelMap)
            {
                Debug.LogError("模型MeshRenderer和模型LightMap贴图不能为空");
                return;
            }
            TexelReplace();
        }

        newMap = EditorGUILayout.ObjectField("新的LightMap贴图:", newMap, typeof(Texture2D), true) as Texture2D;
        if (GUILayout.Button("替换全局lightMap"))
        {
            if (!meshRenderer || !newMap)
            {
                Debug.LogError("模型LightMap贴图和新的LightMap贴图不能为空");
                return;
            }
            LightMapReplace();
        }
    }
    public void Init()
    {
        lightmapData = LightmapSettings.lightmaps[meshRenderer.lightmapIndex];
    }
    public static void SetTexReable(Texture2D tex, bool sRGB = true, bool nearSize = false)
    {
        TextureImportSetting.enabled = false;
        TextureImporter ti = TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        if (ti != null)
        {
            var setting = ti.GetPlatformTextureSettings("Standalone");
            if (ti.isReadable && ti.sRGBTexture == sRGB && setting.format == TextureImporterFormat.RGBAHalf) return;
            Debug.Log(tex);
            //setting.overridden = true;
            setting.format = TextureImporterFormat.RGBAHalf;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = sRGB;
            ti.SetPlatformTextureSettings(setting);
            ti.isReadable = true;
            if (nearSize)
            {
                ti.npotScale = TextureImporterNPOTScale.None;
            }
            ti.SaveAndReimport();
        }
    }

    public static void SetTexNoReable(Texture2D tex, TextureImporterType textureImporterType)
    {
        TextureImportSetting.enabled = true;
        TextureImporter ti = TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        if (ti != null)
        {
            var setting = ti.GetPlatformTextureSettings("Standalone");
            //setting.overridden = true;
            setting.format = TextureImporterFormat.DXT5;
            ti.SetPlatformTextureSettings(setting);
            ti.isReadable = false;
            ti.textureType = textureImporterType;
            ti.SaveAndReimport();
        }
    }

    //全局替换
    public void LightMapReplace()
    {
        Init();
        //SetTexReable(lightmapData.lightmapColor, true);
        //SetTexReable(newMap, true);
        LightmapData[] newLightmapData = LightmapSettings.lightmaps;
        newLightmapData[meshRenderer.lightmapIndex].lightmapColor = newMap;
        LightmapSettings.lightmaps = newLightmapData;
        SetTexNoReable(newMap, TextureImporterType.Lightmap);
    }

    //局部替换
    public void TexelReplace()
    {
        Init();
        SetTexReable(lightmapData.lightmapColor, true);
        SetTexReable(modelMap, true);

        string filePath = AssetDatabase.GetAssetPath(lightmapData.lightmapColor);
        string directoryPath = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string type = Path.GetExtension(filePath);
        Vector2 wh = new Vector2(lightmapData.lightmapColor.width, lightmapData.lightmapColor.height);
        Vector4 lightMapScaleOffset = new Vector4(meshRenderer.lightmapScaleOffset.x, meshRenderer.lightmapScaleOffset.y, meshRenderer.lightmapScaleOffset.z, meshRenderer.lightmapScaleOffset.w);

        Texture2D newLightMap = new Texture2D((int)wh.x, (int)wh.y, TextureFormat.RGBAHalf, true);
        Color[] colors = lightmapData.lightmapColor.GetPixels();
        newLightMap.SetPixels(colors);

        //File.WriteAllBytes(directoryPath + "/" + fileName + "copy" + type, File.ReadAllBytes(filePath));
        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();
        //Texture2D newLightMap = (Texture2D)AssetDatabase.LoadAssetAtPath(directoryPath + "/" + fileName + "copy" + type, typeof(Texture2D));

        SetTexReable(newLightMap, true);
        for (float i = lightMapScaleOffset.z * wh.x; i < lightMapScaleOffset.x * wh.x; i++)
        {
            for (float j = lightMapScaleOffset.w * wh.y; j < lightMapScaleOffset.y * wh.y; j++)
            {
                Color color = modelMap.GetPixelBilinear(i / (lightMapScaleOffset.x * wh.x), j / (lightMapScaleOffset.y * wh.y));
                if(color.a != 0)
                    newLightMap.SetPixel((int)i, (int)j, color);
            }
        }
        File.WriteAllBytes(directoryPath + "/" + fileName + "copy_final.exr", newLightMap.EncodeToEXR());
        //File.WriteAllBytes(directoryPath + "/" + fileName + "copy" + type, File.ReadAllBytes(filePath));
        //SetTexNoReable(lightmapData.lightmapColor, TextureImporterType.Lightmap);
        //SetTexNoReable(newLightMap, TextureImporterType.Lightmap);
        //SetTexNoReable(modelMap, TextureImporterType.Default);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
