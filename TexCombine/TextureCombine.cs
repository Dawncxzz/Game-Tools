using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.IO;
using UnityEditor;
using UnityEngine;
using static TexCombineData;
using System.Collections.Generic;
public class TextureCombine : OdinEditorWindow
{
    public enum TextureSizeType
    {
        _2048 = 2048,
        _1024 = 1024,
        _512 = 512,
        _256 = 256,
        _128 = 128,
        _64 = 64,
        _32 = 32,
        _16 = 16,
    }


    private TexCombineData[] texCombineData = new TexCombineData[4];

    [MenuItem("Art Tools/图片/贴图工具")]
    public static void ShowWindow()
    {
        TextureCombine window = OdinEditorWindow.GetWindow<TextureCombine>();
        window.minSize = new Vector2(550, 500);
        window.maxSize = new Vector2(700, 600);
        window.titleContent = new GUIContent("图片贴图合并");
        window.Show();
        window.tex1 = Selection.activeObject as Texture2D;
        window.tex2 = Selection.activeObject as Texture2D;
        window.tex3 = Selection.activeObject as Texture2D;
        window.tex4 = Selection.activeObject as Texture2D;

    }
    [LabelWidth(100f), LabelText("贴图像素大小")]
    public int pixelSize = 256;


    [LabelWidth(100f), LabelText("贴图名字")]
    public string texName = "特效1";


    [TitleGroup("图片的R通道", horizontalLine: false)]

    [LabelText("贴图1"), LabelWidth(100f), HorizontalGroup("图片的R通道/Horizontal", PaddingRight = 20)]
    public Texture2D tex1;
    [LabelText("贴图1通道"), LabelWidth(100f), HorizontalGroup("图片的R通道/Horizontal")]
    public Channel tex1Channel1 = Channel.R;


    [TitleGroup("图片的G通道", horizontalLine: false)]

    [LabelText("贴图2"), LabelWidth(100f), HorizontalGroup("图片的G通道/Horizontal", PaddingRight = 20)]
    public Texture2D tex2;
    [LabelText("贴图2通道"), LabelWidth(100f), HorizontalGroup("图片的G通道/Horizontal")]
    public Channel tex1Channel2 = Channel.G;


    [TitleGroup("图片的B通道", horizontalLine: false)]

    [LabelText("贴图3"), LabelWidth(100f), HorizontalGroup("图片的B通道/Horizontal", PaddingRight = 20)]
    public Texture2D tex3;
    [LabelText("贴图3通道"), LabelWidth(100f), HorizontalGroup("图片的B通道/Horizontal")]
    public Channel tex1Channel3 = Channel.B;


    [TitleGroup("图片的A通道", horizontalLine: false)]

    [LabelText("贴图4"), LabelWidth(100f), HorizontalGroup("图片的A通道/Horizontal", PaddingRight = 20)]
    public Texture2D tex4;
    [LabelText("贴图4通道"), LabelWidth(100f), HorizontalGroup("图片的A通道/Horizontal")]
    public Channel tex1Channel4 = Channel.A;

    [Button("贴图合并", ButtonStyle.CompactBox), PropertySpace(20, 20)]
    private void TexCombine()
    {
        DoTexCombine();
    }


    [FoldoutGroup("序列帧", 1)]
    public int _row = 4;
    [FoldoutGroup("序列帧", 1)]
    public int _col = 4;
    [FoldoutGroup("序列帧", 1)]
    public List<Texture2D> _texList;
    [FoldoutGroup("序列帧", 1)]
    [Button("贴图合并生成序列帧", ButtonStyle.CompactBox), PropertySpace(20, 20)]
    public void DoTextureSequence()
    {
        TextureSequence(_row, _col, _texList);
    }

