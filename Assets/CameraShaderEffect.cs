using UnityEngine;

[ExecuteInEditMode]
public class CameraShaderEffect : MonoBehaviour
{
    public enum VisualizationMode
    {
        Depth,
        Normals,
        Texture,
        Fog,
        Reflections1,
        Specular,
        Reflections2,
        Reflections3,
    }

    public VisualizationMode Mode = VisualizationMode.Depth;
    private Material _material;
    public Camera cam;

    void Start()
    {
        if (_material == null)
        {
            Shader shader = Shader.Find("Custom/DepthAndNormalsVisualizer");
            _material = new Material(shader);
        }

        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        // Включаем генерацию необходимых текстур
        cam.depthTextureMode |= DepthTextureMode.DepthNormals;
    }


    [Min(0.001f)] public float _DepthHit = 1;
    [Range(0, 60)] public int _MaxSteps = 60;

    public float _StepSize = 0.01f;
    public float _TestNumber = 1;
    public float _TestNumber2 = 1;
    public float _TestNumber3 = 1;
    public float _TestNumber4 = 0;

    public bool _IsRectangular = true;
    public bool _CheckBuffer = true;


    void Update()
    {
        if (_material != null)
        {
            SetVariables();
        }
    }

    void SetVariables()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }


        // Устанавливаем режим визуализации в шейдере
        _material.SetFloat("_Mode", (float)Mode);
        _material.SetInt("_MaxSteps", _MaxSteps);
        _material.SetFloat("_StepSize", (_StepSize / 100));
        _material.SetFloat("_DepthHit", _DepthHit / 100);
        _material.SetFloat("_CameraFarPlane", cam.farClipPlane);
        _material.SetFloat("_CameraNearPlane", cam.nearClipPlane);
        _material.SetFloat("_IsRectangular", (_IsRectangular ? 1f : 0f));
        _material.SetFloat("_TestNumber", _TestNumber);
        _material.SetFloat("_TestNumber2", _TestNumber2);
        _material.SetFloat("_TestNumber3", _TestNumber3);
        _material.SetFloat("_TestNumber4", _TestNumber4);
        if (_CheckBuffer)
        {
            _material.SetTexture("_CameraGBufferTexture0", Shader.GetGlobalTexture("_CameraGBufferTexture0"));
        }
        else
        {
            _material.SetTexture("_CameraGBufferTexture0", null);
        }
        //_material.SetTexture("_CameraGBufferTexture1", Shader.GetGlobalTexture("_CameraGBufferTexture1"));

        _material.SetVector("_CameraWorldPos", cam.transform.position);
        _material.SetFloat("_CameraFOV", cam.fieldOfView);


        _material.SetMatrix("_InvProjMatrix", cam.projectionMatrix.inverse);
        _material.SetMatrix("_ProjMatrix", cam.projectionMatrix);
        _material.SetMatrix("_InvViewProjMatrix", (cam.projectionMatrix * cam.worldToCameraMatrix).inverse);
        _material.SetMatrix("_ViewProjMatrix", (cam.projectionMatrix * cam.worldToCameraMatrix));

        _material.SetMatrix("_CameraToWorldMatrix", cam.worldToCameraMatrix.inverse);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (_material != null)
        {
            //SetVariables();

            Graphics.Blit(src, dest, _material);
        }
    }
}