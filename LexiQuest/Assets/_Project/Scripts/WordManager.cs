using UnityEngine;
using TMPro;

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
        GameObject newCard = Instantiate(cardPrefab, handContainer, false);
        newCard.transform.localScale = Vector3.one;
        
        CardInteraction newCardScript = newCard.GetComponent<CardInteraction>();
        newCardScript.inputDisplayArea = this.inputDisplayArea;
        newCardScript.wordInputField = this.wordInputField;
        newCardScript.spellInputPanel = this.spellInputPanel; 
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

        if (CardInteraction.currentlyPlayedCard != null)
        {
            CardInteraction.currentlyPlayedCard.ReturnToHand();
        }

        // --- NEW: Destroy unstarred cards and keep the starred ones! ---
        foreach (Transform child in handContainer)
        {
            CardInteraction card = child.GetComponent<CardInteraction>();
            if (card != null)
            {
                if (!card.isStarred)
                {
                    Destroy(child.gameObject); // Trash it!
                }
                else
                {
                    card.RemoveStar(); // Remove the star visual for the next round
                }
            }
        }
        // ---------------------------------------------------------------

        // Ink Math (Patch 1, 2, & 3)
        if (currentInk == 0)
        {
            currentInk += 7; 
        }
        else
        {
            currentInk += 5; 
        }

        if (currentInk > maxInk) currentInk = maxInk;
        UpdateInkUI();
        
        // --- UPDATED: Smart Hand Refill using the starred cards count ---
        // We use cardsStarredThisTurn instead of childCount because Destroy() 
        // doesn't update childCount until the very end of the frame!
        int cardsNeeded = startingHandSize - cardsStarredThisTurn;
        for (int i = 0; i < cardsNeeded; i++)
        {
            DrawNewCard();
        }
        
        // Reset the star limit for the new turn
        cardsStarredThisTurn = 0; 
        // ----------------------------------------------------------------
    }
}