    public static Texture2D TextureSequence(int row,int col,List<Texture2D> texList)
    {
        int wsize = texList[0].width;
        int hsize = texList[0].height;

        int count = row * col;

        foreach (var ttt in texList)
        {
            GToolUtil.ChangeTexReadable(ttt, false,true);
        }

        Texture2D tex = new Texture2D(col * wsize, row * hsize, TextureFormat.RGBA32, false);
        if (texList.Count >= count)
        {
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    int index = i * col + j;
                    int startx = j * wsize;
                    int starty = i * hsize;
                    Texture2D tex_index = texList[index];

                    for (int w = 0; w < wsize; w++)
                    {
                        for (int h = 0; h < hsize; h++)
                        {
                            tex.SetPixel(startx + w, starty + h, tex_index.GetPixel(w, h));
                        }
                    }
                }
            }
        }
        tex.Apply();
        string path = EditorUtility.SaveFilePanel("save path", "Assets/Art_test/map/cloud/png/", "clouds", "png");
        if (path != null)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            path = path.Replace(Application.dataPath, "Assets/");
            Texture2D finalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            GToolUtil.ChangeTexReadable(finalTex, false,true);
            Selection.activeObject = finalTex;
            return finalTex;
        }
        return null;
    }


    [FoldoutGroup("贴图缩放", 2)]
    public TextureSizeType sizeType = TextureSizeType._512;
    [FoldoutGroup("贴图缩放", 2)]
    public Texture2D scaleTexture;
    [FoldoutGroup("贴图缩放", 2)]
    [LabelText("是否覆盖原来贴图")]
    public bool sacleOverride;

    [FoldoutGroup("贴图缩放", 2)]
    [Button("贴图缩放", ButtonStyle.CompactBox), PropertySpace(20, 20)]
    private void TextureScale()
    {
        GToolUtil.ChangeTexReadable(scaleTexture, false);
        int size = (int)sizeType;
        Texture2D newTex = new Texture2D(size, size);
        if (scaleTexture.width <= size) return;
        for (int w = 0; w < size; w++) 
        {
            for (int h = 0; h < size; h++) 
            {
                float u = (float)w / (float)size;
                float v = (float)h / (float)size;
                Color c = scaleTexture.GetPixelBilinear(u, v);
                newTex.SetPixel(w, h, c);
            }
        }
        newTex.Apply();
        string path;
        if (sacleOverride)
        {
            path = AssetDatabase.GetAssetPath(scaleTexture);
        }
        else
        {
            path = EditorUtility.SaveFilePanel("save path", "Assets", scaleTexture.name, "tga");
        }
       
        if (path != null)
        {
            if (path.EndsWith(".tga"))
            {
                File.WriteAllBytes(path, newTex.EncodeToTGA());
            }
            else 
            {
                File.WriteAllBytes(path, newTex.EncodeToPNG());
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        GToolUtil.ChangeTexNoReadable(scaleTexture);
    }


    private void DoTexCombine()
    {
        texCombineData[0] = new TexCombineData(tex1, tex1Channel1);
        texCombineData[1] = new TexCombineData(tex2, tex1Channel2);
        texCombineData[2] = new TexCombineData(tex3, tex1Channel3);
        texCombineData[3] = new TexCombineData(tex4, tex1Channel4);
        Color color = new Color(0,0,0,0);
        if (texCombineData != null)
        {
            for (int i = 0; i < 4; i++)
            {
                GToolUtil.ChangeTexReadable(texCombineData[i].tex, true);
            }
            Texture2D newTex = new Texture2D(pixelSize, pixelSize);
            for (int i = 0; i < pixelSize; i++)
            {
                for (int j = 0; j < pixelSize; j++)
                {
                    color = new Color(0, 0, 0, 0);
                    for (int k = 0; k < texCombineData.Length; k++)
                    {

                        switch (texCombineData[k].channel)
                        {
                            case Channel.R:
                                SetChannelColor(ref color, texCombineData[k].tex.GetPixelBilinear((float)i / pixelSize, (float)j / pixelSize).r, k);
                                break;
                            case Channel.G:
                                SetChannelColor(ref color, texCombineData[k].tex.GetPixelBilinear((float)i / pixelSize, (float)j / pixelSize).g, k);
                                break;
                            case Channel.B:
                                SetChannelColor(ref color, texCombineData[k].tex.GetPixelBilinear((float)i / pixelSize, (float)j / pixelSize).b, k);
                                break;
                            case Channel.A:
                                SetChannelColor(ref color, texCombineData[k].tex.GetPixelBilinear((float)i / pixelSize, (float)j / pixelSize).a, k);
                                break;
                            default:
                                break;
                        }
                    }
                    newTex.SetPixel(i, j, color);
                }
            }

            newTex.Apply();
            byte[] bytes = EncodeToTGAExtension.EncodeToTGA(newTex);
            string oldPath = AssetDatabase.GetAssetPath(texCombineData[0].tex);
            string newPath = oldPath.Replace(Path.GetFileNameWithoutExtension(oldPath), texName);
            File.WriteAllBytes(newPath, bytes);
            Selection.activeObject = newTex;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }





    private void SetChannelColor(ref Color color, float color2, int channel)
    {
        switch (channel)
        {
            case 0:
                color.r = color2;
                break;
            case 1:
                color.g = color2;
                break;
            case 2:
                color.b = color2;
                break;
            case 3:
                color.a = color2;
                break;
            default:
                break;
        }
    }
}



public class TexCombineData
{
    public TexCombineData(Texture2D tex, Channel channel)
    {
        this.tex = tex;
        this.channel = channel;
    }
    public TexCombineData()
    {
        this.tex = null;
        this.channel = Channel.R;
    }
    public Texture2D tex;
    public enum Channel
    {
        R, 
        G,
        B,
        A
    }
    public Channel channel;
}
