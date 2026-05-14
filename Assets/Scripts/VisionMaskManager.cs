using UnityEngine;

[ExecuteAlways]
public class VisionMaskManager : MonoBehaviour
{
    [Tooltip("Drag your Vision Camera here")]
    public Camera visionCamera;

    private RenderTexture _dynamicMask;

    void Start()
    {
        if (visionCamera == null) visionCamera = GetComponent<Camera>();
    }

    void Update()
    {
        if (_dynamicMask == null || _dynamicMask.width != Screen.width || _dynamicMask.height != Screen.height)
        {
            if (_dynamicMask != null) _dynamicMask.Release();

            _dynamicMask = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
            _dynamicMask.name = "DynamicVisionMask";
            _dynamicMask.filterMode = FilterMode.Bilinear;

            if (visionCamera != null) visionCamera.targetTexture = _dynamicMask;
        }

        if (_dynamicMask != null)
        {
            Shader.SetGlobalTexture("_VisionMask", _dynamicMask);
        }
    }

    private void OnDisable()
    {
        if (_dynamicMask != null) _dynamicMask.Release();
    }
}