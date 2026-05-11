using UnityEngine;

// [ExecuteAlways] makes this work in the Scene View too!
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraSync : MonoBehaviour
{
    [Tooltip("Drag your Main Camera here")]
    [SerializeField] private Camera mainCam;

    private Camera _visionCam;

    private void Start()
    {
        if (mainCam == null) mainCam = Camera.main;
        _visionCam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (mainCam == null) return;

        transform.position = mainCam.transform.position;
        transform.rotation = mainCam.transform.rotation;

        _visionCam.orthographicSize = mainCam.orthographicSize;
    }
}