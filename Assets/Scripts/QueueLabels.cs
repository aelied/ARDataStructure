using UnityEngine;
using TMPro;

public class QueueLabels : MonoBehaviour
{
    [Header("Label Settings")]
    public GameObject frontLabelPrefab;
    public GameObject backLabelPrefab;
    public float labelOffsetY = 0.15f;
    public float singleNodeLabelSpacing = 0.08f; // Vertical spacing when both labels on same node
    public float labelScale = 1.5f; // 👈 Adjust this value to make labels bigger or smaller

    private GameObject frontLabel;
    private GameObject backLabel;
    private QueueManager queueManager;

    void Start()
    {
        queueManager = GetComponent<QueueManager>();
    }

    void Update()
    {
        UpdateLabels();
    }

    void UpdateLabels()
    {
        if (queueManager == null || !queueManager.IsQueuePlaced())
        {
            HideLabels();
            return;
        }

        int queueSize = queueManager.Size();

        if (queueSize == 0)
        {
            HideLabels();
            return;
        }

        // Get node positions from queue manager
        var nodes = queueManager.GetNodes();

        if (nodes.Count > 0)
        {
            bool isSingleNode = nodes.Count == 1;

            if (isSingleNode)
            {
                Vector3 nodePos = nodes[0].transform.position;

                ShowFrontLabel(nodePos, labelOffsetY + singleNodeLabelSpacing);
                ShowBackLabel(nodePos, labelOffsetY - singleNodeLabelSpacing);
            }
            else
            {
                ShowFrontLabel(nodes[0].transform.position, labelOffsetY);
                ShowBackLabel(nodes[nodes.Count - 1].transform.position, labelOffsetY);
            }
        }
    }

    void ShowFrontLabel(Vector3 position, float yOffset)
    {
        if (frontLabel == null && frontLabelPrefab != null)
        {
            frontLabel = Instantiate(frontLabelPrefab);
            frontLabel.transform.localScale *= labelScale; // 👈 Scale up label
        }

        if (frontLabel != null)
        {
            frontLabel.SetActive(true);
            Vector3 labelPos = position + Vector3.up * yOffset;
            frontLabel.transform.position = labelPos;

            if (Camera.main != null)
            {
                frontLabel.transform.LookAt(Camera.main.transform);
                frontLabel.transform.Rotate(0, 180, 0);
            }
        }
    }

    void ShowBackLabel(Vector3 position, float yOffset)
    {
        if (backLabel == null && backLabelPrefab != null)
        {
            backLabel = Instantiate(backLabelPrefab);
            backLabel.transform.localScale *= labelScale; // 👈 Scale up label
        }

        if (backLabel != null)
        {
            backLabel.SetActive(true);
            Vector3 labelPos = position + Vector3.up * yOffset;
            backLabel.transform.position = labelPos;

            if (Camera.main != null)
            {
                backLabel.transform.LookAt(Camera.main.transform);
                backLabel.transform.Rotate(0, 180, 0);
            }
        }
    }

    void HideLabels()
    {
        if (frontLabel != null)
            frontLabel.SetActive(false);

        if (backLabel != null)
            backLabel.SetActive(false);
    }

    void OnDestroy()
    {
        if (frontLabel != null)
            Destroy(frontLabel);

        if (backLabel != null)
            Destroy(backLabel);
    }
}
