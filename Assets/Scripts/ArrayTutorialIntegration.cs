using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrayTutorialIntegration : MonoBehaviour
{
    [Header("References")]
    public TutorialSystem tutorialSystem;
    public InteractiveArrayCars arrayController;

    [Header("Tutorial Step IDs")]
    private const string WELCOME = "array_welcome";
    private const string INSERT = "array_insert";
    private const string REMOVE = "array_remove";
    private const string ACCESS = "array_access";
    private const string MOVEMENT = "array_movement";
    private const string RESET = "array_reset";

    private bool hasShownWelcome = false;

    void Start()
    {
        if (tutorialSystem == null)
            tutorialSystem = FindObjectOfType<TutorialSystem>();

        if (arrayController == null)
            arrayController = FindObjectOfType<InteractiveArrayCars>();

        if (tutorialSystem != null)
        {
            tutorialSystem.SetSceneKey("ArrayCars");
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
            title = "Welcome to Array Parking Lot",
            textContent = "This is an interactive demonstration of the ARRAY data structure!\n\n" +
                          "Arrays store elements in FIXED-SIZE, INDEXED positions\n" +
                          "Each parking spot has an INDEX (0, 1, 2, ...)\n" +
                          "You can INSERT, REMOVE, or ACCESS cars instantly!\n\n" +
                          "ARRAY CHARACTERISTICS:\n" +
                          "- Fixed size (6 spots in this demo)\n" +
                          "- Direct index access\n" +
                          "- Contiguous memory storage\n" +
                          "- Fast random access O(1)\n\n" +
                          "Let's learn how arrays work by parking some cars!"
        });

        // Insert tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = INSERT,
            title = "INSERT Operation ",
            textContent = "HOW TO PARK A CAR:\n\n" +
                          "1. Click 'INSERT' button\n" +
                          "2. Enter the parking spot index (0-5)\n" +
                          "3. Use arrow buttons to drive the car\n" +
                          "4. Park in the green zone\n" +
                          "5. Click CONFIRM when ready\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT TIME\n" +
                          "Space Complexity: O(1) - No extra space\n" +
                          "Operations: Direct index assignment\n\n" +
                          "WHY O(1)?\n" +
                          "Arrays use direct memory addressing!\n" +
                          "array[index] = value is one operation,\n" +
                          "regardless of array size!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Database record insertion at known position\n" +
                          "• Game inventory slots\n" +
                          "• Calendar day selection"
        });

        // Remove tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = REMOVE,
            title = "REMOVE Operation ",
            textContent = "HOW TO REMOVE A CAR:\n\n" +
                          "1. Click 'REMOVE' button\n" +
                          "2. Enter the spot index with a car\n" +
                          "3. Use arrow buttons to drive out\n" +
                          "4. Move to the red EXIT zone\n" +
                          "5. Click CONFIRM to complete\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT TIME\n" +
                          "Space Complexity: O(1) - No extra space\n" +
                          "Operations: Direct index removal\n\n" +
                          "WHY O(1)?\n" +
                          "Direct access to index means instant removal!\n" +
                          "array[index] = null is one operation.\n" +
                          "No shifting or searching required!\n\n" +
                          "NOTE: Spot becomes empty and available!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Removing item from shopping cart by position\n" +
                          "• Clearing calendar event at specific day\n" +
                          "• Deleting player from team roster slot"
        });

        // Access tutorial with algorithm analysis
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = ACCESS,
            title = "ACCESS Operation ",
            textContent = "HOW TO CHECK A PARKING SPOT:\n\n" +
                          "1. Click 'ACCESS' button\n" +
                          "2. Enter the spot index to check\n" +
                          "3. See if it's occupied or empty!\n\n" +
                          "ALGORITHM ANALYSIS:\n" +
                          "Time Complexity: O(1) - CONSTANT TIME\n" +
                          "Space Complexity: O(1) - No extra space\n" +
                          "Operations: Read value at index\n\n" +
                          "WHY O(1)?\n" +
                          "Arrays calculate exact memory address:\n" +
                          "address = base + (index × element_size)\n" +
                          "This is ONE mathematical operation!\n\n" +
                          "KEY ADVANTAGE:\n" +
                          "No need to check other spots first!\n" +
                          "Direct jump to any index instantly!\n\n" +
                          "CODE EXAMPLE:\n" +
                          "value = array[3]  // Instant access!\n\n" +
                          "REAL-WORLD USE:\n" +
                          "• Checking score at game round\n" +
                          "• Reading sensor data at time index\n" +
                          "• Looking up grade for student ID"
        });

        // Movement controls tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = MOVEMENT,
            title = "Movement Controls",
            textContent = "DRIVING THE CARS:\n\n" +
                          "LEFT - Move car left\n" +
                          "RIGHT - Move car right\n" +
                          "FORWARD - Move car forward\n" +
                          "BACK - Move car backward\n" +
                          "UP - Lift car up\n" +
                          "DOWN - Lower car down\n\n" +
                          "FEATURES:\n" +
                          "• Cars can't collide with each other!\n" +
                          "• Stay within parking lot boundaries!\n" +
                          "• Smooth continuous movement!\n\n" +
                          "Click CANCEL to abort operation\n\n" +
                          "TIP: Hold buttons for continuous movement!"
        });

        // Reset tutorial
        tutorialSystem.tutorialSteps.Add(new TutorialSystem.TutorialStep
        {
            stepID = RESET,
            title = "RESET Scene",
            textContent = "START FRESH:\n\n" +
                          "Click the RESET button to:\n" +
                          "• Clear all parked cars\n" +
                          "• Reset the parking lot\n" +
                          "• Place a new scene\n\n" +
                          "TO RESPAWN:\n" +
                          "Point your camera at a flat surface\n" +
                          "(floor or table) to spawn the parking lot again!\n\n" +
                          "USE CASES:\n" +
                          "• Practice different operations\n" +
                          "• Try different parking patterns\n" +
                          "• Start learning from scratch"
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

    public void OnInsertButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(INSERT);
    }

    public void OnRemoveButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(REMOVE);
    }

    public void OnAccessButtonClicked()
    {
        if (tutorialSystem != null)
            tutorialSystem.ShowTutorial(ACCESS);
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

    public void ShowFirstTimeSequence()
    {
        if (tutorialSystem != null)
        {
            List<string> sequence = new List<string>
            {
                WELCOME,
                INSERT,
                MOVEMENT
            };
            tutorialSystem.ShowTutorialSequence(sequence);
        }
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