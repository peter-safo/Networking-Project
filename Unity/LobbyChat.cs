using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyChat : MonoBehaviour
{
    public InputField messageInputField;
    public Text chatTextArea;
    //public TextBoxController textBoxController;


    void Start()
    {
        // Check if the messageInputField is assigned
        if (messageInputField == null)
        {
            Debug.LogError("InputField is not assigned to LobbyChat.");
            return;
        }

        messageInputField.onEndEdit.AddListener(OnEndEdit);
    }

    void Update()
    {
        // Check if the specific button is pressed
        if (Input.GetKeyDown(KeyCode.T))
        {
            // Enable input on the text box if it's assigned
            if (messageInputField != null)
            {
                messageInputField.interactable = true;
                messageInputField.Select();
                messageInputField.ActivateInputField();
            }
            else
            {
                Debug.LogError("InputField is not assigned to TextBoxController.");
            }
        }
    }

    void OnEndEdit(string text)
    {
        // Check if the pressed key was "Enter"
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // Send the message
            SendMessage();
        }
    }

    public void SendMessage()
    {
        // Check if the messageInputField is assigned
        if (messageInputField == null)
        {
            Debug.LogError("InputField is not assigned to LobbyChat.");
            return;
        }

        string message = messageInputField.text;

        // Check if NetworkManager instance exists
        if (NetworkManager.Instance != null)
        {
            // Send the message using the NetworkManager
            NetworkManager.Instance.SendChatMessage(message);

            // Display the sent message in the chat area
            chatTextArea.text += $"You: {message}\n";

            // Clear the input field after sending the message
            messageInputField.text = "";
        }
        else
        {
            Debug.LogError("NetworkManager instance is null.");
        }
    }

    public void ReceiveMessage(string sender, string message)
    {
        // Display the received message in the chat area
        chatTextArea.text += $"{sender}: {message}\n";
    }
}
