using UnityEngine;

[ExecuteInEditMode]
public class SimpleShader : MonoBehaviour
{
    public string shaderName = "SimpleReflection";
    private Camera cam;
    public Material _material;

    void Start()
    {
        SetVariables();
    }

    void Update()
    {
        SetVariables();
    }

    void SetVariables()
    {
        if (_material == null)
        {
            Shader shader = Shader.Find(shaderName);
            _material = new Material(shader);
        }

        if (cam == null)
        {
            cam = GetComponent<Camera>();
            //Enable depth and normals
            cam.depthTextureMode |= DepthTextureMode.DepthNormals;
            cam.renderingPath = RenderingPath.DeferredShading;
        }

        _material.SetTexture("_CameraGBufferTexture0", Shader.GetGlobalTexture("_CameraGBufferTexture0"));
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (_material != null)
        {
            Graphics.Blit(src, dest, _material);
        }
    }
}