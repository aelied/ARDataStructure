using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeTutorialIntegration : MonoBehaviour
{
    [Header("References")]
    public TutorialSystem tutorialSystem;
    public InteractiveFamilyTree treeController;

    [Header("Tutorial Step IDs")]
    private const string WELCOME = "tree_welcome";
    private const string ADD_CHILD = "tree_add_child";
    private const string POSITIONING = "tree_positioning";
    private const string SNAPPING = "tree_snapping";
    private const string INORDER = "tree_inorder";
    private const string PREORDER = "tree_preorder";
    private const string POSTORDER = "tree_postorder";
    private const string MOVEMENT = "tree_movement";
    private const string RESET = "tree_reset";

    private bool hasShownWelcome = false;

    void Start()
    {
        if (tutorialSystem == null)
            tutorialSystem = FindObjectOfType<TutorialSystem>();

        if (treeController == null)
            treeController = FindObjectOfType<InteractiveFamilyTree>();

        if (tutorialSystem != null)
        {
            tutorialSystem.SetSceneKey("FamilyTree");
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
            title = "Welcome to Binary Tree",
            textContent = "This is a BINARY TREE structure!\n\n" +
                          "• ROOT: Ancestor at top (entry point)\n" +
                          "• NODES: Family members (data holders)\n" +
                          "• CHILDREN: Left & Right (max 2)\n" +
                          "• PARENT: Node above\n" +
                          "• LEAF: Node with no children\n\n" +
                          "BINARY TREE CHARACTERISTICS:\n" +
                          "- Hierarchical structure\n" +
                          "- Each node has max 2 children\n" +
                          "- Efficient searching in BST form\n" +
                          "- Natural recursive operations\n" +
                          "- Multiple traversal orders\n\n" +
                          "Like a real family tree!\n" +
                          "But each parent has only 2 children!"
        });

        // Add Child tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = ADD_CHILD,
            title = "ADD CHILD ",
            textContent = "HOW TO ADD A FAMILY MEMBER:\n\n" +
                          "1. Click 'ADD CHILD' button\n" +
                          "2. Enter person's name\n" +
                          "3. Click 'SELECT' to start positioning\n" +
                          "4. Move near a circle indicator\n" +
                          "5. Node will SNAP when close!\n" +
                          "6. Click CONFIRM when snapped\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(log n) AVERAGE\n" +
                          "Worst Case: O(n) if unbalanced\n" +
                          "Space Complexity: O(1) - One new node\n" +
                          "Operations: Traverse + insert\n\n" +
                          "WHY O(log n) AVERAGE?\n" +
                          "In balanced tree: height = log(n)\n" +
                          "Must traverse from root to insertion point.\n" +
                          "Each level halves remaining nodes!\n\n" +
                          "SEARCH LOGIC:\n" +
                          "1. Start at root\n" +
                          "2. Choose left or right child\n" +
                          "3. Repeat until empty spot found\n" +
                          "4. Insert new node there\n\n" +
                          "BALANCED VS UNBALANCED:\n" +
                          "Balanced: Height = log(n)\n" +
                          "   Insert time = O(log n)\n" +
                          "Unbalanced: Height = n (like list)\n" +
                          "   Insert time = O(n)\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• File system directories\n" +
                          "• HTML DOM structure\n" +
                          "• Expression trees (math)\n" +
                          "• Decision trees (AI)"
        });

        // Positioning tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = POSITIONING,
            title = "Positioning Nodes",
            textContent = "MOVE THE PREVIEW NODE:\n\n" +
                          "UP - Move node upward\n" +
                          "DOWN - Move node downward\n" +
                          "LEFT - Move node left\n" +
                          "RIGHT - Move node right\n\n" +
                          "YOUR GOAL:\n" +
                          "Move preview close to a circle indicator!\n" +
                          "• LEFT circles = left child slot\n" +
                          "• RIGHT circles = right child slot\n\n" +
                          "MAGIC HAPPENS:\n" +
                          "Get close and node SNAPS automatically!\n" +
                          "Circle turns GREEN = Ready to confirm!\n\n" +
                          "TIP:\n" +
                          "Small movements = precise control\n" +
                          "Keep adjusting until snap occurs!\n\n" +
                          "MAGNETIC SNAPPING:\n" +
                          "Helps ensure correct placement!\n" +
                          "Prevents accidental misalignment.\n" +
                          "Snap distance: 0.03 units"
        });

        // Snapping tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = SNAPPING,
            title = "Magnetic Snapping Explained",
            textContent = "HOW SNAPPING WORKS:\n\n" +
                          "INDICATOR TYPES:\n" +
                          "Blue = left child position\n" +
                          "Orange = right child position\n\n" +
                          "SNAP SEQUENCE:\n" +
                          "1. Move preview near indicator\n" +
                          "2. Within 0.03 units = SNAP!\n" +
                          "3. Indicator turns GREEN\n" +
                          "4. CONFIRM button appears\n" +
                          "5. Preview locks to position\n\n" +
                          "VALIDATION:\n" +
                          "• MUST be snapped to confirm!\n" +
                          "• Prevents wrong placements\n" +
                          "• Ensures tree structure integrity\n" +
                          "• Visual feedback at every step\n\n" +
                          "CAN'T CONFIRM IF:\n" +
                          "X Not close enough to indicator\n" +
                          "X Indicator hasn't turned green\n" +
                          "X Preview isn't locked in place\n\n" +
                          "WHY THIS MATTERS:\n" +
                          "Binary tree has strict rules:\n" +
                          "• Each node max 2 children\n" +
                          "• Must specify left vs right\n" +
                          "• Position affects tree structure\n\n" +
                          "GREEN = GOOD TO GO!\n" +
                          "When indicator glows green,\n" +
                          "you're perfectly positioned!"
        });

        // In-Order tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = INORDER,
            title = "IN-ORDER Traversal - Algorithm",
            textContent = "IN-ORDER TRAVERSAL:\n\n" +
                          "ORDER: LEFT -> ROOT -> RIGHT\n\n" +
                          "1. Click 'IN-ORDER' button\n" +
                          "2. Watch yellow highlights\n" +
                          "3. See visit order in instructions\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(n) - LINEAR\n" +
                          "Space Complexity: O(h) - Height\n" +
                          "Operations: Visit all n nodes once\n\n" +
                          "WHY O(n)?\n" +
                          "Must visit EVERY node exactly once!\n" +
                          "n nodes = n visits required.\n" +
                          "No shortcuts possible!\n\n" +
                          "RECURSIVE ALGORITHM:\n" +
                          "InOrder(node):\n" +
                          "    if node == null: return\n" +
                          "    InOrder(node.left)    // Visit left\n" +
                          "    Visit(node)           // Visit root\n" +
                          "    InOrder(node.right)   // Visit right\n\n" +
                          "SPACE COMPLEXITY O(h):\n" +
                          "h = height of tree\n" +
                          "Recursion uses call stack.\n" +
                          "Max depth = height!\n" +
                          "Balanced tree: h = log(n)\n" +
                          "Worst case: h = n\n\n" +
                          "SPECIAL PROPERTY:\n" +
                          "For Binary Search Tree (BST):\n" +
                          "In-order gives SORTED sequence!\n" +
                          "Example: 1, 3, 4, 6, 7, 9\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Get sorted data from BST\n" +
                          "• Flatten tree to array\n" +
                          "• Validate BST property\n" +
                          "• Expression evaluation"
        });

        // Pre-Order tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = PREORDER,
            title = "PRE-ORDER Traversal - Algorithm",
            textContent = "PRE-ORDER TRAVERSAL:\n\n" +
                          "ORDER: ROOT -> LEFT -> RIGHT\n\n" +
                          "1. Click 'PRE-ORDER' button\n" +
                          "2. Watch parent visited first!\n" +
                          "3. Then children in sequence\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(n) - LINEAR\n" +
                          "Space Complexity: O(h) - Height\n" +
                          "Operations: Visit all n nodes once\n\n" +
                          "WHY O(n)?\n" +
                          "Same as in-order: visit all nodes!\n" +
                          "Different order, same count.\n\n" +
                          "RECURSIVE ALGORITHM:\n" +
                          "PreOrder(node):\n" +
                          "    if node == null: return\n" +
                          "    Visit(node)            // Visit root FIRST!\n" +
                          "    PreOrder(node.left)    // Then left\n" +
                          "    PreOrder(node.right)   // Then right\n\n" +
                          "TOP-DOWN APPROACH:\n" +
                          "Processes parent BEFORE children!\n" +
                          "Like reading a family hierarchy\n" +
                          "from ancestors downward.\n\n" +
                          "VISIT EXAMPLE:\n" +
                          "        A\n" +
                          "       / \\\n" +
                          "      B   C\n" +
                          "     / \\\n" +
                          "    D   E\n" +
                          "Pre-order: A, B, D, E, C\n\n" +
                          "KEY USE CASE:\n" +
                          "COPY/CLONE A TREE!\n" +
                          "Process parent first to create it,\n" +
                          "then attach children.\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Copy tree structure\n" +
                          "• Serialize tree to file\n" +
                          "• Prefix expression parsing\n" +
                          "• Directory listing (top-down)"
        });

        // Post-Order tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = POSTORDER,
            title = "POST-ORDER Traversal - Algorithm",
            textContent = "POST-ORDER TRAVERSAL:\n\n" +
                          "ORDER: LEFT -> RIGHT -> ROOT\n\n" +
                          "1. Click 'POST-ORDER' button\n" +
                          "2. Watch children visited first!\n" +
                          "3. Parent processed last\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(n) - LINEAR\n" +
                          "Space Complexity: O(h) - Height\n" +
                          "Operations: Visit all n nodes once\n\n" +
                          "WHY O(n)?\n" +
                          "Still visiting every node once!\n" +
                          "Order changes, complexity doesn't.\n\n" +
                          "RECURSIVE ALGORITHM:\n" +
                          "PostOrder(node):\n" +
                          "    if node == null: return\n" +
                          "    PostOrder(node.left)   // Left first\n" +
                          "    PostOrder(node.right)  // Then right\n" +
                          "    Visit(node)            // Root LAST!\n\n" +
                          "BOTTOM-UP APPROACH:\n" +
                          "Processes children BEFORE parent!\n" +
                          "Like calculating from leaves upward.\n\n" +
                          "VISIT EXAMPLE:\n" +
                          "        A\n" +
                          "       / \\\n" +
                          "      B   C\n" +
                          "     / \\\n" +
                          "    D   E\n" +
                          "Post-order: D, E, B, C, A\n\n" +
                          "KEY USE CASES:\n" +
                          "DELETE A TREE SAFELY!\n" +
                          "Delete children first, then parent.\n" +
                          "Prevents orphaned nodes!\n\n" +
                          "CALCULATE TREE PROPERTIES!\n" +
                          "Height, size, sum computed bottom-up.\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Delete tree safely\n" +
                          "• Calculate expression trees\n" +
                          "• Postfix expression evaluation\n" +
                          "• Free memory (delete nodes)\n" +
                          "• Compute folder sizes"
        });

        // Movement tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = MOVEMENT,
            title = "Movement Controls",
            textContent = "DIRECTIONAL BUTTONS:\n\n" +
                          "UP - Move node upward\n" +
                          "DOWN - Move node downward\n" +
                          "LEFT - Move node left\n" +
                          "RIGHT - Move node right\n\n" +
                          "GOAL:\n" +
                          "Position preview near indicators!\n" +
                          "Get close enough to trigger SNAP!\n\n" +
                          "VISUAL FEEDBACK:\n" +
                          "• Blue circle = LEFT child slot\n" +
                          "• Orange circle = RIGHT child slot\n" +
                          "• Green = SNAPPED and ready!\n" +
                          "• Preview becomes semi-transparent\n\n" +
                          "TIPS:\n" +
                          "• Hold buttons for continuous movement\n" +
                          "• Release to stop\n" +
                          "• Small adjustments work best\n" +
                          "• Watch for color change!\n\n" +
                          "Click CANCEL to abort operation\n\n" +
                          "PRECISION:\n" +
                          "Take your time!\n" +
                          "Proper positioning ensures\n" +
                          "correct tree structure."
        });

        // Reset tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = RESET,
            title = "RESET Scene",
            textContent = "START OVER:\n\n" +
                          "Click RESET button to:\n" +
                          "• Clear entire tree\n" +
                          "• Remove all nodes except root\n" +
                          "• Break all parent-child links\n" +
                          "• Remove scene from AR space\n\n" +
                          "TO RESPAWN:\n" +
                          "Point camera at flat surface\n" +
                          "to spawn new family tree!\n\n" +
                          "LEARNING EXERCISES:\n" +
                          "Master binary trees with these:\n\n" +
                          "1. BALANCED TREE:\n" +
                          "   Create a perfectly balanced tree.\n" +
                          "   Each level fully filled!\n\n" +
                          "2. SKEWED TREE:\n" +
                          "   Only add LEFT children.\n" +
                          "   Creates linked-list shape!\n\n" +
                          "3. TRAVERSAL COMPARISON:\n" +
                          "   Run all 3 traversals on same tree.\n" +
                          "   Note different visit orders!\n\n" +
                          "4. HEIGHT CALCULATION:\n" +
                          "   Count levels in your tree.\n" +
                          "   Height = longest path to leaf.\n\n" +
                          "CHALLENGE QUESTIONS:\n" +
                          "• Why is balanced tree better?\n" +
                          "• What's max nodes at level 3?\n" +
                          "• How to make tree into BST?"
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

    public void OnAddChildButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(ADD_CHILD);
    }

    public void OnPositioningStarted()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(POSITIONING);
    }

    public void OnSnappingDetected()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(SNAPPING);
    }

    public void OnInOrderButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(INORDER);
    }

    public void OnPreOrderButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(PREORDER);
    }

    public void OnPostOrderButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(POSTORDER);
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