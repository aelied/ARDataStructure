using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackTutorialIntegration : MonoBehaviour
{
    [Header("References")]
    public TutorialSystem tutorialSystem;
    public InteractiveStackPlates stackController;

    [Header("Tutorial Step IDs")]
    private const string WELCOME = "stack_welcome";
    private const string PUSH = "stack_push";
    private const string POP = "stack_pop";
    private const string PEEK = "stack_peek";
    private const string MOVEMENT = "stack_movement";
    private const string LIFO = "stack_lifo";
    private const string RESET = "stack_reset";

    private bool hasShownWelcome = false;

    void Start()
    {
        if (tutorialSystem == null)
            tutorialSystem = FindObjectOfType<TutorialSystem>();

        if (stackController == null)
            stackController = FindObjectOfType<InteractiveStackPlates>();

        if (tutorialSystem != null)
        {
            tutorialSystem.SetSceneKey("StackPlates");
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
            title = "Welcome to Stack Data Structure",
            textContent = "This is an interactive STACK demonstration!\n\n" +
                          "• PUSH: Add plate to TOP\n" +
                          "• POP: Remove plate from TOP\n" +
                          "• PEEK: View top plate (read-only)\n\n" +
                          "STACK CHARACTERISTICS:\n" +
                          "- LIFO: Last In, First Out\n" +
                          "- Only access the TOP element\n" +
                          "- Dynamic size (grows/shrinks)\n" +
                          "- All operations O(1)\n\n" +
                          "Think of it like stacking dishes!\n" +
                          "You can only add or take from the top!"
        });

        // PUSH tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = PUSH,
            title = "PUSH Operation ",
            textContent = "HOW TO ADD A PLATE:\n\n" +
                          "1. Click 'PUSH' button\n" +
                          "2. New plate appears on side\n" +
                          "3. Use arrow buttons to move:\n" +
                          "   Left / Right\n" +
                          "   Forward / Back\n" +
                          "4. Position over green circle\n" +
                          "5. Click CONFIRM when ready\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT\n" +
                          "Space Complexity: O(1) - Single element\n" +
                          "Operations: Update top pointer, add item\n\n" +
                          "WHY O(1)?\n" +
                          "Stack only modifies the TOP pointer!\n" +
                          "No need to traverse or shift elements.\n" +
                          "Just increment top and place item!\n\n" +
                          "PSEUDOCODE:\n" +
                          "top = top + 1\n" +
                          "stack[top] = new_element\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Function call stack in programming\n" +
                          "• Browser back button history\n" +
                          "• Undo/Redo operations in editors\n" +
                          "• Expression evaluation (calculators)"
        });

        // POP tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = POP,
            title = "POP Operation ",
            textContent = "HOW TO REMOVE A PLATE:\n\n" +
                          "1. Click 'POP' button\n" +
                          "2. Top plate becomes movable\n" +
                          "3. Use arrows to move away\n" +
                          "4. Move to red REMOVE zone\n" +
                          "5. Click CONFIRM to complete\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT\n" +
                          "Space Complexity: O(1) - No extra space\n" +
                          "Operations: Read top, decrement pointer\n\n" +
                          "WHY O(1)?\n" +
                          "Always remove from TOP - no searching!\n" +
                          "Just return top element and decrement.\n" +
                          "No shifting of other elements needed!\n\n" +
                          "PSEUDOCODE:\n" +
                          "element = stack[top]\n" +
                          "top = top - 1\n" +
                          "return element\n\n" +
                          "LIFO RULE:\n" +
                          "Can ONLY remove from TOP!\n" +
                          "Last plate added = First plate removed!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Function returns (call stack)\n" +
                          "• Backtracking in games/puzzles\n" +
                          "• Parsing expressions/syntax\n" +
                          "• Memory management"
        });

        // PEEK tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = PEEK,
            title = "PEEK Operation ",
            textContent = "HOW TO VIEW TOP PLATE:\n\n" +
                          "1. Click 'PEEK' button\n" +
                          "2. Top plate glows cyan\n" +
                          "3. See its position number\n" +
                          "4. Stack remains unchanged!\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT\n" +
                          "Space Complexity: O(1) - Read only\n" +
                          "Operations: Return stack[top]\n\n" +
                          "WHY O(1)?\n" +
                          "Direct access to top pointer!\n" +
                          "Just read the value, no modification.\n" +
                          "No traversal needed!\n\n" +
                          "PSEUDOCODE:\n" +
                          "if (top >= 0):\n" +
                          "    return stack[top]\n" +
                          "else:\n" +
                          "    return 'Stack Empty'\n\n" +
                          "KEY FEATURE:\n" +
                          "PEEK is NON-DESTRUCTIVE!\n" +
                          "Look without removing!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Check next undo action\n" +
                          "• Preview top of deck in games\n" +
                          "• Validate stack state\n" +
                          "• Debugging stack contents"
        });

        // LIFO tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = LIFO,
            title = "LIFO Principle Explained",
            textContent = "LAST IN, FIRST OUT!\n\n" +
                          "THE CORE CONCEPT:\n" +
                          "The LAST plate you add\n" +
                          "is the FIRST plate you remove!\n\n" +
                          "REAL-WORLD ANALOGY:\n" +
                          "Think of a stack of dishes:\n" +
                          "• Can't take from middle (would break!)\n" +
                          "• Can't take from bottom (unstable!)\n" +
                          "• MUST take from top (safe & easy!)\n\n" +
                          "OPERATIONS:\n" +
                          "Add to top = PUSH\n" +
                          "Take from top = POP\n\n" +
                          "RESTRICTIONS:\n" +
                          "X Can't access middle elements\n" +
                          "X Can't access bottom elements\n" +
                          "✓ ONLY access top element!\n\n" +
                          "WHY LIFO?\n" +
                          "• Simplifies implementation\n" +
                          "• Guarantees O(1) operations\n" +
                          "• Natural for many algorithms\n\n" +
                          "WHEN TO USE STACKS:\n" +
                          "• Reverse operations (undo)\n" +
                          "• Nested structures (parentheses)\n" +
                          "• Backtracking problems\n" +
                          "• Function call management"
        });

        // Movement tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = MOVEMENT,
            title = "Movement Controls",
            textContent = "MOVING PLATES:\n\n" +
                          "LEFT - Move plate left\n" +
                          "RIGHT - Move plate right\n" +
                          "FORWARD - Move forward\n" +
                          "BACK - Move backward\n\n" +
                          "TARGET ZONES:\n" +
                          "Green Circle = PUSH target\n" +
                          "   Position plate here to add to stack\n\n" +
                          "Red Zone = POP removal\n" +
                          "   Move plate here to remove from stack\n\n" +
                          "FEATURES:\n" +
                          "• Hold buttons for continuous movement\n" +
                          "• Release to stop moving\n" +
                          "• Visual indicators show targets\n\n" +
                          "Click CANCEL to abort operation\n\n" +
                          "TIP:\n" +
                          "Take your time positioning!\n" +
                          "CONFIRM button appears when close enough."
        });

        // Reset tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = RESET,
            title = "RESET Scene",
            textContent = "START FRESH:\n\n" +
                          "Click RESET button to:\n" +
                          "• Clear all plates from stack\n" +
                          "• Reset stack pointer to -1\n" +
                          "• Remove scene from AR space\n" +
                          "• Return to placement mode\n\n" +
                          "TO RESPAWN:\n" +
                          "Point your camera at a flat surface\n" +
                          "(floor or table) to place the stack again!\n\n" +
                          "LEARNING TIP:\n" +
                          "Try these exercises:\n" +
                          "1. PUSH 5 plates, then POP all\n" +
                          "2. PUSH, PEEK, POP pattern\n" +
                          "3. Fill stack to max capacity\n" +
                          "4. Practice LIFO ordering\n\n" +
                          "CHALLENGE:\n" +
                          "Can you predict which plate\n" +
                          "will come out next after\n" +
                          "multiple PUSH operations?"
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

    public void OnPushButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(PUSH);
    }

    public void OnPopButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(POP);
    }

    public void OnPeekButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(PEEK);
    }

    public void OnLIFOExplainButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(LIFO);
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