using System;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEngine;
#endif

[ExecuteInEditMode]
public class CameraShaderEffect2 : MonoBehaviour
{
    
    
    private Material _material;
    public Camera cam;
    
    
#if ASDASD    
    
    
    
    /// <summary>
    /// Screen-space Reflections quality presets.
    /// </summary>
    public enum ScreenSpaceReflectionPreset
    {
        /// <summary>
        /// Lowest quality.
        /// </summary>
        Lower,

        /// <summary>
        /// Low quality.
        /// </summary>
        Low,

        /// <summary>
        /// Medium quality.
        /// </summary>
        Medium,

        /// <summary>
        /// High quality.
        /// </summary>
        High,

        /// <summary>
        /// Higher quality.
        /// </summary>
        Higher,

        /// <summary>
        /// Ultra quality.
        /// </summary>
        Ultra,

        /// <summary>
        /// Overkill (as in: don't use) quality.
        /// </summary>
        Overkill,

        /// <summary>
        /// Custom, tweakable quality settings.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Screen-space Reflections buffer sizes.
    /// </summary>
    public enum ScreenSpaceReflectionResolution
    {
        /// <summary>
        /// Downsampled buffer. Faster but lower quality.
        /// </summary>
        Downsampled,

        /// <summary>
        /// Full-sized buffer. Slower but higher quality.
        /// </summary>
        FullSize,

        /// <summary>
        /// Supersampled buffer. Very slow but much higher quality.
        /// </summary>
        Supersampled
    }



    
        /// <summary>
        /// The quality preset to use for rendering. Use <see cref="ScreenSpaceReflectionPreset.Custom"/>
        /// to tweak settings.
        /// </summary>
        [Tooltip("Choose a quality preset, or use \"Custom\" to create your own custom preset. Don't use a preset higher than \"Medium\" if you desire good performance on consoles.")]
        public ScreenSpaceReflectionPreset preset = ScreenSpaceReflectionPreset.Medium ;

        /// <summary>
        /// The maximum number of steps in the raymarching pass. Higher values mean more reflections.
        /// </summary>
        [Range(0, 256), Tooltip("Maximum number of steps in the raymarching pass. Higher values mean more reflections.")]
        public int maximumIterationCount = 16;

        /// <summary>
        /// Changes the size of the internal buffer. Downsample it to maximize performances or
        /// supersample it to get slow but higher quality results.
        /// </summary>
        [Tooltip("Changes the size of the SSR buffer. Downsample it to maximize performances or supersample it for higher quality results with reduced performance.")]
        public ScreenSpaceReflectionResolution resolution = ScreenSpaceReflectionResolution.Downsampled ;

        /// <summary>
        /// The ray thickness. Lower values are more expensive but allow the effect to detect
        /// smaller details.
        /// </summary>
        [Range(1f, 64f), Tooltip("Ray thickness. Lower values are more expensive but allow the effect to detect smaller details.")]
        public float thickness =  8f ;

        /// <summary>
        /// The maximum distance to traverse in the scene after which it will stop drawing
        /// reflections.
        /// </summary>
        [Tooltip("Maximum distance to traverse after which it will stop drawing reflections.")]
        public float maximumMarchDistance = 100f ;

        /// <summary>
        /// Fades reflections close to the near plane. This is useful to hide common artifacts.
        /// </summary>
        [Range(0f, 1f), Tooltip("Fades reflections close to the near planes.")]
        public float distanceFade =  0.5f ;

        /// <summary>
        /// Fades reflections close to the screen edges.
        /// </summary>
        [Range(0f, 1f), Tooltip("Fades reflections close to the screen edges.")]
        public float vignette =  0.5f ;

        /// <summary>
        /// Returns <c>true</c> if the effect is currently enabled and supported.
        /// </summary>
        /// <param name="context">The current post-processing render context</param>
        /// <returns><c>true</c> if the effect is currently enabled and supported</returns>
        public bool IsEnabledAndSupported(Camera cam)
        {
            return enabled
                && cam.actualRenderingPath == RenderingPath.DeferredShading
                && SystemInfo.supportsMotionVectors
                && SystemInfo.supportsComputeShaders
                && SystemInfo.copyTextureSupport > CopyTextureSupport.None;
        }
    


        RenderTexture m_Resolve;
        RenderTexture m_History;
        int[] m_MipIDs;

        class QualityPreset
        {
            public int maximumIterationCount;
            public float thickness;
            public ScreenSpaceReflectionResolution downsampling;
        }

