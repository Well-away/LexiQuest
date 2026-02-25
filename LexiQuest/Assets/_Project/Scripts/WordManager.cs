using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq; // NEW: Required for Queue.Contains()
using UnityEngine.UI; // NEW: Required for the Slider!

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
    public int startingHandSize = 2; 
    public int maxHandSize = 5;

    [Header("Ink System")]
    public int maxInk = 15; 
    public int currentInk; 
    public TextMeshProUGUI totalInkTextUI;
    public int cardsStarredThisTurn = 0;
    
    [Header("Battle Targets")]
    public player playerTarget; 
    public Enemy enemyTarget;

    [Header("Timer System")]
    public Slider timerSlider; 
    public float maxTurnTime = 25f; // PATCH: Reduced to 25 seconds
    private float currentTimer; 
    private bool isTimerRunning = false;

    // --- NEW: Grimoire History Tracking ---
    [Header("Grimoire System")]
    public int grimoireCooldown = 5; 
    private Queue<string> recentWords = new Queue<string>(); 
    // --------------------------------------

    void Start()
    {
        if (spellInputPanel != null) spellInputPanel.SetActive(false);
        if (timerSlider != null) timerSlider.gameObject.SetActive(false); // Hide timer at start
        
        currentInk = 4; 
        UpdateInkUI();

        for (int i = 0; i < startingHandSize; i++)
        {
            DrawNewCard();
        }
    }

    // --- NEW: The Timer Countdown Logic ---
    void Update()
    {
        if (isTimerRunning)
        {
            currentTimer -= Time.deltaTime; // Smoothly subtracts time every frame
            
            if (timerSlider != null) 
            {
                timerSlider.value = currentTimer;
            }

            if (currentTimer <= 0)
            {
                Debug.LogWarning("<color=red>Time's up!</color> Turn automatically ended.");
                StopTimer();
                EndTurn(); // Force the turn to end if time runs out!
            }
        }
    }

    public void StartTimer()
    {
        currentTimer = maxTurnTime;
        isTimerRunning = true;
        
        if (timerSlider != null) 
        {
            timerSlider.maxValue = maxTurnTime;
            timerSlider.value = currentTimer;
            timerSlider.gameObject.SetActive(true); // Reveal the sliding bar
        }
    }

    public void StopTimer()
    {
        isTimerRunning = false;
        if (timerSlider != null) 
        {
            timerSlider.gameObject.SetActive(false); // Hide the bar when not spelling
        }
    }
    // --------------------------------------

    public void SubmitWord()
    {
        if (CardInteraction.currentlyPlayedCard != null)
        {
            if (playerTarget != null && playerTarget.isStunned)
            {
                Debug.LogWarning("You are STUNNED by Binding! You cannot cast spells. You must click End Turn.");
                return; 
            }

            string submittedWord = wordInputField.text;

            if (submittedWord.Length <= 2)
            {
                Debug.LogWarning("Word is too short! Spells require at least 3 letters.");
                return; 
            }

            int costOfSpell = CardInteraction.currentlyPlayedCard.inkCost;
            if (currentInk < costOfSpell)
            {
                Debug.LogWarning("Not enough Ink to cast this spell!");
                return; 
            }

            string spellName = CardInteraction.currentlyPlayedCard.currentSpell;
            int basePower = costOfSpell * 5; 
            
            int effectiveLength = GetEffectiveWordLength(submittedWord);
            float multiplier = 1.0f;

            if (effectiveLength <= 4) multiplier = 0.5f;     
            else if (effectiveLength == 5) multiplier = 1.0f; 
            else if (effectiveLength == 6) multiplier = 1.5f;
            else if (effectiveLength == 7) multiplier = 2.0f;
            else if (effectiveLength >= 8) multiplier = 2.5f;

            // --- NEW: GRIMOIRE SPAM PENALTY ---
            string normalizedWord = submittedWord.ToLower();
            bool isSpam = recentWords.Contains(normalizedWord);

            if (isSpam)
            {
                multiplier *= 0.5f; // Cuts the final multiplier in half!
                Debug.LogWarning($"<color=orange>GRIMOIRE PENALTY!</color> '{submittedWord}' is on cooldown! Damage reduced by 50%.");
            }
            // ----------------------------------

            int finalPower = Mathf.RoundToInt(basePower * multiplier);

            Debug.Log($"<color=cyan>CASTING:</color> {spellName} via '{submittedWord}' (Effective Length: {effectiveLength}). Base: {basePower} * Mult: {multiplier}x = <color=yellow>{finalPower} Power!</color>");

            // --- NEW: UPDATE GRIMOIRE HISTORY ---
            // Only add the word to the history if it isn't already sitting in the queue
            if (!isSpam) 
            {
                recentWords.Enqueue(normalizedWord);
                if (recentWords.Count > grimoireCooldown)
                {
                    recentWords.Dequeue(); // Removes the oldest word once we hit 5!
                }
            }
            // ------------------------------------

            if (spellName == "Fireball")
            {
                finalPower += 5; 
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

            currentInk -= costOfSpell;
            UpdateInkUI();

            StopTimer(); // NEW: Turn off the timer when a spell is successfully cast!

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
        Debug.Log("Player ended their turn!");

        StopTimer(); 

        if (playerTarget != null)
        {
            playerTarget.ClearStun();
        }

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
                if (card.lockedTurnsLeft > 0)
                {
                    card.DecreaseLock();
                }
                
                // --- PATCH 3: ALL cards survive! No more random destruction. ---
                survivingCards++; 
            }
        }

        if (currentInk == 0) currentInk += 5; 
        else if (currentInk >= 8) currentInk += 3;
        else currentInk += 4; 

        if (currentInk > maxInk) currentInk = maxInk;
        UpdateInkUI();
        
        // --- PATCH 2: Card Regen Math ---
        int cardsToDraw = 0;
        
        if (survivingCards <= 1) 
        {
            cardsToDraw = 2; // Refill 2 if empty or holding 1
        }
        else 
        {
            cardsToDraw = 1; // Refill 1 if holding 2 or more
        }

        // --- PATCH 1: Enforce Max Hand Size ---
        if (survivingCards + cardsToDraw > maxHandSize)
        {
            cardsToDraw = maxHandSize - survivingCards;
        }

        for (int i = 0; i < cardsToDraw; i++)
        {
            DrawNewCard();
        }

        if (enemyTarget != null)
        {
            enemyTarget.TakeTurn(); 
        }

        if (playerTarget != null)
        {
            playerTarget.HandleStartOfTurn(); 
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

                StartTimer(); // NEW: Rerolling perfectly resets the 40 seconds!
            }
            else
            {
                Debug.LogWarning("Not enough Ink to reshuffle this letter!");
            }
        }
    }

    public void UnzoomBackgroundClick()
    {
        if (CardInteraction.currentlyZoomedCard != null)
        {
            CardInteraction.currentlyZoomedCard.Unzoom();
        }
    }

    private int GetEffectiveWordLength(string word)
    {
        string w = word.ToLower();
        int len = w.Length;

        if (w.EndsWith("ing") && len >= 6) return len - 3; 
        if (w.EndsWith("ed") && len >= 5) return len - 2;  
        if (w.EndsWith("es") && len >= 5) return len - 2;  
        if (w.EndsWith("s") && len >= 4 && !w.EndsWith("ss")) return len - 1; 

        return len; 
    }
}