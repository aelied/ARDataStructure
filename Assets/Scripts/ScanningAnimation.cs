using UnityEngine;
using TMPro;

public class ScanningAnimation : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI scanningText;
    
    [Header("Animation Settings")]
    public float dotInterval = 0.5f;
    
    private float timer = 0f;
    private int dotCount = 0;
    
    void Start()
    {
        if (scanningText == null)
        {
            scanningText = GetComponent<TextMeshProUGUI>();
        }
    }
    
    void Update()
    {
        if (scanningText == null) return;
        
        timer += Time.deltaTime;
        
        if (timer >= dotInterval)
        {
            timer = 0f;
            dotCount = (dotCount + 1) % 4; // 0, 1, 2, 3, then back to 0
            
            string dots = new string('.', dotCount);
            scanningText.text = "Scanning" + dots;
        }
    }
    
    void OnDisable()
    {
        // Reset when disabled
        timer = 0f;
        dotCount = 0;
    }
}