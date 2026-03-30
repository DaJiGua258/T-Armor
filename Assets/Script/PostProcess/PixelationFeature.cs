using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelationFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        [Range(1, 50)] public int pixelSize = 8;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public Settings settings = new Settings();
    PixelationPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new PixelationPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 只有在场景视图或游戏视图中才运行，且材质不能为空
        if (settings.material == null) return;
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass?.Dispose();
    }

    class PixelationPass : ScriptableRenderPass
    {
        Settings settings;
        RTHandle m_TemporaryColorTexture;

        public PixelationPass(Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 获取当前摄像机的渲染描述符
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0; // 后处理不需要深度缓冲

            // 关键：确保 RTHandle 被正确分配/重新分配
            RenderingUtils.ReAllocateIfNeeded(ref m_TemporaryColorTexture, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_TempPixelTex");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.material == null || m_TemporaryColorTexture == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Pixelation Pass");
            
            // 获取当前相机的颜色缓冲
            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // 安全检查：如果 source 无效，跳过执行
            if (source == null || source.rt == null) 
            {
                CommandBufferPool.Release(cmd);
                return;
            }

            // 更新 Shader 参数（_ScreenParams.xy 由 URP 每帧自动注入，无需手动传递）
            settings.material.SetFloat("_PixelSize", settings.pixelSize);

            // 执行两次 Blit：
            // 1. 从相机缓冲 -> 临时缓冲 (应用材质)
            // 2. 从临时缓冲 -> 相机缓冲 (写回结果)
            Blitter.BlitCameraTexture(cmd, source, m_TemporaryColorTexture, settings.material, 0);
            Blitter.BlitCameraTexture(cmd, m_TemporaryColorTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            m_TemporaryColorTexture?.Release();
        }
    }
}