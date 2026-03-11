using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using TMPro;

public class ARQueueController : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    
    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public GameObject buttonPanel;
    public UnityEngine.UI.Button enqueueButton;
    public UnityEngine.UI.Button dequeueButton;
    public UnityEngine.UI.Button resetButton;
    
    [Header("Prefabs")]
    public GameObject nodePrefab;
    
    private enum State
    {
        TapSurface,
        PlaceFirst,
        PlaceSecond,
        PlaceMore,
        Ready
    }
    
    private State currentState = State.TapSurface;
    private List<GameObject> queueNodes = new List<GameObject>();
    private List<string> queueData = new List<string>();
    private Vector3 queueStart;
    private Vector3 direction;
    private float spacing;
    
    void Start()
    {
        buttonPanel.SetActive(false);
        UpdateUI();
        
        enqueueButton.onClick.AddListener(OnEnqueue);
        dequeueButton.onClick.AddListener(OnDequeue);
        resetButton.onClick.AddListener(OnReset);
    }
    
    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleTouch(Input.GetTouch(0).position);
        }
    }
    
    void HandleTouch(Vector2 screenPos)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        
        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 hitPos = hits[0].pose.position;
            
            switch (currentState)
            {
                case State.TapSurface:
                    queueStart = hitPos;
                    currentState = State.PlaceFirst;
                    UpdateUI();
                    break;
                    
                case State.PlaceFirst:
                    CreateNode(hitPos, "Coin 1", 0);
                    queueStart = hitPos;
                    currentState = State.PlaceSecond;
                    UpdateUI();
                    break;
                    
                case State.PlaceSecond:
                    direction = (hitPos - queueStart).normalized;
                    spacing = Vector3.Distance(hitPos, queueStart);
                    CreateNode(hitPos, "Coin 2", 1);
                    currentState = State.PlaceMore;
                    UpdateUI();
                    break;
                    
                case State.PlaceMore:
                    CreateNode(hitPos, $"Coin {queueNodes.Count + 1}", queueNodes.Count);
                    if (queueNodes.Count >= 3)
                    {
                        currentState = State.Ready;
                        buttonPanel.SetActive(true);
                        UpdateUI();
                    }
                    break;
            }
        }
    }
    
    void CreateNode(Vector3 position, string data, int index)
    {
        GameObject node = Instantiate(nodePrefab, position + Vector3.up * 0.15f, Quaternion.identity);
        
        // Set text
        TextMeshPro[] texts = node.GetComponentsInChildren<TextMeshPro>();
        if (texts.Length >= 2)
        {
            texts[0].text = data; // Data text
            texts[1].text = $"[{index}]"; // Index text
        }
        
        // Color front node green
        if (index == 0)
        {
            Renderer renderer = node.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.green;
            }
        }
        
        queueNodes.Add(node);
        queueData.Add(data);
        UpdateUI();
    }
    
    void OnEnqueue()
    {
        if (queueNodes.Count >= 5)
        {
            instructionText.text = "Queue is full!";
            return;
        }
        
        instructionText.text = "Tap to add new object at the back";
        currentState = State.PlaceMore;
    }
    
    void OnDequeue()
    {
        if (queueNodes.Count == 0) return;
        
        // Remove front node
        Destroy(queueNodes[0]);
        queueNodes.RemoveAt(0);
        queueData.RemoveAt(0);
        
        // Update colors and indices
        for (int i = 0; i < queueNodes.Count; i++)
        {
            TextMeshPro[] texts = queueNodes[i].GetComponentsInChildren<TextMeshPro>();
            if (texts.Length >= 2)
            {
                texts[1].text = $"[{i}]";
            }
            
            // Color new front green
            Renderer renderer = queueNodes[i].GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = (i == 0) ? Color.green : Color.cyan;
            }
        }
        
        UpdateUI();
    }
    
    void OnReset()
    {
        foreach (GameObject node in queueNodes)
        {
            Destroy(node);
        }
        queueNodes.Clear();
        queueData.Clear();
        
        currentState = State.TapSurface;
        buttonPanel.SetActive(false);
        UpdateUI();
    }
    
    void UpdateUI()
    {
        switch (currentState)
        {
            case State.TapSurface:
                instructionText.text = "Tap on a flat surface";
                break;
            case State.PlaceFirst:
                instructionText.text = "Tap where your first coin is";
                break;
            case State.PlaceSecond:
                instructionText.text = "Tap where your second coin is (to the right)";
                break;
            case State.PlaceMore:
                instructionText.text = $"Tap for coin {queueNodes.Count + 1} (need at least 3)";
                break;
            case State.Ready:
                instructionText.text = "Queue ready! Use buttons below";
                break;
        }
        
        statusText.text = $"Queue Size: {queueNodes.Count}";
        if (queueNodes.Count > 0)
        {
            statusText.text += $"\nFront: {queueData[0]}";
            statusText.text += $"\nBack: {queueData[queueData.Count - 1]}";
        }
    }
}