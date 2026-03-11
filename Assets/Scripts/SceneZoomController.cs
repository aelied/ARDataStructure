using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Universal zoom controller for all AR data structure scenes
/// Uses pinch-to-zoom gesture: pinch out to zoom in, pinch in to zoom out
/// </summary>
public class SceneZoomController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float minScale = 0.3f;
    public float maxScale = 3.0f;
    public float zoomSmoothSpeed = 8f;
    public float pinchSensitivity = 1.5f; // Multiplier for pinch speed

    [Header("UI References - Optional")]
    public TextMeshProUGUI scaleIndicatorText; // Optional: Shows current scale %
    public GameObject pinchHintUI;             // Optional: "Pinch to zoom" hint panel

    [Header("Scene Reference")]
    public Transform sceneToZoom; // Auto-assigned via InitializeZoom()

    // Internal state
    private float currentScale = 1f;
    private float targetScale = 1f;
    private Vector3 initialScale;
    private bool isInitialized = false;

    // Pinch tracking
    private float previousPinchDistance = 0f;
    private bool isPinching = false;

    // Hint auto-hide
    private Coroutine hintCoroutine;

    void Start()
    {
        SetZoomControlsActive(false);
    }

    void Update()
    {
        if (!isInitialized) return;

        HandlePinchGesture();
        SmoothApplyZoom();
    }

    // ─────────────────────────────────────────────
    //  PINCH GESTURE DETECTION
    // ─────────────────────────────────────────────

    void HandlePinchGesture()
    {
        // Need exactly 2 fingers for a pinch
        if (Input.touchCount != 2)
        {
            if (isPinching)
            {
                isPinching = false;
                previousPinchDistance = 0f;
            }
            return;
        }

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        // Skip if either finger is over UI
        if (IsTouchOverUI(touch0) || IsTouchOverUI(touch1))
        {
            isPinching = false;
            return;
        }

        float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);

        // First frame of pinch - just record distance, don't zoom yet
        if (!isPinching || touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            previousPinchDistance = currentPinchDistance;
            isPinching = true;
            return;
        }

        // Calculate how much the pinch changed
        float pinchDelta = currentPinchDistance - previousPinchDistance;
        previousPinchDistance = currentPinchDistance;

        // Convert pixel delta to a scale delta
        // Dividing by screen height normalises across device sizes
        float scaleDelta = (pinchDelta / Screen.height) * pinchSensitivity;

        targetScale = Mathf.Clamp(targetScale + scaleDelta, minScale, maxScale);

        // Show hint briefly while zooming (optional)
        ShowHintBriefly();
    }

    // ─────────────────────────────────────────────
    //  SMOOTH ZOOM APPLICATION
    // ─────────────────────────────────────────────

    void SmoothApplyZoom()
    {
        if (sceneToZoom == null) return;

        currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * zoomSmoothSpeed);
        sceneToZoom.localScale = initialScale * currentScale;

        UpdateScaleIndicator();
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API (called from your scene scripts)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Call this from your scene script when the AR scene is spawned.
    /// </summary>
    public void InitializeZoom(Transform scene)
    {
        sceneToZoom = scene;
        initialScale = scene.localScale;
        currentScale = 1f;
        targetScale = 1f;
        isInitialized = true;

        SetZoomControlsActive(true);
        UpdateScaleIndicator();

        // Show hint briefly on init
        ShowHintBriefly();
    }

    /// <summary>
    /// Call this from your scene script when resetting.
    /// </summary>
    public void ResetZoom()
    {
        isInitialized = false;
        sceneToZoom = null;
        currentScale = 1f;
        targetScale = 1f;
        isPinching = false;
        previousPinchDistance = 0f;

        SetZoomControlsActive(false);
    }

    /// <summary>
    /// Programmatically set zoom to a specific scale (clamped to min/max).
    /// </summary>
    public void SetZoom(float scale)
    {
        if (!isInitialized) return;
        targetScale = Mathf.Clamp(scale, minScale, maxScale);
    }

    public float GetCurrentScale() => currentScale;
    public bool IsInitialized() => isInitialized;

    // ─────────────────────────────────────────────
    //  UI HELPERS
    // ─────────────────────────────────────────────

    void UpdateScaleIndicator()
    {
        if (scaleIndicatorText != null)
        {
            // Show as a percentage for clarity, e.g. "Scale: 120%"
            scaleIndicatorText.text = $"Scale: {Mathf.RoundToInt(currentScale * 100f)}%";
        }
    }

    void SetZoomControlsActive(bool active)
    {
        if (scaleIndicatorText != null)
            scaleIndicatorText.gameObject.SetActive(active);

        if (pinchHintUI != null)
            pinchHintUI.SetActive(active);
    }

    void ShowHintBriefly()
    {
        if (pinchHintUI == null) return;

        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(HintRoutine());
    }

    IEnumerator HintRoutine()
    {
        if (pinchHintUI != null)
            pinchHintUI.SetActive(true);

        yield return new WaitForSeconds(2f);

        if (pinchHintUI != null)
            pinchHintUI.SetActive(false);

        hintCoroutine = null;
    }

    // ─────────────────────────────────────────────
    //  UI TOUCH GUARD
    // ─────────────────────────────────────────────

    bool IsTouchOverUI(Touch touch)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId);
    }
}