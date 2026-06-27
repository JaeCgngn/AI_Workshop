using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NpcChat : MonoBehaviour
{
    const string ChatCompletionsUrl = "https://api.groq.com/openai/v1/chat/completions";
    const string InputControlName = "NpcChatInput";

    [SerializeField] string apiKey;
    [SerializeField] string model = "llama-3.3-70b-versatile";
    [SerializeField] string npcDisplayName = "Villager";
    [TextArea(3, 10)]
    [SerializeField] string persona = "You are a friendly NPC in a fantasy village.";
    [SerializeField] InputField playerInput;
    [SerializeField] Text replyText;
    [SerializeField] float chatPanelHeight = 320f;
    [SerializeField] float chatPanelMargin = 20f;
    [SerializeField] int chatFontSize = 25;

    readonly StringBuilder conversationLog = new StringBuilder();
    Vector2 scrollPosition;
    string guiInputText = "";
    bool waitingForReply;
    bool chatOpen;
    GUIStyle logStyle;
    GUIStyle textFieldStyle;
    GUIStyle buttonStyle;

    public bool IsChatOpen => chatOpen;
    public bool IsInputFocused { get; private set; }
    public string DisplayName => string.IsNullOrWhiteSpace(npcDisplayName) ? "Villager" : npcDisplayName;

    public event Action CloseRequested;
    public event Action<string> OnReplyReceived;
    public event Action<string> OnRequestFailed;

    void OnEnable()
    {
        if (playerInput != null)
            playerInput.onSubmit.AddListener(OnPlayerSubmitted);
    }

    void OnDisable()
    {
        if (playerInput != null)
            playerInput.onSubmit.RemoveListener(OnPlayerSubmitted);
    }

    public void SubmitPlayerMessage()
    {
        if (playerInput == null)
            return;

        OnPlayerSubmitted(playerInput.text);
    }

    void OnPlayerSubmitted(string playerText)
    {
        SendMessage(playerText);

        if (playerInput != null)
            playerInput.text = string.Empty;
    }

    public void SendMessage(string userMessage)
    {
        StartCoroutine(SendMessageCoroutine(userMessage));
    }

    public void SetChatOpen(bool open)
    {
        chatOpen = open;

        if (!chatOpen)
        {
            guiInputText = string.Empty;
            IsInputFocused = false;
            GUI.FocusControl(null);
        }
    }

    public void Configure(string displayName, string personaText)
    {
        npcDisplayName = displayName;
        persona = personaText;
    }

    public void ShareSettingsWith(NpcChat other)
    {
        if (other == null || other == this)
            return;

        other.apiKey = apiKey;
        other.model = model;
    }

    void OnGUI()
    {
        if (!chatOpen)
        {
            IsInputFocused = false;
            return;
        }

        EnsureGuiStyles();

        var chatPanelRect = GetChatPanelRect();
        GUILayout.BeginArea(chatPanelRect, GUI.skin.box);

        GUILayout.Label("Chat with " + DisplayName, logStyle);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        GUILayout.Label(
            conversationLog.Length > 0 ? conversationLog.ToString() : "Say hello to " + DisplayName + "...",
            logStyle);
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        GUI.SetNextControlName(InputControlName);
        guiInputText = GUILayout.TextField(
            guiInputText,
            textFieldStyle,
            GUILayout.ExpandWidth(true),
            GUILayout.Height(chatFontSize + 16));

        GUI.enabled = !waitingForReply;
        var sendPressed = GUILayout.Button(
            "Send",
            buttonStyle,
            GUILayout.Width(120),
            GUILayout.Height(chatFontSize + 16));
        GUI.enabled = true;

        if (sendPressed)
            SendFromGui();

        if (Event.current.type == EventType.KeyDown &&
            Event.current.keyCode == KeyCode.Return &&
            GUI.GetNameOfFocusedControl() == InputControlName)
        {
            SendFromGui();
            Event.current.Use();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        IsInputFocused = GUI.GetNameOfFocusedControl() == InputControlName;

        if (Event.current.type == EventType.KeyDown &&
            Event.current.keyCode == KeyCode.E &&
            !IsInputFocused)
        {
            CloseRequested?.Invoke();
            Event.current.Use();
        }
    }

    void EnsureGuiStyles()
    {
        if (logStyle != null)
            return;

        logStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            fontSize = chatFontSize
        };

        textFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = chatFontSize,
            alignment = TextAnchor.MiddleLeft
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = chatFontSize
        };
    }

    Rect GetChatPanelRect()
    {
        var width = Screen.width - chatPanelMargin * 2f;
        var x = chatPanelMargin;
        var y = Screen.height - chatPanelHeight - chatPanelMargin;
        return new Rect(x, y, width, chatPanelHeight);
    }

    void SendFromGui()
    {
        if (waitingForReply || string.IsNullOrWhiteSpace(guiInputText))
            return;

        var text = guiInputText.Trim();
        guiInputText = string.Empty;
        GUI.FocusControl(null);
        SendMessage(text);
    }

    void AppendLog(string line)
    {
        if (conversationLog.Length > 0)
            conversationLog.AppendLine();

        conversationLog.Append(line);
        scrollPosition.y = float.MaxValue;
    }

    IEnumerator SendMessageCoroutine(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ShowError("API key is not set.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            ShowError("User message is empty.");
            yield break;
        }

        waitingForReply = true;
        AppendLog("You: " + userMessage);

        if (replyText != null)
            replyText.text = "Thinking...";

        var requestBody = new ChatCompletionRequest
        {
            model = model,
            messages = new[]
            {
                new ChatMessage { role = "system", content = persona },
                new ChatMessage { role = "user", content = userMessage }
            }
        };

        var json = JsonUtility.ToJson(requestBody);
        var bodyRaw = Encoding.UTF8.GetBytes(json);

        using (var request = new UnityWebRequest(ChatCompletionsUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                ShowError(request.error + ": " + request.downloadHandler.text);
                yield break;
            }

            var response = JsonUtility.FromJson<ChatCompletionResponse>(request.downloadHandler.text);
            if (response?.choices == null || response.choices.Length == 0 ||
                response.choices[0].message == null)
            {
                ShowError("No reply returned from Groq.");
                yield break;
            }

            ShowReply(response.choices[0].message.content);
        }
    }

    void ShowReply(string reply)
    {
        waitingForReply = false;
        AppendLog(DisplayName + ": " + reply);

        if (replyText != null)
            replyText.text = reply;

        OnReplyReceived?.Invoke(reply);
    }

    void ShowError(string error)
    {
        waitingForReply = false;
        AppendLog("Error: " + error);

        if (replyText != null)
            replyText.text = error;

        Debug.LogError(error);
        OnRequestFailed?.Invoke(error);
    }

    [Serializable]
    class ChatCompletionRequest
    {
        public string model;
        public ChatMessage[] messages;
    }

    [Serializable]
    class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    class ChatCompletionResponse
    {
        public ChatChoice[] choices;
    }

    [Serializable]
    class ChatChoice
    {
        public ChatMessage message;
    }
}
