using UnityEngine;

[ExecuteAlways]
public class VisionMaskManager : MonoBehaviour
{
    [Tooltip("Drag your RT_VisionMask Render Texture here")]
    public RenderTexture visionRenderTexture;

    void Update()
    {
        if (visionRenderTexture != null)
        {
            Shader.SetGlobalTexture("_VisionMask", visionRenderTexture);
        }
    }
}