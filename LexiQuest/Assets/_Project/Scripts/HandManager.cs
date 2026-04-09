using UnityEngine;
using System.Collections.Generic;  // Correct namespace for List

public class HandManager : MonoBehaviour
{
    public List<char> currentHand = new List<char>();
    public string currentWord = "";

    // Call this when a card is clicked
    public void SelectLetter(char letter) {
        if (currentHand.Contains(letter)) {  // Check if letter is available
            currentWord += letter;
            UpdateWordUI();
            // Here you would check your "Grimoire" for repeated word penalties
        }
    }

    private void UpdateWordUI() {
        // Add your UI update logic here
        Debug.Log("Current word: " + currentWord);
    }
}