using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class MapSaver : MonoBehaviour
{
    private Camera cam;
    public bool saveNormalMap = false;
    public bool saveDepthMap = false;
    public bool saveAlbedoMap = false;
    public bool saveSpecularMap = false;
    public bool saveTextureMap = false; // Флаг для сохранения итоговой текстуры
    
    
    private RenderTexture textureMapRT;
    private RenderTexture normalMapRT;
    private RenderTexture depthMapRT;
    private RenderTexture albedoMapRT;
    private RenderTexture specularMapRT;

    private CommandBuffer normalBuffer;
    private CommandBuffer depthBuffer;
    private CommandBuffer albedoBuffer;
    private CommandBuffer specularBuffer;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        SetupRenderTexturesAndBuffers();
    }

    
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (saveTextureMap)
        {
            saveTextureMap = false; // Сбрасываем флаг, чтобы избежать повторного сохранения
            // Инициализируем RenderTexture, если это необходимо
            if (textureMapRT == null || textureMapRT.width != src.width || textureMapRT.height != src.height)
            {
                if (textureMapRT != null) textureMapRT.Release();
                textureMapRT = new RenderTexture(src.width, src.height, 0);
            }

            Graphics.Blit(src, textureMapRT); // Копируем итоговый рендер в textureMapRT
            StartCoroutine(SaveTextureAfterDelay("TextureMap", textureMapRT));
        }

        Graphics.Blit(src, dest); // Важно копировать изображение в dest, чтобы оно отображалось на экране
    }
    
    void SetupRenderTexturesAndBuffers()
    {
        cam.renderingPath = RenderingPath.DeferredShading;

        // Normal Map
        int width = Screen.width;
        int height = Screen.height;
        
        Debug.Log("Setting up textures: "+width+"x"+height);
        
        normalMapRT = new RenderTexture(width, height, 24);
        normalBuffer = new CommandBuffer();
        normalBuffer.Blit(BuiltinRenderTextureType.GBuffer2, normalMapRT);
        cam.AddCommandBuffer(CameraEvent.AfterLighting, normalBuffer);

        // Albedo Map
        albedoMapRT = new RenderTexture(width, height, 24);
        albedoBuffer = new CommandBuffer();
        albedoBuffer.Blit(BuiltinRenderTextureType.GBuffer0, albedoMapRT);
        cam.AddCommandBuffer(CameraEvent.AfterGBuffer, albedoBuffer);

        // Specular Map
        specularMapRT = new RenderTexture(width, height, 24);
        specularBuffer = new CommandBuffer();
        specularBuffer.Blit(BuiltinRenderTextureType.GBuffer1, specularMapRT);
        cam.AddCommandBuffer(CameraEvent.AfterGBuffer, specularBuffer);

        // Depth Map
        depthMapRT = new RenderTexture(width, height, 24);
        Material depthMaterial = new Material(Shader.Find("Hidden/DepthShader")); // Ensure this shader exists and is correct
        depthBuffer = new CommandBuffer();
        depthBuffer.Blit(null, depthMapRT, depthMaterial);
        cam.AddCommandBuffer(CameraEvent.AfterEverything, depthBuffer);
    }

    void Update()
    {
        if (saveNormalMap)
        {
            StartCoroutine(SaveTextureAfterDelay("NormalMap", normalMapRT));
            saveNormalMap = false;
        }
        if (saveDepthMap)
        {
            StartCoroutine(SaveTextureAfterDelay("DepthMap", depthMapRT));
            saveDepthMap = false;
        }
        if (saveAlbedoMap)
        {
            StartCoroutine(SaveTextureAfterDelay("AlbedoMap", albedoMapRT));
            saveAlbedoMap = false;
        }
        if (saveSpecularMap)
        {
            StartCoroutine(SaveTextureAfterDelay("SpecularMap", specularMapRT));
            saveSpecularMap = false;
        }
    }

    IEnumerator SaveTextureAfterDelay(string fileName, RenderTexture rt)
    {
        yield return new WaitForEndOfFrame();
        SaveTexture(rt, fileName);
    }

    void SaveTexture(RenderTexture rt, string fileName)
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();
        string path = $"Assets/SavedTextures/{fileName}.png";
        if (!Directory.Exists(Path.GetDirectoryName(path)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }
        File.WriteAllBytes(path, bytes);

        Debug.Log($"{fileName} saved to {path}");

#if UNITY_EDITOR
        AssetDatabase.ImportAsset(path);
#endif

        RenderTexture.active = currentRT;
        DestroyImmediate(texture);
    }

    void OnDisable()
    {
        if (normalBuffer != null) cam.RemoveCommandBuffer(CameraEvent.AfterLighting, normalBuffer);
        if (depthBuffer != null) cam.RemoveCommandBuffer(CameraEvent.AfterEverything, depthBuffer);
        if (albedoBuffer != null) cam.RemoveCommandBuffer(CameraEvent.AfterGBuffer, albedoBuffer);
        if (specularBuffer != null) cam.RemoveCommandBuffer(CameraEvent.AfterGBuffer, specularBuffer);

        DestroyImmediate(normalMapRT);
        DestroyImmediate(depthMapRT);
        DestroyImmediate(albedoMapRT);
        DestroyImmediate(specularMapRT);
    }
}
