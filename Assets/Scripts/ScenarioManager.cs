using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// ScenarioManager  (Unified — handles ALL data structures including Trees)
/// =========================================================================
/// Fetches the active scenario list from the admin server on app launch,
/// then dynamically configures 2 scenario buttons so the AR app always
/// shows whichever 2 scenarios the admin chose on scenarios.html.
///
/// Works for: Arrays, Stacks, Queue, LinkedList, Graph, Trees
///
/// ── SETUP (Inspector) ────────────────────────────────────────────────────────
///   1. Add this component to an empty GameObject in the scene.
///   2. Set dataStructure to match the scene:
///        "Arrays"     → InteractiveArrayCars scene
///        "Stacks"     → InteractiveStackPlates scene
///        "Queue"      → InteractiveCoffeeQueue scene
///        "LinkedList" → InteractiveTrainList scene
///        "Graph"      → InteractiveCityGraph scene
///        "Trees"      → InteractiveFamilyTree scene
///   3. Assign scenarioButton1 / scenarioButton2
///   4. Assign button1Label / button2Label  (TextMeshProUGUI on each button)
///   5. Assign the controller for this scene (leave unused ones empty/null)
///   6. Set serverUrl = "https://structureality-admin.onrender.com"
///
/// ── TREES NOTE ──────────────────────────────────────────────────────────────
///   The Trees scene (InteractiveFamilyTree) already has its own scenario
///   selection panel (FamilyTree / FruitTree buttons). ScenarioManager now
///   drives those buttons dynamically, including the new ForestTrail scenario.
///   Difficulty (Beginner / Intermediate) is still chosen by the STUDENT.
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    // =========================================================================
    // INSPECTOR FIELDS
    // =========================================================================

    [Header("Server Config")]
    public string serverUrl = "https://structureality-admin.onrender.com";

    [Tooltip("Which data structure this scene teaches.\n" +
             "  Arrays     → InteractiveArrayCars scene\n" +
             "  Stacks     → InteractiveStackPlates scene\n" +
             "  Queue      → InteractiveCoffeeQueue scene\n" +
             "  LinkedList → InteractiveTrainList scene\n" +
             "  Graph      → InteractiveCityGraph scene\n" +
             "  Trees      → InteractiveFamilyTree scene")]
    public string dataStructure = "Arrays";

    [Header("Scenario Buttons")]
    public UnityEngine.UI.Button scenarioButton1;
    public UnityEngine.UI.Button scenarioButton2;
    public TextMeshProUGUI button1Label;
    public TextMeshProUGUI button2Label;

    [Header("AR Controllers  (assign only the one for this scene)")]
    public InteractiveArrayCars    arrayController;
    public InteractiveStackPlates  stackController;
    public InteractiveCoffeeQueue  queueController;
    public InteractiveTrainList    linkedListController;
    public InteractiveCityGraph    graphController;
    public InteractiveFamilyTree   treeController;      // NEW — assign for the Trees scene

    [Header("Offline Fallback")]
    [Tooltip("Arrays fallback:     ParkingLot, VendingMachine\n" +
             "Stacks fallback:     Plates, Warehouse\n" +
             "Queue fallback:      CoffeeShop, ConveyorBelt\n" +
             "LinkedList fallback: Train, Solar\n" +
             "Graph fallback:      CityMap, IslandNetwork\n" +
             "Trees fallback:      FamilyTree, FruitTree")]
    public string[] fallbackScenarios = new string[] { "ParkingLot", "VendingMachine" };

    // =========================================================================
    // SCENARIO REGISTRY
    // =========================================================================

    [Serializable]
    public class ScenarioMeta
    {
        public string dsKey;
        public string id;
        public string buttonLabel;
    }

    private readonly ScenarioMeta[] ALL_SCENARIOS = new ScenarioMeta[]
    {
        // ── Arrays ────────────────────────────────────────────────────────────
        new ScenarioMeta { dsKey = "Arrays", id = "ParkingLot",    buttonLabel = "Parking Lot"       },
        new ScenarioMeta { dsKey = "Arrays", id = "VendingMachine", buttonLabel = "Vending Machine"  },
        new ScenarioMeta { dsKey = "Arrays", id = "Supermarket",    buttonLabel = "Supermarket Shelf" },

        // ── Stacks ────────────────────────────────────────────────────────────
        new ScenarioMeta { dsKey = "Stacks", id = "Plates",         buttonLabel = "Plates"            },
        new ScenarioMeta { dsKey = "Stacks", id = "Warehouse",      buttonLabel = "Warehouse"         },
        new ScenarioMeta { dsKey = "Stacks", id = "Kitchen",        buttonLabel = "Kitchen"           },

        // ── Queue ─────────────────────────────────────────────────────────────
        new ScenarioMeta { dsKey = "Queue",  id = "CoffeeShop",     buttonLabel = "Coffee Shop"       },
        new ScenarioMeta { dsKey = "Queue",  id = "ConveyorBelt",   buttonLabel = "Conveyor Belt"     },
        new ScenarioMeta { dsKey = "Queue",  id = "Hospital",       buttonLabel = "Hospital ER"       },

        // ── LinkedList ────────────────────────────────────────────────────────
        new ScenarioMeta { dsKey = "LinkedList", id = "Train",      buttonLabel = "Train"             },
        new ScenarioMeta { dsKey = "LinkedList", id = "Solar",      buttonLabel = "Solar System"      },
        new ScenarioMeta { dsKey = "LinkedList", id = "CityMetro",  buttonLabel = "City Metro"        },

        // ── Graph ─────────────────────────────────────────────────────────────
        new ScenarioMeta { dsKey = "Graph",  id = "CityMap",         buttonLabel = "City Map"         },
        new ScenarioMeta { dsKey = "Graph",  id = "IslandNetwork",   buttonLabel = "Island Network"   },
        new ScenarioMeta { dsKey = "Graph",  id = "SpaceStation",    buttonLabel = "Space Station"    },

        // ── Trees ─────────────────────────────────────────────────────────────
        new ScenarioMeta { dsKey = "Trees",  id = "FamilyTree",      buttonLabel = "Family Tree"      },
        new ScenarioMeta { dsKey = "Trees",  id = "FruitTree",       buttonLabel = "Fruit Tree"       },
        new ScenarioMeta { dsKey = "Trees",  id = "ForestTrail",     buttonLabel = "Forest Trail"     },
    };

    // =========================================================================
    // PRIVATE STATE
    // =========================================================================
    private string[] activeScenarioIds = null;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================
    void Start()
    {
        StartCoroutine(FetchAndApplyScenarios());
    }

    // =========================================================================
    // FETCH FROM SERVER
    // =========================================================================
    IEnumerator FetchAndApplyScenarios()
    {
        string url = $"{serverUrl}/api/scenarios/active?ds={dataStructure}";
        Debug.Log($"[ScenarioManager | {dataStructure}] Fetching: {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    ScenarioResponse response =
                        JsonUtility.FromJson<ScenarioResponse>(req.downloadHandler.text);

                    if (response != null && response.success
                        && response.scenarios != null && response.scenarios.Length > 0)
                    {
                        activeScenarioIds = response.scenarios;
                        Debug.Log($"[ScenarioManager | {dataStructure}] Server → [{string.Join(", ", activeScenarioIds)}]");
                    }
                    else
                    {
                        Debug.LogWarning($"[ScenarioManager | {dataStructure}] Bad server response — using fallback.");
                        activeScenarioIds = fallbackScenarios;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ScenarioManager | {dataStructure}] Parse error — fallback. {ex.Message}");
                    activeScenarioIds = fallbackScenarios;
                }
            }
            else
            {
                Debug.LogWarning($"[ScenarioManager | {dataStructure}] Network error ({req.error}) — fallback.");
                activeScenarioIds = fallbackScenarios;
            }
        }

        ApplyScenarioConfig();
    }

    // =========================================================================
    // APPLY CONFIG TO BUTTONS
    // =========================================================================
    void ApplyScenarioConfig()
    {
        if (activeScenarioIds == null || activeScenarioIds.Length == 0)
        {
            Debug.LogError($"[ScenarioManager | {dataStructure}] No scenario IDs — cannot configure buttons.");
            return;
        }

        if (activeScenarioIds.Length >= 1)
            ConfigureButton(scenarioButton1, button1Label, activeScenarioIds[0]);

        if (activeScenarioIds.Length >= 2)
            ConfigureButton(scenarioButton2, button2Label, activeScenarioIds[1]);
        else
        {
            if (scenarioButton2 != null) scenarioButton2.gameObject.SetActive(false);
        }

        Debug.Log($"[ScenarioManager | {dataStructure}] Buttons configured.");
    }

    void ConfigureButton(UnityEngine.UI.Button btn, TextMeshProUGUI label, string scenarioId)
    {
        if (btn == null) return;

        ScenarioMeta meta        = FindMeta(scenarioId);
        string       displayText = meta != null ? meta.buttonLabel : scenarioId;

        if (label != null)
            label.text = displayText;
        else
        {
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = displayText;
        }

        btn.onClick.RemoveAllListeners();

        if (dataStructure == "Arrays" && arrayController != null)
        {
            InteractiveArrayCars.Scenario s = ArrayScenarioIdToEnum(scenarioId);
            btn.onClick.AddListener(() => { Debug.Log($"[SM|Arrays] → {scenarioId}"); arrayController.OnScenarioChosen(s); });
        }
        else if (dataStructure == "Stacks" && stackController != null)
        {
            InteractiveStackPlates.Scenario s = StackScenarioIdToEnum(scenarioId);
            btn.onClick.AddListener(() => { Debug.Log($"[SM|Stacks] → {scenarioId}"); stackController.OnScenarioChosen(s); });
        }
        else if (dataStructure == "Queue" && queueController != null)
        {
            InteractiveCoffeeQueue.ScenarioMode s = QueueScenarioIdToEnum(scenarioId);
            btn.onClick.AddListener(() => { Debug.Log($"[SM|Queue] → {scenarioId}"); queueController.OnScenarioChosen(s); });
        }
        else if (dataStructure == "LinkedList" && linkedListController != null)
        {
            InteractiveTrainList.Scenario s = LinkedListScenarioIdToEnum(scenarioId);
            btn.onClick.AddListener(() => { Debug.Log($"[SM|LinkedList] → {scenarioId}"); linkedListController.OnScenarioChosen(s); });
        }
        else if (dataStructure == "Graph" && graphController != null)
        {
            InteractiveCityGraph.ScenarioMode s = GraphScenarioIdToEnum(scenarioId);
            btn.onClick.AddListener(() => { Debug.Log($"[SM|Graph] → {scenarioId}"); graphController.OnScenarioChosen(s); });
        }
        else if (dataStructure == "Trees" && treeController != null)
        {
            InteractiveFamilyTree.ScenarioMode s = TreeScenarioIdToEnum(scenarioId);
            btn.onClick.AddListener(() => { Debug.Log($"[SM|Trees] → {scenarioId}"); treeController.OnScenarioChosen(s); });
        }
        else
        {
            Debug.LogWarning($"[ScenarioManager] No controller assigned for dataStructure='{dataStructure}'.");
        }

        btn.gameObject.SetActive(true);
        Debug.Log($"[ScenarioManager | {dataStructure}] Button set: '{displayText}'");
    }

    // =========================================================================
    // SCENARIO ID → ENUM
    // =========================================================================

    InteractiveArrayCars.Scenario ArrayScenarioIdToEnum(string id)
    {
        switch (id)
        {
            case "ParkingLot":     return InteractiveArrayCars.Scenario.Parking;
            case "VendingMachine": return InteractiveArrayCars.Scenario.Vending;
            case "Supermarket":    return InteractiveArrayCars.Scenario.Supermarket;
            default:
                Debug.LogWarning($"[SM|Arrays] Unknown '{id}' — default Parking.");
                return InteractiveArrayCars.Scenario.Parking;
        }
    }

    InteractiveStackPlates.Scenario StackScenarioIdToEnum(string id)
    {
        switch (id)
        {
            case "Plates":    return InteractiveStackPlates.Scenario.Plates;
            case "Warehouse": return InteractiveStackPlates.Scenario.Warehouse;
            case "Kitchen":   return InteractiveStackPlates.Scenario.Kitchen;
            default:
                Debug.LogWarning($"[SM|Stacks] Unknown '{id}' — default Plates.");
                return InteractiveStackPlates.Scenario.Plates;
        }
    }

    InteractiveCoffeeQueue.ScenarioMode QueueScenarioIdToEnum(string id)
    {
        switch (id)
        {
            case "CoffeeShop":   return InteractiveCoffeeQueue.ScenarioMode.CoffeeShop;
            case "ConveyorBelt": return InteractiveCoffeeQueue.ScenarioMode.ConveyorBelt;
            case "Hospital":     return InteractiveCoffeeQueue.ScenarioMode.Hospital;
            default:
                Debug.LogWarning($"[SM|Queue] Unknown '{id}' — default CoffeeShop.");
                return InteractiveCoffeeQueue.ScenarioMode.CoffeeShop;
        }
    }

    InteractiveTrainList.Scenario LinkedListScenarioIdToEnum(string id)
    {
        switch (id)
        {
            case "Train":     return InteractiveTrainList.Scenario.Train;
            case "Solar":     return InteractiveTrainList.Scenario.Solar;
            case "CityMetro": return InteractiveTrainList.Scenario.CityMetro;
            default:
                Debug.LogWarning($"[SM|LinkedList] Unknown '{id}' — default Train.");
                return InteractiveTrainList.Scenario.Train;
        }
    }

    InteractiveCityGraph.ScenarioMode GraphScenarioIdToEnum(string id)
    {
        switch (id)
        {
            case "CityMap":       return InteractiveCityGraph.ScenarioMode.CityMap;
            case "IslandNetwork": return InteractiveCityGraph.ScenarioMode.IslandNetwork;
            case "SpaceStation":  return InteractiveCityGraph.ScenarioMode.SpaceStation;
            default:
                Debug.LogWarning($"[SM|Graph] Unknown '{id}' — default CityMap.");
                return InteractiveCityGraph.ScenarioMode.CityMap;
        }
    }

    /// <summary>
    /// Maps server-side Trees scenario IDs to InteractiveFamilyTree.ScenarioMode.
    /// Admin panel sends "FamilyTree", "FruitTree", or "ForestTrail".
    /// Difficulty is chosen by the student inside the app via the difficulty panel.
    /// </summary>
    InteractiveFamilyTree.ScenarioMode TreeScenarioIdToEnum(string id)
    {
        switch (id)
        {
            case "FamilyTree":  return InteractiveFamilyTree.ScenarioMode.FamilyTree;
            case "FruitTree":   return InteractiveFamilyTree.ScenarioMode.FruitTree;
            case "ForestTrail": return InteractiveFamilyTree.ScenarioMode.ForestTrail;
            default:
                Debug.LogWarning($"[SM|Trees] Unknown '{id}' — default FamilyTree.");
                return InteractiveFamilyTree.ScenarioMode.FamilyTree;
        }
    }

    // =========================================================================
    // HELPERS
    // =========================================================================
    ScenarioMeta FindMeta(string id)
    {
        foreach (var m in ALL_SCENARIOS)
            if (m.id == id && m.dsKey == dataStructure) return m;
        foreach (var m in ALL_SCENARIOS)
            if (m.id == id) return m;
        return null;
    }

    // =========================================================================
    // JSON RESPONSE MODEL
    // =========================================================================
    [Serializable]
    private class ScenarioResponse
    {
        public bool     success;
        public string[] scenarios;
    }
}