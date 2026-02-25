using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine.UI; 

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
    
    [Header("Battle Targets")]
    public player playerTarget; 
    public Enemy enemyTarget;

    [Header("Timer System")]
    public Slider timerSlider; 
    public float maxTurnTime = 60f; // PATCH 1: 1-Minute Total Timer
    private float currentTimer; 
    private bool isTimerRunning = false;

    [Header("Grimoire System")]
    public int grimoireCooldown = 5; 
    private Queue<string> recentWords = new Queue<string>(); 

    [Header("Word Discovery System")]
    private HashSet<string> discoveredWords = new HashSet<string>();

    void Start()
    {
        if (spellInputPanel != null) spellInputPanel.SetActive(false);
        if (timerSlider != null) timerSlider.gameObject.SetActive(false); 
        
        currentInk = 4; 
        UpdateInkUI();

        for (int i = 0; i < startingHandSize; i++)
        {
            DrawNewCard();
        }

        // Start the global turn timer the moment the game begins!
        StartTurnTimer();
    }

    void Update()
    {
        if (isTimerRunning)
        {
            currentTimer -= Time.deltaTime; 
            
            if (timerSlider != null) 
            {
                timerSlider.value = currentTimer;
            }

            if (currentTimer <= 0)
            {
                Debug.LogWarning("<color=red>Time's up!</color> Turn automatically ended.");
                StopTimer();
                EndTurn(); 
            }
        }
    }

    public void StartTurnTimer()
    {
        currentTimer = maxTurnTime;
        isTimerRunning = true;
        
        if (timerSlider != null) 
        {
            timerSlider.maxValue = maxTurnTime; 
            timerSlider.value = currentTimer;
            timerSlider.gameObject.SetActive(true); 
        }
    }

    public void StopTimer()
    {
        isTimerRunning = false;
        if (timerSlider != null) 
        {
            timerSlider.gameObject.SetActive(false); 
        }
    }

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

            if (effectiveLength <= 4) 
            {
                multiplier = 0.5f;     
            }
            else if (effectiveLength == 5) 
            {
                multiplier = 1.0f; 
            }
            else if (effectiveLength >= 6) 
            {
                multiplier = 1.0f + ((effectiveLength - 5) * 0.12f);
            }

            string normalizedWord = submittedWord.ToLower();

            bool isNewDiscovery = !discoveredWords.Contains(normalizedWord);
            if (isNewDiscovery)
            {
                multiplier *= 1.5f; 
                discoveredWords.Add(normalizedWord);
            }

            bool isSpam = recentWords.Contains(normalizedWord);
            if (isSpam)
            {
                multiplier *= 0.5f; 
                Debug.LogWarning($"<color=orange>GRIMOIRE PENALTY!</color> '{submittedWord}' is on cooldown! Damage reduced by 50%.");
            }

            int finalPower = Mathf.RoundToInt(basePower * multiplier);

            string discoveryLog = isNewDiscovery ? " <color=yellow>[NEW DISCOVERY x1.5]</color>" : "";
            Debug.Log($"<color=cyan>CASTING:</color> {spellName} via '{submittedWord}' (Eff Length: {effectiveLength}). Base: {basePower} * Mult: {multiplier}x = <color=yellow>{finalPower} Power!</color>{discoveryLog}");

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

            if (!isSpam) 
            {
                recentWords.Enqueue(normalizedWord);
                if (recentWords.Count > grimoireCooldown)
                {
                    recentWords.Dequeue(); 
                }
            }

            // NOTE: We no longer stop the timer here! The turn keeps going!

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

        StopTimer(); // Pause it while the turn transition happens

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
                survivingCards++; 
            }
        }

        if (currentInk == 0) currentInk += 5; 
        else if (currentInk >= 8) currentInk += 3;
        else currentInk += 4; 

        if (currentInk > maxInk) currentInk = maxInk;
        UpdateInkUI();
        
        int cardsToDraw = 0;
        
        if (survivingCards <= 1) cardsToDraw = 2; 
        else cardsToDraw = 1; 

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

        // Restart the fresh 60 seconds for the new turn!
        StartTurnTimer();
    }

    // PATCH 2: Renamed to ShuffleLetter
    public void ShuffleLetter()
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

                // --- PATCH 2: Deduct 5 seconds instead of resetting ---
                currentTimer -= 5f; 
                // We do not need to manually check if it drops below 0 here. 
                // The Update() loop will catch it on the very next frame and force an EndTurn!
            }
            else
            {
                Debug.LogWarning("Not enough Ink to shuffle this letter!");
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