using UnityEngine;

public class ObjectAnimator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 30f; // Degrees per second
    public Vector3 rotationAxis = Vector3.up; // Rotate around Y axis
    
    [Header("Bounce Settings")]
    public float bounceHeight = 0.02f; // How high it bounces
    public float bounceSpeed = 2f; // How fast it bounces
    
    private Vector3 startPosition;
    private float timeOffset;
    
    void Start()
    {
        // Store the initial local position
        startPosition = transform.localPosition;
        
        // Random time offset so objects don't all bounce in sync
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }
    
    void Update()
    {
        // Rotate the object
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        
        // Bounce up and down using sine wave
        float bounce = Mathf.Sin((Time.time * bounceSpeed) + timeOffset) * bounceHeight;
        transform.localPosition = startPosition + new Vector3(0, bounce, 0);
    }
    
    // Call this if the parent (QR code) moves
    public void UpdateStartPosition()
    {
        startPosition = transform.localPosition;
    }
}