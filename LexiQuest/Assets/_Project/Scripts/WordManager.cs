using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class WordManager : MonoBehaviour
{
    public static WordManager instance;

    private void Awake()
    {
        // This locks the instance in the moment the game starts
        if (instance == null) instance = this;
    }

    [Header("UI References")]
    public TMP_InputField wordInputField;
    public Transform inputDisplayArea;
    public GameObject spellInputPanel; 
    
    [Header("Card Spawning")]
    public GameObject cardPrefab; 
    public Transform handContainer; 
    public int startingHandSize = 5; 

    // --- NEW: Ink System Variables ---
    [Header("Ink System")]
    public int maxInk = 15; 
    public int currentInk; // Removed the "= 10" because we will set it in Start()
    public TextMeshProUGUI totalInkTextUI;
    // ---------------------------------

    public int cardsStarredThisTurn = 0;
    void Start()
    {
        if (spellInputPanel != null) spellInputPanel.SetActive(false);
        currentInk = 5; 
        UpdateInkUI();

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

            // --- NEW: Check if the player can afford the spell! ---
            int costOfSpell = CardInteraction.currentlyPlayedCard.inkCost;
            if (currentInk < costOfSpell)
            {
                Debug.LogWarning("Not enough Ink to cast this spell!");
                // (Later, we can add a red flash to the Ink UI here!)
                return; // Cancels the submission completely
            }

            // Deduct the ink and update the screen
            currentInk -= costOfSpell;
            UpdateInkUI();
            // ------------------------------------------------------

            Debug.Log("Player cast spell with word: " + submittedWord);

            Destroy(CardInteraction.currentlyPlayedCard.gameObject);
            CardInteraction.currentlyPlayedCard = null;

            /*
            if (handContainer.childCount == 0)
            {
                Debug.Log("Hand is empty! Dealing a fresh set of cards.");
                for (int i = 0; i < startingHandSize; i++)
                {
                    DrawNewCard();
                }
            }
            */

            wordInputField.text = "";
            if (spellInputPanel != null)
            {
                spellInputPanel.SetActive(false); 
            }
        }
    }

    public void DrawNewCard()
    {
        // 1. Create the alphabet pool (Patch 1: X and Z removed!)
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWY"; 
        List<char> availableLetters = new List<char>(alphabet.ToCharArray());

        // 2. Patch 2: Look at the current hand and cross off letters we already have
        foreach (Transform child in handContainer)
        {
            CardInteraction existingCard = child.GetComponent<CardInteraction>();
            // We check if it has a letter assigned so we don't count empty data
            if (existingCard != null && existingCard.currentLetter != '\0')
            {
                availableLetters.Remove(existingCard.currentLetter);
            }
        }

        // Safety fallback: If we somehow run out of unique letters, refill the pool
        if (availableLetters.Count == 0)
        {
            availableLetters = new List<char>(alphabet.ToCharArray());
        }

        // 3. Pick a random unique letter from the remaining options
        char chosenLetter = availableLetters[Random.Range(0, availableLetters.Count)];

        // 4. Spawn the card
        GameObject newCard = Instantiate(cardPrefab, handContainer, false);
        newCard.transform.localScale = Vector3.one;
        
        CardInteraction newCardScript = newCard.GetComponent<CardInteraction>();
        newCardScript.inputDisplayArea = this.inputDisplayArea;
        newCardScript.wordInputField = this.wordInputField;
        newCardScript.spellInputPanel = this.spellInputPanel; 
        
        // 5. Hand the unique letter down to the newly spawned card!
        newCardScript.InitializeCardData(chosenLetter);
    }

    public void EnforceStartingLetter(string currentText)
    {
        if (CardInteraction.currentlyPlayedCard == null) return;
        char requiredLetter = CardInteraction.currentlyPlayedCard.currentLetter;

        if (string.IsNullOrEmpty(currentText))
        {
            wordInputField.text = requiredLetter.ToString();
            wordInputField.MoveTextEnd(false); 
            return;
        }

        if (char.ToUpper(currentText[0]) != char.ToUpper(requiredLetter))
        {
            wordInputField.text = requiredLetter.ToString() + currentText.Substring(1);
        }
    }

    // --- NEW: Helper method to update the text on screen ---
    // --- UPDATED: Made public so the cards can trigger it when starred! ---
    public void UpdateInkUI()
    {
        if (totalInkTextUI != null) totalInkTextUI.text = currentInk.ToString();
    }

    public void EndTurn()
    {
        Debug.Log("Player ended their turn!");

        // 1. Return played card to hand if left there
        if (CardInteraction.currentlyPlayedCard != null)
        {
            CardInteraction.currentlyPlayedCard.ReturnToHand();
        }

        // 2. Destroy unstarred cards and un-star the saved ones
        foreach (Transform child in handContainer)
        {
            CardInteraction card = child.GetComponent<CardInteraction>();
            if (card != null)
            {
                if (!card.isStarred)
                {
                    Destroy(child.gameObject); 
                }
                else
                {
                    card.RemoveStar(); 
                }
            }
        }

        // --- UPDATED: Ink Math with Diminishing Returns ---
        if (currentInk == 0)
        {
            currentInk += 7; 
            Debug.Log("Ink depleted! Bonus Refill: +7 Ink");
        }
        else if (currentInk >= 10) // NEW PATCH: If 10 or more, only give 3!
        {
            currentInk += 3;
            Debug.Log("High Ink! Diminishing returns: +3 Ink");
        }
        else
        {
            currentInk += 5; 
            Debug.Log("Standard Refill: +5 Ink");
        }

        // Cap it at the maximum
        if (currentInk > maxInk) currentInk = maxInk;
        
        UpdateInkUI();
        // --------------------------------------------------
        
        // 4. Smart Hand Refill
        int cardsNeeded = startingHandSize - cardsStarredThisTurn;
        for (int i = 0; i < cardsNeeded; i++)
        {
            DrawNewCard();
        }
        
        // 5. Reset the star limit for the new turn
        cardsStarredThisTurn = 0; 
    }

    // --- NEW: The Reshuffle Logic ---
    public void ReshuffleSelectedCardLetter()
    {
        // 1. Make sure a card is actually sitting in the spelling area
        if (CardInteraction.currentlyPlayedCard != null)
        {
            // 2. Check if they have the required 1 Ink
            if (currentInk >= 1)
            {
                // Pay the cost
                currentInk -= 1;
                UpdateInkUI();

                // 3. Dealer Logic: Get a unique letter
                string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWY"; 
                List<char> availableLetters = new List<char>(alphabet.ToCharArray());

                foreach (Transform child in handContainer)
                {
                    CardInteraction existingCard = child.GetComponent<CardInteraction>();
                    if (existingCard != null && existingCard.currentLetter != '\0')
                    {
                        availableLetters.Remove(existingCard.currentLetter);
                    }
                }
                
                // Make sure we don't accidentally roll the exact same letter it already has!
                availableLetters.Remove(CardInteraction.currentlyPlayedCard.currentLetter);

                if (availableLetters.Count == 0)
                {
                    availableLetters = new List<char>(alphabet.ToCharArray());
                }

                char newLetter = availableLetters[Random.Range(0, availableLetters.Count)];

                // 4. Force the card to update its visual text
                CardInteraction.currentlyPlayedCard.ChangeLetter(newLetter);

                // 5. Instantly update the blinking text box so they can start typing!
                wordInputField.text = newLetter.ToString();
                wordInputField.MoveTextEnd(false); 
                
                Debug.Log("Spent 1 Ink. Reshuffled letter to: " + newLetter);
            }
            else
            {
                Debug.LogWarning("Not enough Ink to reshuffle this letter!");
            }
        }
    }
}