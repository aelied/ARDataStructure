using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using System.Text.RegularExpressions;

public class CodeLabPanel : MonoBehaviour
{
    [Header("Root Panel")]
    public GameObject codePanelRoot;

    [Header("Header Bar")]
    public Button backButton;
    public TextMeshProUGUI panelTitleText;
    public TextMeshProUGUI challengeSubtitleText;

    [Header("Language Toggle")]
    public Button pythonLanguageButton;
    public Button javaLanguageButton;
    public Image pythonButtonBackground;
    public Image javaButtonBackground;
    public TextMeshProUGUI pythonButtonText;
    public TextMeshProUGUI javaButtonText;

    [Header("Problem Statement")]
    public TextMeshProUGUI problemStatementText;

    [Header("Template Dropdown")]
    public Button templateDropdownButton;
    public TextMeshProUGUI templateDropdownLabel;
    public GameObject templateDropdownPanel;
    public Button[] templateButtons;
    public TextMeshProUGUI[] templateButtonLabels;

    [Header("Code Editor")]
    public TMP_InputField codeInputField;
    public TextMeshProUGUI lineNumbersText;
    public ScrollRect codeScrollRect;

    [Header("Action Buttons")]
    public Button runButton;
    public Button submitButton;
    public TextMeshProUGUI runButtonText;
    public TextMeshProUGUI submitButtonText;

    [Header("Output / Result Panel")]
    public GameObject resultPanel;
    public TextMeshProUGUI consoleOutputText;
    public TextMeshProUGUI analysisText;
    public GameObject passResultPanel;
    public GameObject failResultPanel;
    public TextMeshProUGUI resultStatusText;

    [Header("After-Submit Buttons")]
    public Button lessonButton;
    public Button tryARButton;
    public Button nextButton;

    [Header("Loading Indicator")]
    public GameObject loadingSpinner;
    public TextMeshProUGUI loadingStatusText;

    [Header("Colors")]
    public Color activeLanguageColor   = new Color(0.29f, 0.49f, 1f);
    public Color inactiveLanguageColor = new Color(0.29f, 0.32f, 0.41f);
    public Color passColor             = new Color(0.18f, 0.80f, 0.44f);
    public Color failColor             = new Color(0.91f, 0.30f, 0.24f);

    [Header("Navigation")]
    public TopicPanelBridge topicPanelBridge;
    public BottomNavigation bottomNavigation;
    public UpdatedLearnPanelController learnPanelController;

    [Header("Bottom Nav (keep visible above overlay)")]
    public GameObject bottomNavRoot;

    private const int OverlaySortOrder   = 100;
    private const int BottomNavSortOrder = 101;

    [Header("Groq API Settings")]
    // Get your FREE key at: https://console.groq.com/keys
    public string groqApiKey = "YOUR_GROQ_API_KEY";

    // Free model — fast Llama 3 70B via Groq
    private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string GroqModel  = "llama-3.3-70b-versatile";

    // ── Template data ──────────────────────────────────────────────────

    private enum Language { Python, Java }
    private Language currentLanguage      = Language.Python;
    private bool     isDropdownOpen       = false;
    private int      currentTemplateIndex = 0;

    private static readonly string[] PythonTemplateNames = { "Hello World", "Fibonacci", "Linked List", "Stack", "Queue" };
    private static readonly string[] JavaTemplateNames   = { "Hello World", "Fibonacci", "Linked List", "Stack", "Queue" };

