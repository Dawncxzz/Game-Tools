#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.IO;
public class OverdrawBlitRenderPass : ScriptableRenderPass
{
    public static bool sceneOverDraw;
    public static float overdrawAvg;

    public enum RenderTarget
    {
        Color,
        RenderTexture,
    }

    public Material blitMaterial = null;
    public FilterMode filterMode { get; set; }

    private RenderTargetIdentifier source { get; set; }
    private RenderTargetHandle destination { get; set; }

    private bool overdrawAnalyzeLog { get; set; }
    RenderTargetHandle m_ovredrawRTHandle;
    public RenderTexture m_ovredrawRTTexture;

    string m_ProfilerTag = "OverdrawRender";
    string overdrawSavePath = "Assets/Arts/overdraw.png";
    CameraData camdata;

    Vector2 lastScreen = Vector2.zero;

    /// <summary>
    /// Create the CopyColorPass
    /// </summary>
    public OverdrawBlitRenderPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
        this.blitMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Art/Tool/OverDraw-GammaTLiner"));
        m_ovredrawRTHandle.Init("_OvewdrawTmpColorTexture");
    }

    /// <summary>
    /// Configure the pass with the source and destination to execute on.
    /// </summary>
    /// <param name="source">Source Render Target</param>
    /// <param name="destination">Destination Render Target</param>
    public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, bool analyze)
    {
        this.source = source;
        this.destination = destination;
        this.overdrawAnalyzeLog = analyze;
    }

    /// <inheritdoc/>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

        camdata = renderingData.cameraData;

        RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
        opaqueDesc.depthBufferBits = 0;

        if( m_ovredrawRTTexture == null || (new Vector2(opaqueDesc.width,opaqueDesc.height) != lastScreen))
        {
            m_ovredrawRTTexture = new RenderTexture(opaqueDesc.width, opaqueDesc.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            m_ovredrawRTTexture.name = "overdrawRT";
            lastScreen = new Vector2(opaqueDesc.width, opaqueDesc.height);
        }
        //cmd.GetTemporaryRT(m_ovredrawRTHandle.id, opaqueDesc, filterMode);
        //Blit(cmd, source, m_ovredrawRTHandle.Identifier());
        cmd.Blit(source, m_ovredrawRTTexture);
        //cmd.Blit(m_ovredrawRTTexture, m_ovredrawRTHandle.Identifier());
        AnalyzeOverdrawInfo();
        Blit(cmd, m_ovredrawRTTexture, source, blitMaterial,0);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);

    }

    /// <inheritdoc/>
    public override void FrameCleanup(CommandBuffer cmd)
    {
       cmd.ReleaseTemporaryRT(m_ovredrawRTHandle.id);
    }

    Texture2D tex;
    Vector2 lastTexSize = Vector2.zero;
    void AnalyzeOverdrawInfo()
    {
        UniversalRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        float renderscale = asset.renderScale;
        int width = (int)(camdata.camera.pixelWidth * renderscale);
        int height = (int)(camdata.camera.pixelHeight * renderscale);
        if (tex == null || lastTexSize != new Vector2(width, height)) 
        {
            tex = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
            lastTexSize = new Vector2(width, height);
        }
        RenderTexture old = RenderTexture.active;
        RenderTexture.active = m_ovredrawRTTexture;

        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();


        float totalValue = 0;

        float pixel = Shader.IsKeywordEnabled("_CHECKSCENE")?0.1f:0.05f;
        Color[] colors = tex.GetPixels();
        if (sceneOverDraw)
        {
            foreach (var c in colors)
            {
                totalValue += c.r;
            }
        }
        else
        {
            foreach (var c in colors)
            {
                totalValue += c.r;
            }
        }
        
        overdrawAvg = (totalValue / pixel) / (width * height);
    }
    //void SaveOverDrawRT()
    //{
    //    int width = 1920;// m_ovredrawRTTexture.width;
    //    int height = 1080;// m_ovredrawRTTexture.height;
    //    RenderTexture old = RenderTexture.active;
    //    RenderTexture.active = m_ovredrawRTTexture;
    //    Texture2D saveTex = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
    //    saveTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    //    saveTex.Apply();
    //    if (!Directory.Exists(Path.GetDirectoryName(overdrawSavePath)))
    //    {
    //        Directory.CreateDirectory(Path.GetDirectoryName(overdrawSavePath));
    //    }
    //    File.WriteAllBytes(overdrawSavePath, saveTex.EncodeToPNG());

    //    AssetDatabase.SaveAssets();
    //    AssetDatabase.Refresh();
    //    RenderTexture.active = old;

    //    Texture2D.DestroyImmediate(saveTex);
    //    saveTex = null;

    //    TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath(overdrawSavePath);
    //    ti.isReadable = true;
    //    ti.mipmapEnabled = false;
    //    ti.npotScale = TextureImporterNPOTScale.None;
    //    ti.textureCompression = TextureImporterCompression.Uncompressed;
    //    AssetDatabase.ImportAsset(overdrawSavePath, ImportAssetOptions.ForceUpdate);
    //}
}
#endif