        readonly QualityPreset[] m_Presets =
        {
            new QualityPreset { maximumIterationCount = 10, thickness = 32, downsampling = ScreenSpaceReflectionResolution.Downsampled  }, // Lower
            new QualityPreset { maximumIterationCount = 16, thickness = 32, downsampling = ScreenSpaceReflectionResolution.Downsampled  }, // Low
            new QualityPreset { maximumIterationCount = 32, thickness = 16, downsampling = ScreenSpaceReflectionResolution.Downsampled  }, // Medium
            new QualityPreset { maximumIterationCount = 48, thickness =  8, downsampling = ScreenSpaceReflectionResolution.Downsampled  }, // High
            new QualityPreset { maximumIterationCount = 16, thickness = 32, downsampling = ScreenSpaceReflectionResolution.FullSize }, // Higher
            new QualityPreset { maximumIterationCount = 48, thickness = 16, downsampling = ScreenSpaceReflectionResolution.FullSize }, // Ultra
            new QualityPreset { maximumIterationCount = 128, thickness = 12, downsampling = ScreenSpaceReflectionResolution.Supersampled }, // Overkill
        };

        enum Pass
        {
            Test,
            Resolve,
            Reproject,
            Composite
        }

        public DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }

        internal void CheckRT(ref RenderTexture rt, int width, int height, FilterMode filterMode, bool useMipMap)
        {
            if (rt == null || !rt.IsCreated() || rt.width != width || rt.height != height)
            {
                if (rt != null)
                {
                    rt.Release();
                    Destroy(rt);
                }

                rt = new RenderTexture(width, height, 0, RenderTextureFormat.DefaultHDR)
                {
                    filterMode = filterMode,
                    useMipMap = useMipMap,
                    autoGenerateMips = false,
                    hideFlags = HideFlags.HideAndDontSave
                };

                rt.Create();
            }
        }

