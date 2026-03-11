using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueueTutorialIntegration : MonoBehaviour
{
    [Header("References")]
    public TutorialSystem tutorialSystem;
    public InteractiveCoffeeQueue queueController;

    [Header("Tutorial Step IDs")]
    private const string WELCOME = "queue_welcome";
    private const string ENQUEUE = "queue_enqueue";
    private const string DEQUEUE = "queue_dequeue";
    private const string PEEK = "queue_peek";
    private const string JOYSTICK = "queue_joystick";
    private const string FIFO = "queue_fifo";
    private const string ADVANCE = "queue_advance";
    private const string RESET = "queue_reset";

    private bool hasShownWelcome = false;

    void Start()
    {
        if (tutorialSystem == null)
            tutorialSystem = FindObjectOfType<TutorialSystem>();

        if (queueController == null)
            queueController = FindObjectOfType<InteractiveCoffeeQueue>();

        if (tutorialSystem != null)
        {
            tutorialSystem.SetSceneKey("CoffeeQueue");
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
            title = "Welcome to Queue Data Structure",
            textContent = "This is an interactive QUEUE demonstration!\n\n" +
                          "• ENQUEUE: Join at REAR (back)\n" +
                          "• DEQUEUE: Leave from FRONT\n" +
                          "• PEEK: View who's next (read-only)\n\n" +
                          "QUEUE CHARACTERISTICS:\n" +
                          "- FIFO: First In, First Out\n" +
                          "- Add at rear, remove from front\n" +
                          "- Fair ordering (no cutting!)\n" +
                          "- All operations O(1)\n\n" +
                          "Think of it like a coffee shop line!\n" +
                          "First person in line gets served first!"
        });

        // ENQUEUE tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = ENQUEUE,
            title = "ENQUEUE Operation ",
            textContent = "HOW TO ADD A CUSTOMER:\n\n" +
                          "1. Click 'ENQUEUE' button\n" +
                          "2. New customer appears\n" +
                          "3. Use JOYSTICK to move\n" +
                          "4. Move to REAR (orange marker)\n" +
                          "5. Position in green circle\n" +
                          "6. Click CONFIRM when ready\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT\n" +
                          "Space Complexity: O(1) - Single element\n" +
                          "Operations: Update rear pointer, add item\n\n" +
                          "WHY O(1)?\n" +
                          "Queue maintains REAR pointer!\n" +
                          "Just increment rear and place customer.\n" +
                          "No traversal or shifting needed!\n\n" +
                          "PSEUDOCODE:\n" +
                          "rear = rear + 1\n" +
                          "queue[rear] = new_customer\n" +
                          "size = size + 1\n\n" +
                          "KEY RULE:\n" +
                          "New customers ALWAYS join at BACK!\n" +
                          "No cutting in line allowed!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Print job scheduling\n" +
                          "• Request handling in servers\n" +
                          "• Breadth-first search (BFS)\n" +
                          "• Task queues in operating systems"
        });

        // DEQUEUE tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = DEQUEUE,
            title = "DEQUEUE Operation ",
            textContent = "HOW TO SERVE A CUSTOMER:\n\n" +
                          "1. Click 'DEQUEUE' button\n" +
                          "2. Front customer becomes movable\n" +
                          "3. Use JOYSTICK to move\n" +
                          "4. Move to red EXIT zone\n" +
                          "5. Click CONFIRM to complete\n" +
                          "6. Watch queue ADVANCE!\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT\n" +
                          "Space Complexity: O(1) - No extra space\n" +
                          "Operations: Read front, increment pointer\n\n" +
                          "WHY O(1)?\n" +
                          "Queue tracks FRONT pointer!\n" +
                          "Just return front element and increment.\n" +
                          "Other elements don't physically move!\n\n" +
                          "PSEUDOCODE:\n" +
                          "customer = queue[front]\n" +
                          "front = front + 1\n" +
                          "size = size - 1\n" +
                          "return customer\n\n" +
                          "FIFO RULE:\n" +
                          "ALWAYS serve from FRONT!\n" +
                          "First customer in = First customer out!\n\n" +
                          "AUTO-ADVANCE:\n" +
                          "Everyone moves forward in line!\n" +
                          "Watch the smooth animation!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Customer service lines\n" +
                          "• Message queues (email, chat)\n" +
                          "• CPU scheduling\n" +
                          "• Event handling systems"
        });

        // PEEK tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = PEEK,
            title = "PEEK Operation ",
            textContent = "HOW TO SEE WHO'S NEXT:\n\n" +
                          "1. Click 'PEEK' button\n" +
                          "2. Front customer glows cyan\n" +
                          "3. See their position [0]\n" +
                          "4. Queue stays unchanged!\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT\n" +
                          "Space Complexity: O(1) - Read only\n" +
                          "Operations: Return queue[front]\n\n" +
                          "WHY O(1)?\n" +
                          "Direct access to front pointer!\n" +
                          "Just read the value, no modification.\n" +
                          "No searching or traversal!\n\n" +
                          "PSEUDOCODE:\n" +
                          "if (front <= rear):\n" +
                          "    return queue[front]\n" +
                          "else:\n" +
                          "    return 'Queue Empty'\n\n" +
                          "KEY FEATURE:\n" +
                          "PEEK is NON-DESTRUCTIVE!\n" +
                          "Look without removing!\n" +
                          "Queue remains intact!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Check next task without starting it\n" +
                          "• Preview waiting customers\n" +
                          "• Validate queue state\n" +
                          "• Display 'Now Serving' signs"
        });

        // Joystick tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = JOYSTICK,
            title = "Joystick Controls",
            textContent = "USING THE JOYSTICK:\n\n" +
                          "HOW IT WORKS:\n" +
                          "1. Touch/click the gray handle\n" +
                          "2. DRAG in any direction\n" +
                          "3. Customer moves where you drag!\n" +
                          "4. Customer faces movement direction\n" +
                          "5. RELEASE to stop moving\n\n" +
                          "FEATURES:\n" +
                          "• 360 degree movement freedom\n" +
                          "• Variable speed control\n" +
                          "• Auto-centers when released\n" +
                          "• Smooth, natural control\n\n" +
                          "TIPS:\n" +
                          "• Small movements = precise control\n" +
                          "• Large movements = fast movement\n" +
                          "• Hold near center for slow speed\n" +
                          "• Drag to edge for max speed\n\n" +
                          "Click CANCEL to abort operation\n\n" +
                          "LIKE A REAL JOYSTICK:\n" +
                          "More intuitive than arrow buttons!\n" +
                          "Move naturally in any direction!"
        });

        // FIFO tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = FIFO,
            title = "FIFO Principle Explained",
            textContent = "FIRST IN, FIRST OUT!\n\n" +
                          "THE CORE CONCEPT:\n" +
                          "The FIRST person who joins the line\n" +
                          "is the FIRST person served!\n\n" +
                          "REAL-WORLD ANALOGY:\n" +
                          "Think of a coffee shop line:\n" +
                          "• Alice arrives - joins back of line\n" +
                          "• Bob arrives - joins behind Alice\n" +
                          "• Carol arrives - joins behind Bob\n" +
                          "• Alice gets served FIRST!\n\n" +
                          "OPERATIONS:\n" +
                          "Join at REAR = ENQUEUE\n" +
                          "Serve from FRONT = DEQUEUE\n\n" +
                          "FAIRNESS:\n" +
                          "• No cutting in line!\n" +
                          "• Everyone waits their turn\n" +
                          "• Maintains order of arrival\n" +
                          "• Democratic and fair\n\n" +
                          "VS STACK (LIFO):\n" +
                          "Stack: Last in, First out (dishes)\n" +
                          "Queue: First in, First out (line)\n\n" +
                          "WHY FIFO?\n" +
                          "• Fair resource allocation\n" +
                          "• Prevents starvation\n" +
                          "• Natural for sequential tasks\n" +
                          "• Reflects real-world waiting\n\n" +
                          "WHEN TO USE QUEUES:\n" +
                          "• Task scheduling (fair order)\n" +
                          "• Buffer management (streaming)\n" +
                          "• Level-order tree traversal\n" +
                          "• Request processing"
        });

        // Advance tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = ADVANCE,
            title = "Queue Advancement Explained",
            textContent = "WHAT HAPPENS AFTER DEQUEUE:\n\n" +
                          "AUTOMATIC PROCESS:\n" +
                          "1. Front customer leaves (served)\n" +
                          "2. Everyone steps forward!\n" +
                          "3. Next person becomes [0]\n" +
                          "4. REAR marker updates position\n" +
                          "5. Smooth animation plays\n\n" +
                          "ADVANCEMENT COMPLEXITY:\n" +
                          "Visual: O(n) - All animate forward\n" +
                          "Logical: O(1) - Just pointer update\n\n" +
                          "IMPLEMENTATION NOTE:\n" +
                          "In this demo, customers physically move\n" +
                          "for visual clarity. In real queues:\n" +
                          "• Only pointers move (O(1))\n" +
                          "• Elements don't shift physically\n" +
                          "• Circular buffer optimization\n\n" +
                          "WATCH FOR:\n" +
                          "• Smooth walking animations\n" +
                          "• Position numbers updating [0][1][2]\n" +
                          "• REAR marker moving forward\n" +
                          "• Everyone maintaining order\n\n" +
                          "POSITION UPDATES:\n" +
                          "Was [1] becomes [0]\n" +
                          "Was [2] becomes [1]\n" +
                          "Was [3] becomes [2]\n" +
                          "Everyone moves up one spot!\n\n" +
                          "REAL-WORLD PARALLEL:\n" +
                          "Like a real coffee shop line -\n" +
                          "everyone steps forward when\n" +
                          "the person ahead is served!"
        });

        // Reset tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = RESET,
            title = "RESET Scene",
            textContent = "START FRESH:\n\n" +
                          "Click RESET button to:\n" +
                          "• Clear entire queue\n" +
                          "• Remove all customers\n" +
                          "• Reset front/rear pointers\n" +
                          "• Remove scene from AR space\n\n" +
                          "TO RESPAWN:\n" +
                          "Point your camera at a flat surface\n" +
                          "(floor or table) to place the coffee shop again!\n\n" +
                          "LEARNING EXERCISES:\n" +
                          "Try these to master queues:\n\n" +
                          "1. BASIC FIFO:\n" +
                          "   Enqueue 3 people, then dequeue all.\n" +
                          "   Note the order!\n\n" +
                          "2. MIXED OPERATIONS:\n" +
                          "   Enqueue 2, Dequeue 1, Enqueue 2 more.\n" +
                          "   Who's at front now?\n\n" +
                          "3. PEEK PRACTICE:\n" +
                          "   Fill queue, use PEEK multiple times.\n" +
                          "   Does front change?\n\n" +
                          "4. CAPACITY TEST:\n" +
                          "   Fill to max capacity.\n" +
                          "   Try to enqueue more!\n\n" +
                          "CHALLENGE QUESTION:\n" +
                          "If you enqueue: Alice, Bob, Carol\n" +
                          "Then dequeue once...\n" +
                          "Who's at the front now?"
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

    public void OnEnqueueButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(ENQUEUE);
    }

    public void OnDequeueButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(DEQUEUE);
    }

    public void OnPeekButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(PEEK);
    }

    public void OnJoystickUsed()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(JOYSTICK);
    }

    public void OnFIFOExplainButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(FIFO);
    }

    public void OnQueueAdvanced()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(ADVANCE);
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