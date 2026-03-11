using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows rotating AR scenes by clicking/touching and dragging left or right.
/// Automatically disables when 2+ fingers are detected (for pinch-to-zoom compatibility).
/// </summary>
public class SwipeRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed multiplier")]
    public float rotationSpeed = 100f;
    
    [Tooltip("Smooth rotation transition speed")]
    public float smoothSpeed = 5f;
    
    [Tooltip("Enable rotation snapping to 45-degree increments")]
    public bool snapRotation = false;
    
    [Tooltip("Snap angle increment (degrees)")]
    public float snapAngle = 45f;
    
    private Vector2 touchStartPos;
    private bool isSwiping = false;
    private float targetRotationY;
    private Transform sceneTransform;
    
    void Start()
    {
        // Initialize target rotation to current rotation
        if (sceneTransform != null)
        {
            targetRotationY = sceneTransform.eulerAngles.y;
        }
    }
    
    void Update()
    {
        HandleRotationInput();
        
        // Smooth rotation
        if (sceneTransform != null)
        {
            Vector3 currentEuler = sceneTransform.eulerAngles;
            float smoothedY = Mathf.LerpAngle(currentEuler.y, targetRotationY, Time.deltaTime * smoothSpeed);
            sceneTransform.eulerAngles = new Vector3(currentEuler.x, smoothedY, currentEuler.z);
        }
    }
    
    void HandleRotationInput()
    {
        if (sceneTransform == null) return;
        
        // ✅ DISABLE ROTATION if 2+ fingers detected (pinch gesture)
        if (Input.touchCount >= 2)
        {
            isSwiping = false;
            return;
        }
        
        // Touch input (mobile)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    // Ignore if starting touch on UI
                    if (IsTouchOverUI(touch.fingerId))
                    {
                        isSwiping = false;
                        return;
                    }
                    
                    touchStartPos = touch.position;
                    isSwiping = true;
                    break;
                
                case TouchPhase.Moved:
                    if (isSwiping)
                    {
                        // Calculate drag delta from last frame
                        Vector2 currentPos = touch.position;
                        Vector2 dragDelta = currentPos - touchStartPos;
                        
                        // Rotate continuously based on horizontal drag
                        // Negative because dragging left should rotate left (clockwise from top)
                        float rotationAmount = -dragDelta.x * (rotationSpeed / 100f);
                        targetRotationY += rotationAmount;
                        
                        // Update start position for next frame
                        touchStartPos = currentPos;
                    }
                    break;
                
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isSwiping)
                    {
                        isSwiping = false;
                        
                        // Final snap if enabled
                        if (snapRotation)
                        {
                            targetRotationY = Mathf.Round(targetRotationY / snapAngle) * snapAngle;
                        }
                    }
                    break;
            }
        }
        // Mouse input (editor/desktop testing)
        else if (Input.GetMouseButtonDown(0))
        {
            // Check if starting on UI
            if (!IsTouchOverUI(-1))
            {
                touchStartPos = Input.mousePosition;
                isSwiping = true;
            }
        }
        else if (Input.GetMouseButton(0) && isSwiping)
        {
            // Calculate drag delta from last frame
            Vector2 currentPos = Input.mousePosition;
            Vector2 dragDelta = currentPos - touchStartPos;
            
            // Rotate continuously based on horizontal drag
            float rotationAmount = -dragDelta.x * (rotationSpeed / 100f);
            targetRotationY += rotationAmount;
            
            // Update start position for next frame
            touchStartPos = currentPos;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isSwiping)
            {
                isSwiping = false;
                
                // Final snap if enabled
                if (snapRotation)
                {
                    targetRotationY = Mathf.Round(targetRotationY / snapAngle) * snapAngle;
                }
            }
        }
    }
    
    bool IsTouchOverUI(int fingerId)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
            return false;
        
        if (fingerId >= 0)
        {
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(fingerId);
        }
        else
        {
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
    }
    
    /// <summary>
    /// Initialize the rotation system with the scene transform
    /// Call this after spawning your AR scene
    /// </summary>
    public void InitializeRotation(Transform scene)
    {
        sceneTransform = scene;
        if (sceneTransform != null)
        {
            targetRotationY = sceneTransform.eulerAngles.y;
        }
    }
    
    /// <summary>
    /// Reset rotation to initial angle
    /// </summary>
    public void ResetRotation()
    {
        if (sceneTransform != null)
        {
            targetRotationY = 0f;
            sceneTransform.eulerAngles = new Vector3(
                sceneTransform.eulerAngles.x,
                0f,
                sceneTransform.eulerAngles.z
            );
        }
    }
    
    /// <summary>
    /// Rotate to a specific angle
    /// </summary>
    public void RotateToAngle(float angle)
    {
        targetRotationY = angle;
    }
    
    /// <summary>
    /// Get current rotation angle
    /// </summary>
    public float GetCurrentRotation()
    {
        return sceneTransform != null ? sceneTransform.eulerAngles.y : 0f;
    }
}