        public void Render(Camera cam)
        {
            CommandBuffer cmd = cam.command;
            cmd.BeginSample("Screen-space Reflections");

            // Get quality settings
            if (preset != ScreenSpaceReflectionPreset.Custom)
            {
                int id = (int)preset;
                maximumIterationCount = m_Presets[id].maximumIterationCount;
                thickness = m_Presets[id].thickness;
                resolution = m_Presets[id].downsampling;
            }

            maximumMarchDistance = Mathf.Max(0f, maximumMarchDistance);

            // Square POT target
            int size = Mathf.ClosestPowerOfTwo(Mathf.Min(width, height));

            if (resolution == ScreenSpaceReflectionResolution.Downsampled)
                size >>= 1;
            else if (resolution == ScreenSpaceReflectionResolution.Supersampled)
                size <<= 1;

            // The gaussian pyramid compute works in blocks of 8x8 so make sure the last lod has a
            // minimum size of 8x8
            const int kMaxLods = 12;
            int lodCount = Mathf.FloorToInt(Mathf.Log(size, 2f) - 3f);
            lodCount = Mathf.Min(lodCount, kMaxLods);

            CheckRT(ref m_Resolve, size, size, FilterMode.Trilinear, true);

            Texture2D noiseTex = context.resources.blueNoise256[0];
            PropertySheet sheet = context.propertySheets.Get(context.resources.shaders.screenSpaceReflections);
            _material.SetTexture("_Noise", noiseTex);

            var screenSpaceProjectionMatrix = new Matrix4x4();
            screenSpaceProjectionMatrix.SetRow(0, new Vector4(size * 0.5f, 0f, 0f, size * 0.5f));
            screenSpaceProjectionMatrix.SetRow(1, new Vector4(0f, size * 0.5f, 0f, size * 0.5f));
            screenSpaceProjectionMatrix.SetRow(2, new Vector4(0f, 0f, 1f, 0f));
            screenSpaceProjectionMatrix.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

            var projectionMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
            screenSpaceProjectionMatrix *= projectionMatrix;

            _material.SetMatrix("_ViewMatrix", cam.worldToCameraMatrix);
            _material.SetMatrix("_InverseViewMatrix", cam.worldToCameraMatrix.inverse);
            _material.SetMatrix("_ScreenSpaceProjectionMatrix", screenSpaceProjectionMatrix);
            _material.SetVector("_Params", new Vector4((float)vignette, distanceFade, maximumMarchDistance, lodCount));
            _material.SetVector("_Params2", new Vector4((float)width / (float)height, (float)size / (float)noiseTex.width, thickness, maximumIterationCount));

            cmd.GetTemporaryRT("_Test", size, size, 0, FilterMode.Point, context.sourceFormat);
            cmd.BlitFullscreenTriangle(context.source, "_Test", sheet, (int)Pass.Test);

            if (context.isSceneView)
            {
                cmd.BlitFullscreenTriangle(context.source, m_Resolve, sheet, (int)Pass.Resolve);
            }
            else
            {
                CheckRT(ref m_History, size, size, FilterMode.Bilinear, false);

                if (m_ResetHistory)
                {
                    context.command.BlitFullscreenTriangle(context.source, m_History);
                    m_ResetHistory = false;
                }

                cmd.GetTemporaryRT("_SSRResolveTemp", size, size, 0, FilterMode.Bilinear, sourceFormat);
                cmd.BlitFullscreenTriangle(context.source, "_SSRResolveTemp", sheet, (int)Pass.Resolve);

                _material.SetTexture("_History", m_History);
                cmd.BlitFullscreenTriangle("_SSRResolveTemp", m_Resolve, sheet, (int)Pass.Reproject);

                cmd.CopyTexture(m_Resolve, 0, 0, m_History, 0, 0);

                cmd.ReleaseTemporaryRT("_SSRResolveTemp");
            }

            cmd.ReleaseTemporaryRT("_Test");

            // Pre-cache mipmaps ids
            if (m_MipIDs == null || m_MipIDs.Length == 0)
            {
                m_MipIDs = new int[kMaxLods];

                for (int i = 0; i < kMaxLods; i++)
                    m_MipIDs[i] = Shader.PropertyToID("_SSRGaussianMip" + i);
            }

            ComputeShader compute = context.resources.computeShaders.gaussianDownsample;
            int kernel = compute.FindKernel("KMain");
            RenderTextureFormat mipFormat = RenderTextureFormat.DefaultHDR;;

            var last = new RenderTargetIdentifier(m_Resolve);

            for (int i = 0; i < lodCount; i++)
            {
                size >>= 1;
                Assert.IsTrue(size > 0);

                cmd.GetTemporaryRT(m_MipIDs[i], size, size, 0, FilterMode.Bilinear, mipFormat, RenderTextureReadWrite.Default, 1, true);
                cmd.SetComputeTextureParam(compute, kernel, "_Source", last);
                cmd.SetComputeTextureParam(compute, kernel, "_Result", m_MipIDs[i]);
                cmd.SetComputeVectorParam(compute, "_Size", new Vector4(size, size, 1f / size, 1f / size));
                cmd.DispatchCompute(compute, kernel, size / 8, size / 8, 1);
                cmd.CopyTexture(m_MipIDs[i], 0, 0, m_Resolve, 0, i + 1);

                last = m_MipIDs[i];
            }

            for (int i = 0; i < lodCount; i++)
                cmd.ReleaseTemporaryRT(m_MipIDs[i]);

            _material.SetTexture("_Resolve", m_Resolve);
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.Composite, preserveDepth: true);
            cmd.EndSample("Screen-space Reflections");
        }

        public void Release()
        {
            Destroy(m_Resolve);
            Destroy(m_History);
            m_Resolve = null;
            m_History = null;
        }
        
        
        
        
        
        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, PropertySheet propertySheet, int pass, bool clear = false, Rect? viewport = null, bool preserveDepth = false)
        {
            cmd.BlitFullscreenTriangle(source, destination, propertySheet, pass, clear ? LoadAction.Clear : LoadAction.DontCare, viewport, preserveDepth);

        }
        
        
        
        /// <summary>
        /// Blits a fullscreen triangle using a given material.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="propertySheet">The property sheet to use</param>
        /// <param name="pass">The pass from the material to use</param>
        /// <param name="loadAction">The load action for this blit</param>
        /// <param name="viewport">An optional viewport to consider for the blit</param>
        /// <param name="preserveDepth">Should the depth buffer be preserved?</param>
        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, PropertySheet propertySheet, int pass, RenderBufferLoadAction loadAction, Rect? viewport = null, bool preserveDepth = false)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
#if UNITY_2018_2_OR_NEWER
            bool clear = (loadAction == LoadAction.Clear);
            if (clear)
                loadAction = LoadAction.DontCare;
#else
            bool clear = false;
#endif
            if (viewport != null)
                loadAction = LoadAction.Load;

            cmd.SetRenderTargetWithLoadStoreAction(destination, loadAction, StoreAction.Store, preserveDepth ? LoadAction.Load : loadAction, StoreAction.Store);

            if (viewport != null)
                cmd.SetViewport(viewport.Value);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }  
        public static void SetRenderTargetWithLoadStoreAction(this CommandBuffer cmd, RenderTargetIdentifier rt, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction)
        {
#if UNITY_2018_2_OR_NEWER
            cmd.SetRenderTarget(rt, loadAction, storeAction);
#else
            cmd.SetRenderTarget(rt);
#endif
        }
    }


    #endif
}
