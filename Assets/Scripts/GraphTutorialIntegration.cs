using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphTutorialIntegration : MonoBehaviour
{
    [Header("References")]
    public TutorialSystem tutorialSystem;
    public InteractiveCityGraph graphController;

    [Header("Tutorial Step IDs")]
    private const string WELCOME    = "graph_welcome";
    private const string ADD_NODE   = "graph_add_node";
    private const string ADD_EDGE   = "graph_add_edge";
    private const string REMOVE_NODE = "graph_remove_node";
    private const string BFS        = "graph_bfs";
    private const string DFS        = "graph_dfs";
    private const string DIJKSTRA   = "graph_dijkstra";
    private const string MOVEMENT   = "graph_movement";
    private const string RESET      = "graph_reset";

    private bool hasShownWelcome = false;

    void Start()
    {
        if (tutorialSystem  == null) tutorialSystem  = FindObjectOfType<TutorialSystem>();
        if (graphController == null) graphController = FindObjectOfType<InteractiveCityGraph>();

        if (tutorialSystem != null)
        {
            // FIX: Do NOT call SetSceneKey() here anymore.
            // Set the sceneKey field to "CityGraph" directly in the TutorialSystem
            // Inspector. Calling SetSceneKey() after Start() re-runs
            // LoadTutorialProgress() under a different key, breaking persistence.
            SetupTutorials();
        }
    }

    void SetupTutorials()
    {
        tutorialSystem.tutorialSteps.Clear();

        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = WELCOME,
            title = "Welcome to Graph Data Structure",
            textContent = "This is an interactive GRAPH!\n\n" +
                          "• VERTICES (Nodes): City buildings\n" +
                          "• EDGES: Roads connecting cities\n" +
                          "• WEIGHTS: Distance/cost between cities\n" +
                          "• DIRECTED: One-way or two-way roads\n\n" +
                          "GRAPH CHARACTERISTICS:\n" +
                          "- Shows relationships & connections\n" +
                          "- Non-linear structure\n" +
                          "- Flexible connections (any to any)\n" +
                          "- Can have cycles\n" +
                          "- Real-world network modeling\n\n" +
                          "Like a real city map!\n" +
                          "Roads connect buildings in complex ways!"
        });

        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = ADD_NODE,
            title = "ADD VERTEX (Node) ",
            textContent = "HOW TO ADD A CITY:\n\n" +
                          "1. Click 'ADD NODE' button\n" +
                          "2. Enter city name\n" +
                          "3. Click 'SELECT' to start positioning\n" +
                          "4. Use arrow buttons to move:\n" +
                          "   Up / Down / Left / Right\n" +
                          "5. Position anywhere in space\n" +
                          "6. Click CONFIRM when positioned\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT\n" +
                          "Space Complexity: O(1) - One vertex\n\n" +
                          "WHY O(1)?\n" +
                          "Just add new vertex to collection!\n" +
                          "No searching or traversal needed.\n\n" +
                          "PSEUDOCODE:\n" +
                          "AddVertex(name):\n" +
                          "    vertex = new Vertex(name)\n" +
                          "    graph.vertices.add(vertex)  // O(1)\n" +
                          "    return vertex\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Add new location to map\n" +
                          "• New user in social network\n" +
                          "• New router in network"
        });

        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = ADD_EDGE,
            title = "ADD EDGE (Road) ",
            textContent = "HOW TO CONNECT CITIES:\n\n" +
                          "1. Click 'ADD EDGE' button\n" +
                          "2. Enter FROM city name\n" +
                          "3. Enter TO city name\n" +
                          "4. Click 'CONNECT'\n" +
                          "5. Road appears with distance!\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) AVERAGE\n" +
                          "Worst Case: O(V) if checking duplicates\n\n" +
                          "PSEUDOCODE (Adjacency List):\n" +
                          "AddEdge(from, to, weight):\n" +
                          "    edge = new Edge(to, weight)\n" +
                          "    from.edges.append(edge)  // O(1)\n" +
                          "    if undirected:\n" +
                          "        to.edges.append(new Edge(from, weight))\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Add road between cities\n" +
                          "• Connect users (friend request)\n" +
                          "• Link web pages (hyperlink)"
        });

        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = REMOVE_NODE,
            title = "REMOVE VERTEX ",
            textContent = "HOW TO REMOVE A CITY:\n\n" +
                          "1. Click 'REMOVE NODE' button\n" +
                          "2. Enter city name to remove\n" +
                          "3. Click 'CONFIRM REMOVE'\n" +
                          "4. City AND all connected roads vanish!\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(V + E) - LINEAR\n" +
                          "Must check ALL edges in the graph!\n\n" +
                          "PSEUDOCODE:\n" +
                          "RemoveVertex(target):\n" +
                          "    for each vertex v in graph:  // O(V)\n" +
                          "        remove edges pointing to target  // O(E)\n" +
                          "    graph.vertices.remove(target)  // O(1)\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Close airport (remove + reroute)\n" +
                          "• Delete user (remove + unfriend all)"
        });

        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = BFS,
            title = "BFS Algorithm - Analysis",
            textContent = "BREADTH-FIRST SEARCH:\n\n" +
                          "Explores neighbors level by level.\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(V + E)\n" +
                          "Space Complexity: O(V) - Queue storage\n\n" +
                          "ALGORITHM:\n" +
                          "BFS(start):\n" +
                          "    queue = [start]\n" +
                          "    visited = {start}\n" +
                          "    while queue not empty:\n" +
                          "        current = queue.dequeue()\n" +
                          "        visit(current)\n" +
                          "        for each neighbor of current:\n" +
                          "            if neighbor not in visited:\n" +
                          "                visited.add(neighbor)\n" +
                          "                queue.enqueue(neighbor)\n\n" +
                          "KEY PROPERTIES:\n" +
                          "• Finds SHORTEST PATH (unweighted)!\n" +
                          "• Uses QUEUE (FIFO) data structure\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Social network friend suggestions\n" +
                          "• GPS shortest path (unweighted)"
        });

        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = DFS,
            title = "DFS Algorithm - Analysis",
            textContent = "DEPTH-FIRST SEARCH:\n\n" +
                          "Goes as deep as possible first!\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(V + E)\n" +
                          "Space Complexity: O(V) - Recursion stack\n\n" +
                          "RECURSIVE ALGORITHM:\n" +
                          "DFS(current, visited):\n" +
                          "    visited.add(current)\n" +
                          "    visit(current)\n" +
                          "    for each neighbor of current:\n" +
                          "        if neighbor not in visited:\n" +
                          "            DFS(neighbor, visited)\n\n" +
                          "KEY PROPERTIES:\n" +
                          "• Uses STACK (LIFO) or recursion\n" +
                          "• Good for cycle detection\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Maze solving\n" +
                          "• Topological sorting"
        });

        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = DIJKSTRA,
            title = "Dijkstra's Algorithm - Analysis",
            textContent = "SHORTEST PATH ALGORITHM:\n\n" +
                          "Finds cheapest route between two nodes.\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O((V + E) log V)\n" +
                          "Space Complexity: O(V)\n\n" +
                          "ALGORITHM:\n" +
                          "Dijkstra(start, end):\n" +
                          "    distances[start] = 0\n" +
                          "    distances[others] = infinity\n" +
                          "    heap = [(0, start)]\n" +
                          "    while heap not empty:\n" +
                          "        dist, current = heap.extract_min()\n" +
                          "        if current == end: break\n" +
                          "        for each neighbor, weight:\n" +
                          "            new_dist = dist + weight\n" +
                          "            if new_dist < distances[neighbor]:\n" +
                          "                distances[neighbor] = new_dist\n" +
                          "                heap.insert(new_dist, neighbor)\n\n" +
                          "LIMITATION: No negative weights!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• GPS navigation (Google Maps!)\n" +
                          "• Network routing protocols"
        });

        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = MOVEMENT,
            title = "Movement Controls",
            textContent = "POSITIONING CITIES:\n\n" +
                          "UP    - Move forward\n" +
                          "DOWN  - Move backward\n" +
                          "LEFT  - Move left\n" +
                          "RIGHT - Move right\n\n" +
                          "TIPS:\n" +
                          "• Spread cities out for clarity\n" +
                          "• Avoid overlapping buildings\n" +
                          "• Hold buttons for continuous movement\n\n" +
                          "Click CANCEL to abort the operation."
        });

        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = RESET,
            title = "RESET Scene",
            textContent = "START FRESH:\n\n" +
                          "Click RESET to:\n" +
                          "• Clear all cities (vertices)\n" +
                          "• Remove all roads (edges)\n" +
                          "• Remove scene from AR space\n\n" +
                          "TO RESPAWN:\n" +
                          "Point camera at flat surface.\n\n" +
                          "CHALLENGE EXERCISES:\n" +
                          "• Build a complete graph\n" +
                          "• Create a disconnected graph\n" +
                          "• Compare BFS vs DFS paths\n" +
                          "• Test Dijkstra on multiple routes"
        });
    }

    // ── Public callbacks (called by InteractiveCityGraph) ──────────────────

    public void ShowWelcomeTutorial()
    {
        if (!hasShownWelcome && tutorialSystem != null)
        {
            tutorialSystem.ShowTutorial(WELCOME);
            hasShownWelcome = true;
        }
    }

    public void OnAddNodeButtonClicked()     { tutorialSystem?.ShowTutorial(ADD_NODE);    }
    public void OnAddEdgeButtonClicked()     { tutorialSystem?.ShowTutorial(ADD_EDGE);    }
    public void OnRemoveNodeButtonClicked()  { tutorialSystem?.ShowTutorial(REMOVE_NODE); }
    public void OnBFSButtonClicked()         { tutorialSystem?.ShowTutorial(BFS);         }
    public void OnDFSButtonClicked()         { tutorialSystem?.ShowTutorial(DFS);         }
    public void OnDijkstraButtonClicked()    { tutorialSystem?.ShowTutorial(DIJKSTRA);    }
    public void OnMovementStarted()          { tutorialSystem?.ShowTutorial(MOVEMENT);    }
    public void OnResetButtonClicked()       { tutorialSystem?.ShowTutorial(RESET);       }

    public void ResetTutorials()
    {
        if (tutorialSystem != null)
        {
            tutorialSystem.ResetAllTutorials();
            hasShownWelcome = false;
        }
    }
}