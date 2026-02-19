using UnityEngine;
using TMPro;

public class WordManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField wordInputField;
    public Transform inputDisplayArea;
    
    [Header("Card Spawning")]
    public GameObject cardPrefab; // Your blue Letter_Card prefab
    public Transform handContainer; // The layout group at the bottom

    // We will link this to your Submit Button!
    public void SubmitWord()
    {
        // Make sure a card is actually played before submitting
        if (CardInteraction.currentlyPlayedCard != null)
        {
            // Grab the word you typed (We will add the dictionary validation here later!)
            string submittedWord = wordInputField.text;
            Debug.Log("Player cast spell with word: " + submittedWord);

            // 1. Destroy the played card
            Destroy(CardInteraction.currentlyPlayedCard.gameObject);
            
            // 2. Clear the slot so it's empty
            CardInteraction.currentlyPlayedCard = null;

            // 3. Deal a brand new card into the player's hand
            GameObject newCard = Instantiate(cardPrefab, handContainer);
            
            // 4. Give the new card its map! (So it knows where the spelling area is)
            CardInteraction newCardScript = newCard.GetComponent<CardInteraction>();
            newCardScript.inputDisplayArea = this.inputDisplayArea;
            newCardScript.wordInputField = this.wordInputField;

            // 5. Hide and clear the input field for the next turn
            wordInputField.text = "";
            wordInputField.gameObject.SetActive(false);
        }
    }
}