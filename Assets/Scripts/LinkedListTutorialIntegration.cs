using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinkedListTutorialIntegration : MonoBehaviour
{
    [Header("References")]
    public TutorialSystem tutorialSystem;
    public InteractiveTrainList trainController;

    [Header("Tutorial Step IDs")]
    private const string WELCOME = "linkedlist_welcome";
    private const string ADD_HEAD = "linkedlist_add_head";
    private const string ADD_TAIL = "linkedlist_add_tail";
    private const string REMOVE_HEAD = "linkedlist_remove_head";
    private const string TRAVERSE = "linkedlist_traverse";
    private const string MOVEMENT = "linkedlist_movement";
    private const string POINTERS = "linkedlist_pointers";
    private const string RESET = "linkedlist_reset";

    private bool hasShownWelcome = false;

    void Start()
    {
        if (tutorialSystem == null)
            tutorialSystem = FindObjectOfType<TutorialSystem>();

        if (trainController == null)
            trainController = FindObjectOfType<InteractiveTrainList>();

        if (tutorialSystem != null)
        {
            tutorialSystem.SetSceneKey("TrainList");
            SetupTutorials();
        }
    }

    void SetupTutorials()
    {
        tutorialSystem.tutorialSteps.Clear();

        // Welcome tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = WELCOME,
            title = "Welcome to Linked List",
            textContent = "This is a LINKED LIST data structure!\n\n" +
                          "• HEAD: First train car (entry point)\n" +
                          "• NODES: Connected cars with data\n" +
                          "• NEXT: Pointer to next car\n" +
                          "• NULL: End of train marker\n\n" +
                          "LINKED LIST CHARACTERISTICS:\n" +
                          "- Dynamic size (grows/shrinks easily)\n" +
                          "- Non-contiguous memory\n" +
                          "- Each node links to next\n" +
                          "- Fast insert/delete at head O(1)\n" +
                          "- Sequential access only\n\n" +
                          "Like train cars linked together!\n" +
                          "Each car connects to the next!"
        });

        // Add Head tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = ADD_HEAD,
            title = "INSERT AT HEAD ",
            textContent = "HOW TO ADD CAR TO FRONT:\n\n" +
                          "1. Click 'ADD HEAD' button\n" +
                          "2. Enter cargo name (e.g., 'Coal')\n" +
                          "3. Click 'CONFIRM'\n" +
                          "4. Use Left/Right arrows to position car\n" +
                          "5. Move to green target zone\n" +
                          "6. Click CONFIRM when close\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT\n" +
                          "Space Complexity: O(1) - One new node\n" +
                          "Operations: Create node, update pointers\n\n" +
                          "WHY O(1)?\n" +
                          "Only need to update HEAD pointer!\n" +
                          "Don't traverse the list at all.\n" +
                          "Just 3 pointer assignments!\n\n" +
                          "PSEUDOCODE:\n" +
                          "new_car.next = head\n" +
                          "head = new_car\n" +
                          "// Done! No loops needed!\n\n" +
                          "POINTER OPERATIONS:\n" +
                          "1. Create new car node\n" +
                          "2. Point new car to old head\n" +
                          "3. Update head to new car\n" +
                          "Old head becomes second car!\n\n" +
                          "VS ARRAY:\n" +
                          "Array insert at start: O(n) - shift all!\n" +
                          "Linked List: O(1) - just relink!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Browser history (recent first)\n" +
                          "• Undo/Redo stacks\n" +
                          "• Music playlist (add to front)\n" +
                          "• LRU cache implementation"
        });

        // Add Tail tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = ADD_TAIL,
            title = "INSERT AT TAIL ",
            textContent = "HOW TO ADD CAR TO END:\n\n" +
                          "1. Click 'ADD TAIL' button\n" +
                          "2. Enter cargo name\n" +
                          "3. Click 'CONFIRM'\n" +
                          "4. Use Left/Right arrows to position\n" +
                          "5. Move to green target zone\n" +
                          "6. Click CONFIRM when ready\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(n) - LINEAR\n" +
                          "Space Complexity: O(1) - One new node\n" +
                          "Operations: Traverse + insert\n\n" +
                          "WHY O(n)?\n" +
                          "Must TRAVERSE entire train to find tail!\n" +
                          "Start at head, follow next pointers\n" +
                          "until we find the last car.\n\n" +
                          "PSEUDOCODE:\n" +
                          "current = head\n" +
                          "while (current.next != null):\n" +
                          "    current = current.next  // O(n)\n" +
                          "current.next = new_car  // O(1)\n\n" +
                          "WHY NOT O(1)?\n" +
                          "No direct tail pointer in basic list!\n" +
                          "Must walk through n-1 cars.\n" +
                          "Can't skip to end like arrays!\n\n" +
                          "OPTIMIZATION:\n" +
                          "Keep a TAIL pointer and get O(1) insert!\n" +
                          "Trade-off: More memory, more updates\n\n" +
                          "VS ARRAY:\n" +
                          "Array append (with space): O(1)\n" +
                          "Linked List: O(n) without tail pointer\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Append to log files\n" +
                          "• Add to playlist end\n" +
                          "• Queue implementation\n" +
                          "• Event history chronological order"
        });

        // Remove Head tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = REMOVE_HEAD,
            title = "REMOVE HEAD ",
            textContent = "HOW TO REMOVE FRONT CAR:\n\n" +
                          "1. Click 'REMOVE HEAD' button\n" +
                          "2. First car becomes movable\n" +
                          "3. Use Left/Right arrows to move\n" +
                          "4. Move to red EXIT zone\n" +
                          "5. Click CONFIRM to complete\n" +
                          "6. Next car becomes new HEAD!\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT\n" +
                          "Space Complexity: O(1) - No extra space\n" +
                          "Operations: Update head pointer, delete\n\n" +
                          "WHY O(1)?\n" +
                          "Just update HEAD pointer!\n" +
                          "Move head to next car.\n" +
                          "No traversal needed!\n\n" +
                          "PSEUDOCODE:\n" +
                          "temp = head\n" +
                          "head = head.next\n" +
                          "delete temp\n" +
                          "// That's it! Very fast!\n\n" +
                          "POINTER OPERATIONS:\n" +
                          "1. Save reference to old head\n" +
                          "2. Move head to second car\n" +
                          "3. Delete old head car\n" +
                          "Second car becomes first!\n\n" +
                          "EDGE CASES:\n" +
                          "If only 1 car: head = null (empty list)\n" +
                          "Always check for null!\n\n" +
                          "VS ARRAY:\n" +
                          "Array remove first: O(n) - shift all!\n" +
                          "Linked List: O(1) - just relink!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Dequeue operations\n" +
                          "• Process first task in queue\n" +
                          "• Remove oldest cache entry\n" +
                          "• Pop from stack (if stack = list)"
        });

        // Traverse tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = TRAVERSE,
            title = "TRAVERSE Operation ",
            textContent = "HOW TO VISIT ALL CARS:\n\n" +
                          "1. Click 'TRAVERSE' button\n" +
                          "2. Watch each car highlight\n" +
                          "3. See cargo and position display\n" +
                          "4. Follows NEXT pointers in order\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(n) - LINEAR\n" +
                          "Space Complexity: O(1) - Constant\n" +
                          "Operations: Visit each node once\n\n" +
                          "WHY O(n)?\n" +
                          "Must visit EVERY single car!\n" +
                          "No shortcuts - follow each link.\n" +
                          "n cars = n visits required!\n\n" +
                          "PSEUDOCODE:\n" +
                          "current = head\n" +
                          "while (current != null):\n" +
                          "    visit(current)  // Process car\n" +
                          "    current = current.next\n\n" +
                          "HOW IT WORKS:\n" +
                          "1. Start at HEAD\n" +
                          "2. Process current car\n" +
                          "3. Move to NEXT car\n" +
                          "4. Repeat until NEXT is NULL\n" +
                          "5. NULL means end of train!\n\n" +
                          "OPERATIONS COUNT:\n" +
                          "3 cars = 3 visits\n" +
                          "10 cars = 10 visits\n" +
                          "n cars = n visits\n" +
                          "Linear growth!\n\n" +
                          "VS ARRAY:\n" +
                          "Array traversal: O(n) - same!\n" +
                          "But array has random access advantage.\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Print all list items\n" +
                          "• Search for specific element\n" +
                          "• Calculate list statistics\n" +
                          "• Display playlist contents"
        });

        // Pointers tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = POINTERS,
            title = "How Pointers Work",
            textContent = "UNDERSTANDING NEXT POINTERS:\n\n" +
                          "POINTER STRUCTURE:\n" +
                          "Each train car node contains:\n" +
                          "• DATA: Cargo information\n" +
                          "• NEXT: Pointer to next car\n\n" +
                          "VISUAL REPRESENTATION:\n" +
                          "[Coal->] -> [Grain->] -> [Steel->] -> NULL\n" +
                          " ^\n" +
                          "HEAD\n\n" +
                          "KEY COMPONENTS:\n" +
                          "• HEAD: Points to first car (entry)\n" +
                          "• NEXT: Links each car together\n" +
                          "• NULL: Marks the end of train\n\n" +
                          "MEMORY LAYOUT:\n" +
                          "Cars can be ANYWHERE in memory!\n" +
                          "Pointers connect them logically.\n" +
                          "Not physically contiguous like arrays.\n\n" +
                          "LINKING PROCESS:\n" +
                          "1. Car A created at address 100\n" +
                          "2. Car B created at address 500\n" +
                          "3. Car A.next = 500 (link!)\n" +
                          "4. Cars now connected!\n\n" +
                          "BREAKING LINKS:\n" +
                          "If Car A.next = null:\n" +
                          "• Connection to Car B lost!\n" +
                          "• Car B becomes unreachable!\n" +
                          "• Rest of train disconnected!\n\n" +
                          "CONNECTORS IN DEMO:\n" +
                          "Visual lines show the NEXT pointers!\n" +
                          "Watch them appear when cars link!\n\n" +
                          "WHY POINTERS MATTER:\n" +
                          "• Enable O(1) insert/delete at head\n" +
                          "• Allow dynamic size changes\n" +
                          "• No wasted space like arrays\n" +
                          "• Flexible memory usage"
        });

        // Movement tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = MOVEMENT,
            title = "Movement Controls",
            textContent = "MOVING TRAIN CARS:\n\n" +
                          "LEFT - Move car left\n" +
                          "RIGHT - Move car right\n\n" +
                          "TARGET ZONES:\n" +
                          "Green = Add target\n" +
                          "   Position car here to attach to train\n\n" +
                          "Red = Remove zone\n" +
                          "   Move car here to detach from train\n\n" +
                          "STAY ON TRACKS:\n" +
                          "Cars automatically align with tracks!\n" +
                          "Left/Right movement only.\n\n" +
                          "FEATURES:\n" +
                          "• Hold buttons for continuous movement\n" +
                          "• Release to stop\n" +
                          "• Visual indicators guide placement\n" +
                          "• CONFIRM appears when close\n\n" +
                          "Click CANCEL to abort operation\n\n" +
                          "TIP:\n" +
                          "Watch the connector lines!\n" +
                          "They show how cars will link together."
        });

        // Reset tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = RESET,
            title = "RESET Scene",
            textContent = "START OVER:\n\n" +
                          "Click RESET button to:\n" +
                          "• Clear entire train (all cars)\n" +
                          "• Set HEAD = null (empty list)\n" +
                          "• Break all NEXT pointers\n" +
                          "• Remove scene from AR space\n\n" +
                          "TO RESPAWN:\n" +
                          "Point camera at flat surface\n" +
                          "(floor or table) to spawn new train!\n\n" +
                          "LEARNING EXERCISES:\n" +
                          "Master linked lists with these:\n\n" +
                          "1. HEAD INSERT SPEED:\n" +
                          "   Add 5 cars at head.\n" +
                          "   Notice it's always fast! (O(1))\n\n" +
                          "2. TAIL INSERT COMPARISON:\n" +
                          "   Add 5 cars at tail.\n" +
                          "   Notice each takes longer! (O(n))\n\n" +
                          "3. TRAVERSAL:\n" +
                          "   Fill train, then traverse.\n" +
                          "   Count how many cars visited.\n\n" +
                          "4. HEAD REMOVAL:\n" +
                          "   Add 3 cars, remove head.\n" +
                          "   What becomes new head?\n\n" +
                          "CHALLENGE:\n" +
                          "Can you explain why removing from\n" +
                          "tail would be O(n) instead of O(1)?\n" +
                          "Hint: How do you find the tail?"
        });
    }

    public void ShowWelcomeTutorial()
    {
        if (!hasShownWelcome && tutorialSystem != null)
        {
            tutorialSystem.ShowTutorial(WELCOME);
            hasShownWelcome = true;
        }
    }

    public void OnAddHeadButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(ADD_HEAD);
    }

    public void OnAddTailButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(ADD_TAIL);
    }

    public void OnRemoveHeadButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(REMOVE_HEAD);
    }

    public void OnTraverseButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(TRAVERSE);
    }

    public void OnPointersExplainButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(POINTERS);
    }

    public void OnMovementStarted()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(MOVEMENT);
    }

    public void OnResetButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(RESET);
    }

    public void ResetTutorials()
    {
        if (tutorialSystem != null)
        {
            tutorialSystem.ResetAllTutorials();
            hasShownWelcome = false;
        }
    }
}