using UnityEngine;
using TMPro;

public class NodeLabelController : MonoBehaviour
{
    [Header("Label Settings")]
    public float floatHeight = 0.25f; // Height above the node
    public bool alwaysFaceCamera = true;
    
    private TextMeshPro labelText;
    private Transform targetNode;
    
    void Awake()
    {
        labelText = GetComponentInChildren<TextMeshPro>();
        
        if (labelText == null)
        {
            Debug.LogError("NodeLabelController: No TextMeshPro found in children!");
        }
    }
    
    void LateUpdate()
    {
        if (alwaysFaceCamera && Camera.main != null)
        {
            // Make label face the camera
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
    
    public void SetText(string text)
    {
        if (labelText != null)
        {
            labelText.text = text;
        }
    }
    
    public void SetColor(Color color)
    {
        if (labelText != null)
        {
            labelText.color = color;
        }
    }
    
    public void SetNodeTarget(Transform node)
    {
        targetNode = node;
    }
}