    private static readonly string[] PythonTemplates =
    {
        "# Hello World\nprint(\"Hello, World!\")",
        "def fibonacci(n):\n    if n <= 1:\n        return n\n    return fibonacci(n-1) + fibonacci(n-2)\n\nfor i in range(10):\n    print(fibonacci(i))",
        "class Node:\n    def __init__(self, data):\n        self.data = data\n        self.next = None\n\nclass LinkedList:\n    def __init__(self):\n        self.head = None\n\n    def append(self, data):\n        new_node = Node(data)\n        if not self.head:\n            self.head = new_node\n            return\n        current = self.head\n        while current.next:\n            current = current.next\n        current.next = new_node\n\n    def display(self):\n        elements = []\n        current = self.head\n        while current:\n            elements.append(str(current.data))\n            current = current.next\n        print(\" -> \".join(elements))\n\nll = LinkedList()\nll.append(1)\nll.append(2)\nll.append(3)\nll.display()",
        "class Stack:\n    def __init__(self):\n        self.items = []\n\n    def push(self, item):\n        self.items.append(item)\n\n    def pop(self):\n        return self.items.pop() if self.items else None\n\n    def peek(self):\n        return self.items[-1] if self.items else None\n\ns = Stack()\ns.push(10)\ns.push(20)\nprint(s.pop())\nprint(s.peek())",
        "from collections import deque\n\nclass Queue:\n    def __init__(self):\n        self.items = deque()\n\n    def enqueue(self, item):\n        self.items.append(item)\n\n    def dequeue(self):\n        return self.items.popleft() if self.items else None\n\nq = Queue()\nq.enqueue(\"first\")\nq.enqueue(\"second\")\nprint(q.dequeue())\nprint(q.dequeue())"
    };

    private static readonly string[] JavaTemplates =
    {
        "public class Main {\n    public static void main(String[] args) {\n        System.out.println(\"Hello, World!\");\n    }\n}",
        "public class Main {\n    static int fibonacci(int n) {\n        if (n <= 1) return n;\n        return fibonacci(n-1) + fibonacci(n-2);\n    }\n    public static void main(String[] args) {\n        for (int i = 0; i < 10; i++) System.out.print(fibonacci(i) + \" \");\n    }\n}",
        "public class Main {\n    static class Node { int data; Node next; Node(int d){data=d;} }\n    static class LinkedList {\n        Node head;\n        void append(int d){ Node n=new Node(d); if(head==null){head=n;return;} Node c=head; while(c.next!=null)c=c.next; c.next=n; }\n        void display(){ Node c=head; while(c!=null){System.out.print(c.data); if(c.next!=null)System.out.print(\" -> \"); c=c.next;} System.out.println(); }\n    }\n    public static void main(String[] args){ LinkedList ll=new LinkedList(); ll.append(1); ll.append(2); ll.append(3); ll.display(); }\n}",
        "import java.util.Stack;\npublic class Main {\n    public static void main(String[] args) {\n        Stack<Integer> stack = new Stack<>();\n        stack.push(10); stack.push(20); stack.push(30);\n        System.out.println(stack.pop());\n        System.out.println(stack.peek());\n    }\n}",
        "import java.util.LinkedList;\nimport java.util.Queue;\npublic class Main {\n    public static void main(String[] args) {\n        Queue<String> queue = new LinkedList<>();\n        queue.offer(\"first\"); queue.offer(\"second\");\n        System.out.println(queue.poll());\n        System.out.println(queue.poll());\n    }\n}"
    };

    private static readonly string[] ProblemStatements =
    {
        "Write a program that outputs a greeting message to the console.",
        "Implement the Fibonacci sequence and print the first 10 numbers.",
        "Create a Linked List class with append and display methods.",
        "Implement a Stack with push, pop, and peek operations.",
        "Implement a Queue with enqueue and dequeue operations."
    };

    // ── State ──────────────────────────────────────────────────────────

    private bool   isRunning    = false;
    private Canvas overlayCanvas;

    // ── Unity Lifecycle ────────────────────────────────────────────────

    void Start()
    {
        if (templateButtons      == null) templateButtons      = new Button[0];
        if (templateButtonLabels == null) templateButtonLabels = new TextMeshProUGUI[0];

        if (topicPanelBridge     == null) topicPanelBridge     = FindObjectOfType<TopicPanelBridge>(true);
        if (bottomNavigation     == null) bottomNavigation     = FindObjectOfType<BottomNavigation>(true);
        if (learnPanelController == null) learnPanelController = FindObjectOfType<UpdatedLearnPanelController>(true);
        if (bottomNavRoot        == null && bottomNavigation != null) bottomNavRoot = bottomNavigation.gameObject;

        EnsureOverlayCanvas();
        EnsureBottomNavOnTop();
        WireButtons();
        SetLanguage(Language.Python);
        SetTemplate(0);
        HideResultPanel();
        CloseTemplateDropdown();

        if (loadingSpinner != null) loadingSpinner.SetActive(false);
        if (codePanelRoot  != null) codePanelRoot.SetActive(false);
    }

