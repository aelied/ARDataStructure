using UnityEngine;

/// <summary>
/// Makes a GameObject always face the camera.
/// Used for text labels in AR scenes.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}