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
    public int startingHandSize = 5; 

    void Start()
    {
        if (spellInputPanel != null)
        {
            spellInputPanel.SetActive(false);
        }

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

            if (submittedWord.Length <= 1)
            {
                Debug.LogWarning("Word is too short! You must type something.");
                return; 
            }

            Debug.Log("Player cast spell with word: " + submittedWord);

            // 1. Destroy the played card
            Destroy(CardInteraction.currentlyPlayedCard.gameObject);
            CardInteraction.currentlyPlayedCard = null;

            // --- NEW: The "Empty Hand" Check ---
            // Since the played card was moved to the Input Display, 
            // the Hand Container's child count is perfectly accurate!
            if (handContainer.childCount == 0)
            {
                Debug.Log("Hand is empty! Dealing a fresh set of cards.");
                for (int i = 0; i < startingHandSize; i++)
                {
                    DrawNewCard();
                }
            }
            // -----------------------------------

            // 3. Reset the UI for the next turn
            wordInputField.text = "";
            if (spellInputPanel != null)
            {
                spellInputPanel.SetActive(false); 
            }
        }
    }

    public void DrawNewCard()
    {
        GameObject newCard = Instantiate(cardPrefab, handContainer, false);
        newCard.transform.localScale = Vector3.one;
        
        CardInteraction newCardScript = newCard.GetComponent<CardInteraction>();
        newCardScript.inputDisplayArea = this.inputDisplayArea;
        newCardScript.wordInputField = this.wordInputField;
        newCardScript.spellInputPanel = this.spellInputPanel; 
    }

    // --- NEW: The strict keystroke watcher ---
    public void EnforceStartingLetter(string currentText)
    {
        // Don't do anything if no card is played yet
        if (CardInteraction.currentlyPlayedCard == null) return;

        char requiredLetter = CardInteraction.currentlyPlayedCard.currentLetter;

        // 1. Did the player delete the whole box? Force the letter back!
        if (string.IsNullOrEmpty(currentText))
        {
            wordInputField.text = requiredLetter.ToString();
            wordInputField.MoveTextEnd(false); // Push the blinking cursor to the right
            return;
        }

        // 2. Did they somehow change the first letter? (e.g. pasted a word)
        // We compare them as uppercase so 'a' and 'A' are treated the same
        if (char.ToUpper(currentText[0]) != char.ToUpper(requiredLetter))
        {
            // Keep whatever else they typed, but force the correct letter to the front
            wordInputField.text = requiredLetter.ToString() + currentText.Substring(1);
        }
    }
}