    void OnEnable()
    {
        HideResultPanel();
        if (codeInputField != null) codeInputField.onValueChanged.AddListener(OnCodeChanged);
    }

    void OnDisable()
    {
        if (codeInputField != null) codeInputField.onValueChanged.RemoveListener(OnCodeChanged);
    }

    // ── Canvas setup ───────────────────────────────────────────────────

    void EnsureOverlayCanvas()
    {
        if (codePanelRoot == null) return;
        overlayCanvas = codePanelRoot.GetComponent<Canvas>() ?? codePanelRoot.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder    = OverlaySortOrder;
        if (codePanelRoot.GetComponent<GraphicRaycaster>() == null)
            codePanelRoot.AddComponent<GraphicRaycaster>();
    }

    void EnsureBottomNavOnTop()
    {
        if (bottomNavRoot == null) return;
        Canvas navCanvas = bottomNavRoot.GetComponent<Canvas>() ?? bottomNavRoot.AddComponent<Canvas>();
        navCanvas.overrideSorting = true;
        navCanvas.sortingOrder    = BottomNavSortOrder;
        if (bottomNavRoot.GetComponent<GraphicRaycaster>() == null)
            bottomNavRoot.AddComponent<GraphicRaycaster>();
        bottomNavRoot.SetActive(true);
        Debug.Log("[CodeLab] Bottom nav sortingOrder set to " + BottomNavSortOrder);
    }

    // ── Button wiring ──────────────────────────────────────────────────

    void WireButtons()
    {
        if (backButton             != null) backButton.onClick.AddListener(OnBackPressed);
        if (pythonLanguageButton   != null) pythonLanguageButton.onClick.AddListener(() => SetLanguage(Language.Python));
        if (javaLanguageButton     != null) javaLanguageButton.onClick.AddListener(() => SetLanguage(Language.Java));
        if (templateDropdownButton != null) templateDropdownButton.onClick.AddListener(ToggleDropdown);
        if (runButton              != null) runButton.onClick.AddListener(OnRunPressed);
        if (submitButton           != null) submitButton.onClick.AddListener(OnSubmitPressed);
        if (lessonButton           != null) lessonButton.onClick.AddListener(OnLessonPressed);
        if (tryARButton            != null) tryARButton.onClick.AddListener(OnTryARPressed);
        if (nextButton             != null) nextButton.onClick.AddListener(OnNextPressed);
        for (int i = 0; i < templateButtons.Length; i++)
        {
            int idx = i;
            if (templateButtons[i] != null) templateButtons[i].onClick.AddListener(() => SetTemplate(idx));
        }
    }

    // ── Language / Template ────────────────────────────────────────────

    void SetLanguage(Language lang)
    {
        currentLanguage = lang;
        bool py = (lang == Language.Python);
        if (pythonButtonBackground != null) pythonButtonBackground.color = py  ? activeLanguageColor : inactiveLanguageColor;
        if (javaButtonBackground   != null) javaButtonBackground.color   = !py ? activeLanguageColor : inactiveLanguageColor;
        if (pythonButtonText != null) pythonButtonText.color = Color.white;
        if (javaButtonText   != null) javaButtonText.color   = Color.white;
        UpdateTemplateButtonLabels();
        SetTemplate(currentTemplateIndex);
        HideResultPanel();
    }

    void UpdateTemplateButtonLabels()
    {
        string[] names = (currentLanguage == Language.Python) ? PythonTemplateNames : JavaTemplateNames;
        for (int i = 0; i < templateButtonLabels.Length; i++)
            if (templateButtonLabels[i] != null)
                templateButtonLabels[i].text = (i < names.Length) ? names[i] : "";
    }

