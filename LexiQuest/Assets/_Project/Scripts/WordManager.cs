using UnityEngine;
using TMPro;

public class WordManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField wordInputField;
    public Transform inputDisplayArea;
    public GameObject spellInputPanel; 
    
    [Header("Card Spawning")]
    public GameObject cardPrefab; 
    public Transform handContainer; 
    public int startingHandSize = 5; // NEW: Set how many cards you start with

    void Start()
    {
        // Hide the panel at the very start of the game just in case
        if (spellInputPanel != null)
        {
            spellInputPanel.SetActive(false);
        }

        // Deal the initial hand automatically!
        for (int i = 0; i < startingHandSize; i++)
        {
            DrawNewCard();
        }
    }

    public void SubmitWord()
    {
        if (CardInteraction.currentlyPlayedCard != null)
        {
            string submittedWord = wordInputField.text;
            Debug.Log("Player cast spell with word: " + submittedWord);

            Destroy(CardInteraction.currentlyPlayedCard.gameObject);
            CardInteraction.currentlyPlayedCard = null;

            // Draw a new card using our new helper method
            DrawNewCard();

            wordInputField.text = "";
            if (spellInputPanel != null)
            {
                spellInputPanel.SetActive(false); 
            }
        }
    }

    // A clean helper method to handle all the spawning and mapping
    public void DrawNewCard()
    {
        GameObject newCard = Instantiate(cardPrefab, handContainer, false);
        newCard.transform.localScale = Vector3.one;
        
        CardInteraction newCardScript = newCard.GetComponent<CardInteraction>();
        newCardScript.inputDisplayArea = this.inputDisplayArea;
        newCardScript.wordInputField = this.wordInputField;
        newCardScript.spellInputPanel = this.spellInputPanel; 
    }
}