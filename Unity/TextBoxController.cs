using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextBoxController : MonoBehaviour
{
    public InputField inputField;
    public KeyCode enableInputKey = KeyCode.T;

    void Start()
    {
        // Add a listener to the input field's "End Edit" event
        inputField.onEndEdit.AddListener(OnEndEdit);

        // Check if the inputField is assigned
        if (inputField == null)
        {
            Debug.LogError("InputField is not assigned to TextBoxController.");
            return;
        }
    }

    void Update()
    {
        // Check if the specific button is pressed
        if (Input.GetKeyDown(enableInputKey))
        {
            // Enable input on the text box if it's assigned
            if (inputField != null)
            {
                inputField.interactable = true;
                inputField.Select();
                inputField.ActivateInputField();
            }
            else
            {
                Debug.LogError("InputField is not assigned to TextBoxController.");
            }
        }
    }

    // Method called when the user finishes editing the input field
    void OnEndEdit(string text)
    {
        // Check if the pressed key was "Enter"
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // Send the message
            SendMessage();
        }
    }

    // Method to send the message
    void SendMessage()
    {
        string message = inputField.text;

        // Your code to send the message goes here

        // Clear the input field after sending the message
        inputField.text = "";
    }
}