    void SetTemplate(int index)
    {
        string[] templates = (currentLanguage == Language.Python) ? PythonTemplates : JavaTemplates;
        currentTemplateIndex = Mathf.Clamp(index, 0, templates.Length - 1);
        if (codeInputField != null) { codeInputField.text = templates[currentTemplateIndex]; UpdateLineNumbers(templates[currentTemplateIndex]); }
        string[] names = (currentLanguage == Language.Python) ? PythonTemplateNames : JavaTemplateNames;
        if (templateDropdownLabel != null && currentTemplateIndex < names.Length) templateDropdownLabel.text = names[currentTemplateIndex];
        if (problemStatementText  != null && currentTemplateIndex < ProblemStatements.Length) problemStatementText.text = ProblemStatements[currentTemplateIndex];
        CloseTemplateDropdown();
        HideResultPanel();
    }

    void ToggleDropdown() { isDropdownOpen = !isDropdownOpen; if (templateDropdownPanel != null) templateDropdownPanel.SetActive(isDropdownOpen); }
    void CloseTemplateDropdown() { isDropdownOpen = false; if (templateDropdownPanel != null) templateDropdownPanel.SetActive(false); }
    void OnCodeChanged(string code) => UpdateLineNumbers(code);

    void UpdateLineNumbers(string code)
    {
        if (lineNumbersText == null) return;
        string[] lines = code.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        StringBuilder sb = new StringBuilder();
        for (int i = 1; i <= lines.Length; i++) sb.AppendLine(i.ToString());
        lineNumbersText.text = sb.ToString();
    }

    // ── Run / Submit ───────────────────────────────────────────────────

    void OnRunPressed()    { if (!isRunning) StartCoroutine(RunCode(false)); }
    void OnSubmitPressed() { if (!isRunning) StartCoroutine(RunCode(true));  }

    IEnumerator RunCode(bool submit)
    {
        isRunning = true;
        HideResultPanel();
        SetRunButtonsInteractable(false);
        if (loadingSpinner    != null) loadingSpinner.SetActive(true);
        if (loadingStatusText != null) loadingStatusText.text = submit ? "Evaluating with Groq..." : "Running with Groq...";

        string code = (codeInputField != null) ? codeInputField.text : "";
        string lang = (currentLanguage == Language.Python) ? "python" : "java";

        yield return StartCoroutine(CallGroqAPI(code, lang, submit));

        if (loadingSpinner != null) loadingSpinner.SetActive(false);
        SetRunButtonsInteractable(true);
        isRunning = false;
    }

    // ── Groq API (OpenAI-compatible) ───────────────────────────────────

