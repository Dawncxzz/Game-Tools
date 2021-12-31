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
    [MenuItem("GameEditor/替换lightMap")]
    public static void ShowWindow()
    {
        window = (ReplaceLightmap)GetWindow(typeof(ReplaceLightmap), false);
        window.titleContent = new GUIContent("替换lightMap");
        window.maxSize = new Vector2(600, 800);
    }

    public void OnGUI()
    {
        meshRenderer = EditorGUILayout.ObjectField("模型MeshRenderer:", meshRenderer, typeof(MeshRenderer), true) as MeshRenderer;
        modelMap = EditorGUILayout.ObjectField("模型LightMap贴图:", modelMap, typeof(Texture2D), true) as Texture2D;
        newMap = EditorGUILayout.ObjectField("新的LightMap贴图:", newMap, typeof(Texture2D), true) as Texture2D;

        if (GUILayout.Button("替换局部lightMap"))
        {
            if (!meshRenderer || !modelMap)
            {
                Debug.LogError("模型MeshRenderer和模型LightMap贴图不能为空");
                return;
            }
            TexelReplace();
        }
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
            if (ti.isReadable && ti.sRGBTexture == sRGB && setting.format == TextureImporterFormat.RGBA32) return;
            setting.overridden = true;
            setting.format = TextureImporterFormat.RGBA32;
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

    public static void SetTexNoReable(Texture2D tex)
    {
        TextureImportSetting.enabled = true;
        TextureImporter ti = TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        if (ti != null)
        {
            var setting = ti.GetPlatformTextureSettings("Standalone");
            setting.overridden = true;
            ti.SetPlatformTextureSettings(setting);
            ti.isReadable = false;
            ti.SaveAndReimport();
        }
    }

    public void LightMapReplace()
    {
        Init();
        SetTexReable(lightmapData.lightmapColor, true);
        lightmapData.lightmapColor = newMap;
        LightmapSettings.lightmaps[meshRenderer.lightmapIndex] = lightmapData;
        SetTexNoReable(lightmapData.lightmapColor);
    }
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

        Texture2D newLightMap = new Texture2D((int)wh.x, (int)wh.y, TextureFormat.RGBA32, true);
        Color[] colors = lightmapData.lightmapColor.GetPixels();
        SetTexReable(newLightMap, true);
        
        //for (int i = 0; i < colors.Length; i++)
        //{
        //    //Debug.Log(colors[i]);

        //    colors[i] = DecodeRGBM(colors[i]);
        //    //colors[i] = DecodeHDR(colors[i], new Vector4(1, 1, 0, 0));
        //    //colors[i] = UnpackLightmapRGBM(colors[i], new Vector4(1, 1, 0, 0));
        //    //colors[i] = EncodeRGBM(new Vector3(colors[i].r, colors[i].g, colors[i].b));
        //}
        newLightMap.SetPixels(colors);


        for (float i = lightMapScaleOffset.z * wh.x; i < lightMapScaleOffset.x * wh.x; i++)
        {
            for (float j = lightMapScaleOffset.w * wh.y; j < lightMapScaleOffset.y * wh.y; j++)
            {
                Color color = modelMap.GetPixelBilinear(i / (lightMapScaleOffset.x * wh.x), j / (lightMapScaleOffset.y * wh.y));
                newLightMap.SetPixel((int)i, (int)j, color);
            }
        }
        File.WriteAllBytes(directoryPath + "/" + fileName + "copy" + type, newLightMap.EncodeToTGA());
        SetTexNoReable(lightmapData.lightmapColor);
        SetTexNoReable(newLightMap);
        SetTexNoReable(modelMap);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static float kRGBMRange = 8.0f;
    public Vector4 EncodeRGBM(Vector3 color)
    {
        color *= 1.0f / kRGBMRange;
        float m = Math.Max(Math.Max(color.x, color.y), Math.Max(color.z, 1e-5f));
        m = (float)Math.Ceiling((m * 255)) / 255;
        Vector3 rgb = new Vector3(color.x, color.y, color.z) / m;
        return new Vector4(rgb.x, rgb.y, rgb.z, m);
    }
    public Vector4 DecodeRGBM(Vector4 rgbm)
    {
        return new Vector4(rgbm.x * rgbm.w * kRGBMRange, rgbm.y * rgbm.w * kRGBMRange, rgbm.z * rgbm.w * kRGBMRange, rgbm.w);
    }

    public Vector4 EncodeHDR(Vector4 rgba)
    {
        Vector4 ouput = rgba;
        if (rgba[3] == 0)                                           // 指数位是0, rgb都是0
        {
            ouput[0] = 0.0f;
            ouput[1] = 0.0f;
            ouput[2] = 0.0f;
        }
        else
        {
            int E = (int)rgba[3] - 128 - 8;                   // 指数位的值
            double P = Math.Pow(2.0, E);                         // 2的E次幂的结果
            ouput[0] = (float)((double)rgba[0] * P);                  // 计算三个通道的值
            ouput[1] = (float)((double)rgba[1] * P);
            ouput[2] = (float)((double)rgba[2] * P);
        }
        return ouput;
    }

    Vector4 DecodeHDR(Color rgbmInput, Vector4 decodeInstructions)
    {
        float alpha = (float)(decodeInstructions.w * (rgbmInput.a - 1.0) + 1.0);
        return new Vector4(rgbmInput.r * (float)(Math.Pow(alpha, decodeInstructions.y) * decodeInstructions.x)
            , rgbmInput.g * (float)(Math.Pow(alpha, decodeInstructions.y) * decodeInstructions.x)
            , rgbmInput.b * (float)(Math.Pow(alpha, decodeInstructions.y) * decodeInstructions.x), rgbmInput.a);
    }

    Vector4 UnpackLightmapRGBM(Color rgbmInput, Vector4 decodeInstructions)
    {
        return new Vector4(rgbmInput.r * (float)(Math.Pow(rgbmInput.a, decodeInstructions.y) * decodeInstructions.x)
            , rgbmInput.g * (float)(Math.Pow(rgbmInput.a, decodeInstructions.y) * decodeInstructions.x)
            , rgbmInput.b * (float)(Math.Pow(rgbmInput.a, decodeInstructions.y) * decodeInstructions.x), rgbmInput.a) ;

    }
}
