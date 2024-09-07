using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RendererFeatures
{
    public class CameraToTextureRenderPassFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private RenderTexture renderTexture;
    
        [SerializeField]
        private Material material;
        
        [SerializeField]
        private string globalTextureId = "_GlobalCameraTexture";

        [SerializeField] 
        private string cameraTag;
        
        [SerializeField]
        private CameraType cameraType = CameraType.Game;
        
        [SerializeField]
        private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        
        private CustomRenderPass _scriptablePass;
        
        private class CustomRenderPass : ScriptableRenderPass
        {
            private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("CameraToTextureRender");
            private RTHandle _sourceHandle;
            private RTHandle _destinationHandle;
            private readonly Material _material;
            private readonly string _cameraTag;
            private readonly CameraType _cameraType;
        
            public CustomRenderPass(RenderTexture renderTexture, Material material, CameraType cameraType, string cameraTag)
            {
                _destinationHandle = RTHandles.Alloc(renderTexture);
                _material = material;
                _cameraType = cameraType;
                _cameraTag = cameraTag;
            }

            public void SetNewTexture(RenderTexture renderTexture)
            {
                if (renderTexture != null)
                {
                    _destinationHandle = RTHandles.Alloc(renderTexture);
                }
            }
            
            public void SetSourceTexture(RTHandle sourceHandle)
            {
                _sourceHandle = sourceHandle;
            }

            public bool IsValidCamera(Camera camera)
            {
                return _cameraType.HasFlag(camera.cameraType) && (string.IsNullOrWhiteSpace(_cameraTag) || camera.CompareTag(_cameraTag));
            }
            
            public bool IsValidHandle(RTHandle handle)
            {
                return handle != null && handle.rt != null && handle.rt.IsCreated();
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // Check if the camera is a game camera
                var camera = renderingData.cameraData.camera;
                if (!IsValidCamera(camera) ||
                    !IsValidHandle(_destinationHandle) ||
                    !IsValidHandle(_sourceHandle))
                    return;
                
                // Create a command buffer
                var cmd = CommandBufferPool.Get();
                
                using (new ProfilingScope(cmd, _profilingSampler))
                {
                    // Blit the camera texture to the render texture
                    //cmd.SetRenderTarget(_destinationHandle);
                    if (_material != null)
                    {
                        //Blitter.BlitTexture(cmd, _sourceHandle, new Vector4(1, 1, 0, 0), _material, 0);
                        Blitter.BlitCameraTexture(cmd, _sourceHandle, _destinationHandle, _material, 0);
                    }
                    else
                    {
                        //Blitter.BlitTexture(cmd, _sourceHandle, new Vector4(1, 1, 0, 0), 0, false);
                        Blitter.BlitCameraTexture(cmd, _sourceHandle, _destinationHandle,  new Vector4(1, 1, 0, 0), 0, false);
                    }
                }
                
                // Execute the command buffer
                context.ExecuteCommandBuffer(cmd);
                
                // Clean up
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
        }

        public override void Create()
        {
            if (renderTexture == null)
            {
                Debug.LogError("Render texture is not set");
                return;
            }

            _scriptablePass = new CustomRenderPass(renderTexture, material, cameraType, cameraTag)
            {
                renderPassEvent = renderPassEvent
            };

            if (!string.IsNullOrWhiteSpace(globalTextureId))
            {
                Shader.SetGlobalTexture(globalTextureId, renderTexture);
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (_scriptablePass == null || !_scriptablePass.IsValidCamera(camera) || renderTexture == null)
            {
                return;
            }
            
            _scriptablePass.SetSourceTexture(renderer.cameraColorTargetHandle);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (_scriptablePass == null || !_scriptablePass.IsValidCamera(camera) || renderTexture == null)
            {
                return;
            }
            
            // Resize the render texture if the camera size changes
            var cameraWidth = camera.pixelWidth;
            var cameraHeight = camera.pixelHeight;

            if (renderTexture.width != cameraWidth || renderTexture.height != cameraHeight)
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                }

                renderTexture.width = cameraWidth;
                renderTexture.height = cameraHeight;
                renderTexture.Create();
                _scriptablePass.SetNewTexture(renderTexture);
                
                if (!string.IsNullOrWhiteSpace(globalTextureId))
                {
                    Shader.SetGlobalTexture(globalTextureId, renderTexture);
                }
                
#if UNITY_EDITOR
                Debug.Log($"Resizing render texture {renderTexture.name} to {cameraWidth}x{cameraHeight}");
#endif
            }
            
            renderer.EnqueuePass(_scriptablePass);
        }
    }
}