    IEnumerator CallGroqAPI(string code, string lang, bool submit)
    {
        string systemPrompt =
            "You are a strict code execution engine. " +
            "Execute the given code EXACTLY as written — do NOT fix, correct, or assume what the developer meant. " +
            "SYNTAX ERRORS: If the code has any syntax error (missing parenthesis, bracket, colon, quote, etc.) " +
            "you MUST return stdout as empty string, stderr as the exact error (e.g. SyntaxError: invalid syntax), exitCode as 1, passed as false. " +
            "RUNTIME ERRORS: If the code throws any runtime exception, return stdout as empty, stderr as the error message, exitCode as 1, passed as false. " +
            "Never silently fix broken code. A missing parenthesis is always a fatal SyntaxError. " +
            "Respond with ONLY a raw JSON object — no markdown, no code fences, no explanation. " +
            "Required fields: stdout (string), stderr (string), exitCode (number), passed (boolean), analysis (string). " +
            "Syntax error example: {\"stdout\":\"\",\"stderr\":\"SyntaxError: invalid syntax (line 1)\",\"exitCode\":1,\"passed\":false,\"analysis\":\"Missing closing parenthesis on line 1.\"}. " +
            "Success example: {\"stdout\":\"Hello, World!\\n\",\"stderr\":\"\",\"exitCode\":0,\"passed\":true,\"analysis\":\"Prints a greeting.\"}";

        string userPrompt = submit
            ? "Evaluate and execute this " + lang + " code:\n" + code
            : "Execute this "              + lang + " code:\n" + code;

        string escapedSystem = EscapeJson(systemPrompt);
        string escapedPrompt = EscapeJson(userPrompt);
        string escapedModel  = EscapeJson(GroqModel);

        // Groq uses the OpenAI chat completions format
        string jsonBody =
            "{" +
              "\"model\":\"" + escapedModel + "\"," +
              "\"max_tokens\":1024," +
              "\"temperature\":0.1," +
              "\"messages\":[" +
                "{\"role\":\"system\",\"content\":\"" + escapedSystem + "\"}," +
                "{\"role\":\"user\",\"content\":\"" + escapedPrompt + "\"}" +
              "]" +
            "}";

        Debug.Log("[CodeLab] Sending Groq request, body length: " + jsonBody.Length);

        byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(GroqApiUrl, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type",  "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + groqApiKey);
            req.timeout = 30;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[CodeLab] Groq response received, length: " + req.downloadHandler.text.Length);
                ParseGroqAndDisplay(req.downloadHandler.text, submit);
            }
            else
            {
                string respBody = req.downloadHandler != null ? req.downloadHandler.text : "no body";
                Debug.LogError("[CodeLab] Groq API error " + req.responseCode + ": " + req.error + " | " + respBody);

                string errMsg = req.responseCode == 401 ? "Invalid Groq API key — check the Inspector."
                              : req.responseCode == 429 ? "Groq rate limit hit — wait a moment and try again."
                              : "API error " + req.responseCode + ": " + req.error;

                ShowResult(errMsg, false, "", submit);
            }
        }
    }

    void ParseGroqAndDisplay(string rawJson, bool submit)
    {
        try
        {
            // Groq/OpenAI format: choices[0].message.content
            GroqResponse groqResp = JsonUtility.FromJson<GroqResponse>(rawJson);
            if (groqResp == null
                || groqResp.choices == null
                || groqResp.choices.Count == 0
                || groqResp.choices[0].message == null)
            {
                ShowResult("Empty response from Groq.", false, "", submit);
                return;
            }

            string assistantText = groqResp.choices[0].message.content ?? "";
            // Strip any accidental markdown fences
            assistantText = Regex.Replace(assistantText, @"```(?:json)?|```", "").Trim();

            CodeEvalResult eval = JsonUtility.FromJson<CodeEvalResult>(assistantText);
            if (eval == null)
            {
                ShowResult(assistantText, false, "", submit);
                return;
            }

            string stdout = (eval.stdout ?? "").Replace("\\n", "\n");
            string stderr = (eval.stderr ?? "").Replace("\\n", "\n");
            string output = !string.IsNullOrEmpty(stdout) ? stdout
                          : !string.IsNullOrEmpty(stderr) ? "Error:\n" + stderr
                          : "No output";

            ShowResult(output, eval.passed, eval.analysis ?? "", submit);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[CodeLab] Parse error: " + e.Message + "\nRaw: " + rawJson);
            ShowResult("Could not parse response. Check Console.", false, "", submit);
        }
    }

    // ── Result display ─────────────────────────────────────────────────

    void ShowResult(string output, bool passed, string analysis, bool showButtons)
    {
        if (resultPanel       != null) resultPanel.SetActive(true);
        if (consoleOutputText != null) consoleOutputText.text = string.IsNullOrEmpty(output) ? "No output" : output;
        if (analysisText      != null) analysisText.text = analysis ?? "";
        if (passResultPanel   != null) passResultPanel.SetActive(passed);
        if (failResultPanel   != null) failResultPanel.SetActive(!passed);
        if (resultStatusText  != null) { resultStatusText.text = passed ? "Pass" : "Fail"; resultStatusText.color = passed ? passColor : failColor; }
        if (lessonButton      != null) lessonButton.gameObject.SetActive(showButtons);
        if (tryARButton       != null) tryARButton.gameObject.SetActive(showButtons);
        if (nextButton        != null) nextButton.gameObject.SetActive(showButtons);
    }

    public void HideResultPanel()
    {
        if (resultPanel       != null) resultPanel.SetActive(false);
        if (consoleOutputText != null) consoleOutputText.text = "";
        if (analysisText      != null) analysisText.text = "";
        if (lessonButton      != null) lessonButton.gameObject.SetActive(false);
        if (tryARButton       != null) tryARButton.gameObject.SetActive(false);
        if (nextButton        != null) nextButton.gameObject.SetActive(false);
    }

    void SetRunButtonsInteractable(bool on)
    {
        if (runButton        != null) runButton.interactable        = on;
        if (submitButton     != null) submitButton.interactable     = on;
        if (runButtonText    != null) runButtonText.color    = on ? Color.white : inactiveLanguageColor;
        if (submitButtonText != null) submitButtonText.color = on ? Color.white : inactiveLanguageColor;
    }

    // ── Navigation ─────────────────────────────────────────────────────

    void OnBackPressed()
    {
        HideOverlay();
        if (learnPanelController != null) learnPanelController.CloseCodeLab();
        TopicPanelBridge bridge = topicPanelBridge ?? FindObjectOfType<TopicPanelBridge>(true);
        if (bridge != null) bridge.ReturnToLessonsList();
        Debug.Log("[CodeLab] Back pressed");
    }

    void OnLessonPressed() => OnBackPressed();

    void OnTryARPressed()
    {
        HideOverlay();
        if (learnPanelController != null) learnPanelController.CloseCodeLab();
        BottomNavigation nav = bottomNavigation ?? FindObjectOfType<BottomNavigation>(true);
        if (nav != null) nav.SwitchToARTab();
    }

    void OnNextPressed()
    {
        int next = currentTemplateIndex + 1;
        string[] templates = (currentLanguage == Language.Python) ? PythonTemplates : JavaTemplates;
        if (next < templates.Length) SetTemplate(next);
        else OnBackPressed();
    }

    // ── Overlay helpers ────────────────────────────────────────────────

    void ShowOverlay()
    {
        if (codePanelRoot == null) return;
        codePanelRoot.SetActive(true);
        if (overlayCanvas != null) { overlayCanvas.overrideSorting = true; overlayCanvas.sortingOrder = OverlaySortOrder; }
        EnsureBottomNavOnTop();
    }

    void HideOverlay() { if (codePanelRoot != null) codePanelRoot.SetActive(false); }

    // ── Utility ────────────────────────────────────────────────────────

    static string EscapeJson(string s)
    {
        if (s == null) return "";
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    // ── Public API ─────────────────────────────────────────────────────

    public void OpenChallenge(string topicName, int templateIndex = 0)
    {
        if (learnPanelController != null) learnPanelController.OpenCodeLab();
        SetLanguage(Language.Python);
        SetTemplate(Mathf.Clamp(templateIndex, 0, PythonTemplates.Length - 1));
        if (panelTitleText        != null) panelTitleText.text        = "Code Lab";
        if (challengeSubtitleText != null) challengeSubtitleText.text = topicName + " Challenge";
        HideResultPanel();
        ShowOverlay();
        Debug.Log("[CodeLab] Opened for topic: " + topicName);
    }

    public void ForceClose()
    {
        HideOverlay();
        if (learnPanelController != null) learnPanelController.CloseCodeLab();
    }
}

// =============================================================================
//  Serialisable types
// =============================================================================

// ── Groq / OpenAI response envelope ───────────────────────────────────────

[System.Serializable]
public class GroqResponse
{
    public List<GroqChoice> choices;
    public string id;
}

[System.Serializable]
public class GroqChoice
{
    public GroqMessage message;
    public string finish_reason;
}

[System.Serializable]
public class GroqMessage
{
    public string role;
    public string content;
}

// ── Code evaluation result ─────────────────────────────────────────────────

[System.Serializable]
public class CodeEvalResult
{
    public string stdout;
    public string stderr;
    public int    exitCode;
    public bool   passed;
    public string analysis;
}