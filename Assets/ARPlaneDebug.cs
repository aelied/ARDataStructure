using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;

public class ARPlaneDebug : MonoBehaviour
{
    private ARPlaneManager planeManager;
    
    void Start()
    {
        planeManager = GetComponent<ARPlaneManager>();
        
        if (planeManager == null)
        {
            Debug.LogError("ARPlaneManager not found!");
            return;
        }
        
        // Subscribe to plane events
        planeManager.planesChanged += OnPlanesChanged;
        
        Debug.Log("AR Plane Debug Started - Waiting for planes...");
    }
    
    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // Log added planes
        foreach (var plane in args.added)
        {
            Debug.Log($"✓ NEW PLANE DETECTED! ID: {plane.trackableId}, Size: {plane.size}");
            
            // Make it more visible by changing color
            var renderer = plane.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0, 1, 0, 0.5f); // Green semi-transparent
            }
        }
        
        // Log updated planes
        foreach (var plane in args.updated)
        {
            Debug.Log($"↻ Plane Updated: {plane.trackableId}, Size: {plane.size}");
        }
        
        // Log removed planes
        foreach (var plane in args.removed)
        {
            Debug.Log($"✗ Plane Removed: {plane.trackableId}");
        }
    }
    
    void Update()
    {
        // Display current state every second
        if (Time.frameCount % 60 == 0)
        {
            int planeCount = 0;
            foreach (var plane in planeManager.trackables)
            {
                planeCount++;
            }
            
            Debug.Log($"AR Status - Session: {ARSession.state}, Planes: {planeCount}");
        }
    }
    
    void OnDestroy()
    {
        if (planeManager != null)
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
    }
}