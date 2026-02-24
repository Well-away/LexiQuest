using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class WordManager : MonoBehaviour
{
    public static WordManager instance;

    private void Awake()
    {
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

    [Header("Ink System")]
    public int maxInk = 15; 
    public int currentInk; 
    public TextMeshProUGUI totalInkTextUI;

    public int cardsStarredThisTurn = 0;
    
    [Header("Battle Targets")]
    public player playerTarget; 
    public Enemy enemyTarget;

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

            // --- UPDATED: Reject words with 3 or fewer letters ---
            if (submittedWord.Length <= 3)
            {
                Debug.LogWarning("Word is too short! Spells require at least 4 letters.");
                return; 
            }

            int costOfSpell = CardInteraction.currentlyPlayedCard.inkCost;
            if (currentInk < costOfSpell)
            {
                Debug.LogWarning("Not enough Ink to cast this spell!");
                return; 
            }

            // --- COMBAT MATH ---
            string spellName = CardInteraction.currentlyPlayedCard.currentSpell;
            int basePower = costOfSpell * 5; 
            float multiplier = 1.0f;

            // --- UPDATED: New Word Length Multipliers ---
            if (submittedWord.Length == 4) multiplier = 1.0f;
            else if (submittedWord.Length == 5) multiplier = 1.5f;
            else if (submittedWord.Length == 6) multiplier = 2.0f;
            else if (submittedWord.Length >= 7) multiplier = 2.5f;

            int finalPower = Mathf.RoundToInt(basePower * multiplier);

            Debug.Log($"<color=cyan>CASTING:</color> {spellName} via '{submittedWord}'. Base: {basePower} * Mult: {multiplier}x = <color=yellow>{finalPower} Power!</color>");

            // --- ROUTE THE SPELL ---
            if (spellName == "Fireball")
            {
                finalPower += 5; // Fireball gets a flat +5 damage bonus!
                if (enemyTarget != null) enemyTarget.TakeDamage(finalPower);
            }
            else if (spellName == "Ice Shards" || spellName == "Wind Blades")
            {
                if (enemyTarget != null) enemyTarget.TakeDamage(finalPower);
            }
            else if (spellName == "Revitalize")
            {
                if (playerTarget != null) playerTarget.Heal(finalPower);
            }
            else if (spellName == "Bubble Shield")
            {
                if (playerTarget != null) playerTarget.AddShield(finalPower);
            }
            // ------------------------

            // Pay the Ink Cost
            currentInk -= costOfSpell;
            UpdateInkUI();

            // Cleanup the Board
            Destroy(CardInteraction.currentlyPlayedCard.gameObject);
            CardInteraction.currentlyPlayedCard = null;

            wordInputField.text = "";
            if (spellInputPanel != null)
            {
                spellInputPanel.SetActive(false); 
            }
        }
    }

    public void DrawNewCard()
    {
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

        if (availableLetters.Count == 0)
        {
            availableLetters = new List<char>(alphabet.ToCharArray());
        }

        char chosenLetter = availableLetters[Random.Range(0, availableLetters.Count)];

        GameObject newCard = Instantiate(cardPrefab, handContainer, false);
        newCard.transform.localScale = Vector3.one;
        
        CardInteraction newCardScript = newCard.GetComponent<CardInteraction>();
        newCardScript.inputDisplayArea = this.inputDisplayArea;
        newCardScript.wordInputField = this.wordInputField;
        newCardScript.spellInputPanel = this.spellInputPanel; 
        
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

    public void UpdateInkUI()
    {
        if (totalInkTextUI != null) totalInkTextUI.text = currentInk.ToString();
    }

    public void EndTurn()
    {
        if (CardInteraction.currentlyPlayedCard != null)
        {
            CardInteraction.currentlyPlayedCard.ReturnToHand();
        }

        int survivingCards = 0;

        foreach (Transform child in handContainer)
        {
            CardInteraction card = child.GetComponent<CardInteraction>();
            if (card != null)
            {
                // --- UPDATED: Handle Locked Cards ---
                if (card.lockedTurnsLeft > 0)
                {
                    card.DecreaseLock();
                    survivingCards++; // It takes up a slot in your hand!
                }
                else if (!card.isStarred)
                {
                    Destroy(child.gameObject); 
                }
                else
                {
                    card.RemoveStar(); 
                    survivingCards++;
                }
            }
        }

        // Ink Math
        if (currentInk == 0) currentInk += 7; 
        else if (currentInk >= 10) currentInk += 3;
        else currentInk += 5; 

        if (currentInk > maxInk) currentInk = maxInk;
        UpdateInkUI();
        
        // Draw replacing cards
        int cardsNeeded = startingHandSize - survivingCards;
        for (int i = 0; i < cardsNeeded; i++)
        {
            DrawNewCard();
        }
        
        cardsStarredThisTurn = 0; 

        // --- NEW: TRIGGER GOLEM AI ---
        if (enemyTarget != null)
        {
            enemyTarget.TakeTurn();
        }
    }

    public void ReshuffleSelectedCardLetter()
    {
        if (CardInteraction.currentlyPlayedCard != null)
        {
            if (currentInk >= 1)
            {
                currentInk -= 1;
                UpdateInkUI();

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
                
                availableLetters.Remove(CardInteraction.currentlyPlayedCard.currentLetter);

                if (availableLetters.Count == 0)
                {
                    availableLetters = new List<char>(alphabet.ToCharArray());
                }

                char newLetter = availableLetters[Random.Range(0, availableLetters.Count)];

                CardInteraction.currentlyPlayedCard.ChangeLetter(newLetter);

                wordInputField.text = newLetter.ToString();
                wordInputField.MoveTextEnd(false); 
            }
            else
            {
                Debug.LogWarning("Not enough Ink to reshuffle this letter!");
            }
        }
    }

    // --- NEW: Background Click Unzoom Logic ---
    public void UnzoomBackgroundClick()
    {
        if (CardInteraction.currentlyZoomedCard != null)
        {
            CardInteraction.currentlyZoomedCard.Unzoom();
        }
    }
    // ------------------------------------------
}