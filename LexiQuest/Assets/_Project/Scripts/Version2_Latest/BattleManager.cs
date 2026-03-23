using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum BattleState { Intro, CategorySelect, SpellSelect, QuestIntro, Typing, Resolution, EnemyTurn }

public class BattleManager : MonoBehaviour
{
    [Header("Typing Phase")]
    public TMP_InputField wordInputField;
    private float currentTypingTimer;
    public BattleState currentState;

    [Header("UI Panels")]
    public GameObject introBanner;
    public GameObject categoryPanel;
    public GameObject spellPanel;
    public GameObject questPanel;
    public GameObject inputArea;
    public QuestManager questManager; // Drag the script here in inspector
    private QuestData currentQuest;

    [Header("Quest UI Text")]
    public TextMeshProUGUI bannerText; // The text on your IntroBanner
    public TextMeshProUGUI tier1StatusText; // Persistent "Starts with..." text
    public TextMeshProUGUI tier2StatusText; // Persistent "Length..." text
    public TextMeshProUGUI tier3StatusText; // Persistent "Side Quest..." text

    [Header("Timer UI")]
    public GameObject timerPanel;
    public Image timerFillBar;
    public TextMeshProUGUI timerText;

    [Header("Settings")]
    public float selectionTimer = 10f;       // For Category pick
    public float spellSelectionTimer = 15f;  // For Spell pick
    public float maxTypingTime = 60f;        // For the actual word quest
    private float currentTimer;

    [Header("Overtime Settings")]
    public float maxOvertime = 30f; // 30 extra seconds
    public float maxPenaltyPercent = 0.30f; // Up to 30% damage reduction
    private bool isOvertime = false;
    private float currentOvertime;

    [Header("Spam Filter")]
    private string lastSuccessfulWord = "";

    void Start()
    {
        // Start the sequence!
        ChangeState(BattleState.Intro);
    }

    public void ChangeState(BattleState newState)
    {
        // PATCH 1: Kill any running timers/waits from the previous state
        StopAllCoroutines(); 

        currentState = newState;

        // Hide all panels first for a clean transition
        introBanner.SetActive(false);
        categoryPanel.SetActive(false);
        spellPanel.SetActive(false);
        questPanel.SetActive(false);
        inputArea.SetActive(false);
        
        // PATCH 2: Hide the timer during transitions
        if (timerPanel != null) timerPanel.SetActive(false); 

        switch (currentState)
        {
            case BattleState.Intro:
                StartCoroutine(HandleIntro());
                break;
            case BattleState.CategorySelect:
                StartCoroutine(HandleCategorySelection());
                break;
            case BattleState.SpellSelect:
                StartCoroutine(HandleSpellSelection()); // Changed this line!
                break;
            case BattleState.QuestIntro:
                StartCoroutine(HandleQuestIntro());
                break;
            case BattleState.Typing:
                inputArea.SetActive(true);
                questPanel.SetActive(true);
                StartCoroutine(HandleTyping());
                break;
            case BattleState.Resolution:
                StartCoroutine(HandleResolution());
                break;
        }
    }

    // --- PHASE LOGIC ---

    IEnumerator HandleIntro()
    {
        introBanner.SetActive(true);
        // Change text to "It's our turn!"
        yield return new WaitForSeconds(1.5f);
        ChangeState(BattleState.CategorySelect);
    }

    IEnumerator HandleCategorySelection()
    {
        categoryPanel.SetActive(true);
        timerPanel.SetActive(true); // Turn on Timer
        float currentTimer = selectionTimer;

        while (currentTimer > 0 && currentState == BattleState.CategorySelect)
        {
            currentTimer -= Time.deltaTime;
            UpdateTimerUI(currentTimer, selectionTimer); // Update visual
            yield return null;
        }

        if (currentTimer <= 0 && currentState == BattleState.CategorySelect)
        {
            Debug.Log("Time out! Defaulting to Defensive.");
            OnCategorySelected("Defense");
        }
    }

    public void OnCategorySelected(string category)
    {
        Debug.Log("Selected: " + category);
        ChangeState(BattleState.SpellSelect);
    }

    IEnumerator HandleSpellSelection()
    {
        spellPanel.SetActive(true);
        timerPanel.SetActive(true); // Turn on the visual timer

        float currentSpellTimer = spellSelectionTimer;

        // The 15-second countdown loop
        while (currentSpellTimer > 0 && currentState == BattleState.SpellSelect)
        {
            currentSpellTimer -= Time.deltaTime;
            UpdateTimerUI(currentSpellTimer, spellSelectionTimer); // Update visual bar
            yield return null;
        }

        // If time runs out and they haven't clicked a spell yet
        if (currentSpellTimer <= 0 && currentState == BattleState.SpellSelect)
        {
            Debug.Log("Time out! Defaulting to the first spell.");
            // Simulate the player clicking the first spell option
            OnSpellSelected("Default Spell"); 
        }
    }

    public void OnSpellSelected(string spellName)
    {
        Debug.Log("Spell chosen: " + spellName);
        ChangeState(BattleState.QuestIntro);
    }

    IEnumerator HandleQuestIntro()
    {
        // 1. Generate the fresh quest data
        currentQuest = questManager.GenerateQuest();

        // 2. TIER 1 BANNER (The Letter)
        introBanner.SetActive(true);
        bannerText.text = "QUEST: Word starting with '" + currentQuest.targetLetter + "'";
        yield return new WaitForSeconds(3f);

        // 3. TIER 2 BANNER (The Length)
        bannerText.text = "SIDE QUEST: Exactly " + currentQuest.targetLength + " letters";
        yield return new WaitForSeconds(3f);

        // 4. PREPARE TYPING UI
        // Set the persistent side-panel text so the player doesn't forget
        tier1StatusText.text = "Letter: " + currentQuest.targetLetter;
        tier2StatusText.text = "Length: " + currentQuest.targetLength;
        tier3StatusText.text = "Bonus: " + currentQuest.tier3Description;

        introBanner.SetActive(false);
        ChangeState(BattleState.Typing);
    }

    public void SkipBanner()
    {
        // Only allow skipping if we are actually in the Quest Intro phase
        if (currentState == BattleState.QuestIntro)
        {
            Debug.Log("Player skipped the banner!");
            
            // Set up the persistent text manually since we skipped the Coroutine
            tier1StatusText.text = "Letter: " + currentQuest.targetLetter;
            tier2StatusText.text = "Length: " + currentQuest.targetLength;
            tier3StatusText.text = "Bonus: " + currentQuest.tier3Description;
            
            // Jump straight to typing
            ChangeState(BattleState.Typing);
        }
    }

    IEnumerator HandleTyping()
    {
        inputArea.SetActive(true);
        questPanel.SetActive(true);
        timerPanel.SetActive(true); 

        float currentTypingTimer = maxTypingTime;
        isOvertime = false; // Reset overtime flag
        wordInputField.text = ""; 
        wordInputField.ActivateInputField(); 

        // PHASE 1: The Standard 60-Second Countdown
        while (currentTypingTimer > 0 && currentState == BattleState.Typing)
        {
            currentTypingTimer -= Time.deltaTime;
            UpdateTimerUI(currentTypingTimer, maxTypingTime); 
            yield return null;
        }

        // PHASE 2: The 30-Second Overtime Countdown
        if (currentState == BattleState.Typing)
        {
            Debug.Log("<color=orange>Entering Overtime! Potency will decay.</color>");
            isOvertime = true;
            currentOvertime = maxOvertime;

            while (currentOvertime > 0 && currentState == BattleState.Typing)
            {
                currentOvertime -= Time.deltaTime;
                UpdateTimerUI(currentOvertime, maxOvertime); 
                
                // Visual UX: Keep the bar red to indicate danger
                timerFillBar.color = Color.red; 
                yield return null;
            }
        }

        // PHASE 3: Total Failure
        if (currentOvertime <= 0 && currentState == BattleState.Typing)
        {
            Debug.Log("<color=red>Overtime exhausted! Spell Failed.</color>");
            ChangeState(BattleState.Resolution); 
        }
    }

    // This is called when the player clicks the "Cast Spell" button
    public void SubmitWord()
    {
        if (currentState != BattleState.Typing) return;

        // Get the word, make it uppercase, and remove accidental spaces
        string playerWord = wordInputField.text.ToUpper().Trim();

        if (string.IsNullOrEmpty(playerWord)) return;

        // --- NEW: 1-WORD COOLDOWN (SPAM FILTER) ---
        if (playerWord == lastSuccessfulWord)
        {
            Debug.Log($"<color=red>Spam Filter: You used '{playerWord}' last turn! Find a new word.</color>");
            
            // Turn the text red manually since OnInputValueChanged only checks the first letter
            wordInputField.textComponent.color = Color.red; 
            return; // Stop the code here. The timer will keep ticking!
        }

        Debug.Log("Player typed: " + playerWord);

        // --- VALIDATION ENGINE ---

        // 1. Check Tier 1 (Must start with the target letter)
        if (playerWord.StartsWith(currentQuest.targetLetter.ToString()))
        {
            // --- NEW: POTENCY CALCULATION ---
            float finalPotency = 1.0f; // 100% base power

            if (isOvertime)
            {
                // Calculate how much of the 30s was used up
                float timeUsed = maxOvertime - currentOvertime; 
                float overtimePercentage = timeUsed / maxOvertime; 
                
                // Calculate the penalty (max 30% reduction)
                float penalty = overtimePercentage * maxPenaltyPercent;
                finalPotency -= penalty;
                
                Debug.Log($"<color=orange>Overtime Penalty Applied: -{(penalty * 100).ToString("F1")}%</color>");
            }

            Debug.Log($"<color=green>Tier 1 Passed! Spell Activated at {(finalPotency * 100).ToString("F1")}% Power.</color>");

            // --- NEW: SAVE THE WORD TO MEMORY ---
            // Because they successfully cast the spell, we remember this word for next turn.
            lastSuccessfulWord = playerWord;

            // 2. Check Tier 2 (Exact Length)
            bool tier2Passed = (playerWord.Length == currentQuest.targetLength);
            
            if (tier2Passed)
            {
                Debug.Log("<color=purple>Tier 2 Passed! Combo Triggered!</color>");

                // 3. PATCH 2: Tier 3 is now NESTED inside Tier 2. 
                // It can only activate if the length requirement was met first!
                bool tier3Passed = false;
                
                switch (currentQuest.tier3Type)
                {
                    case "2+ Vowels":
                        int vowelCount = playerWord.Count(c => "AEIOU".Contains(c));
                        if (vowelCount >= 2) tier3Passed = true;
                        break;
                    case "Contains 'T' or 'R'":
                        if (playerWord.Contains("T") || playerWord.Contains("R")) tier3Passed = true;
                        break;
                    case "Ends in 'S' or 'D'":
                        if (playerWord.EndsWith("S") || playerWord.EndsWith("D")) tier3Passed = true;
                        break;
                }

                if (tier3Passed)
                {
                    Debug.Log("<color=#FFD700>Tier 3 Passed! Master Level Word!</color>");
                    
                    // --- WE WILL ADD THE COMBO STACK LOGIC HERE NEXT ---
                }
            }
            else
            {
                // Optional UX: Tell the player they missed the combo
                Debug.Log("<color=orange>Tier 1 Passed, but Tier 2 Length Failed.</color>");
            }

            ChangeState(BattleState.Resolution);
        }
        else
        {
            Debug.Log($"<color=red>Failed! Word must start with {currentQuest.targetLetter}</color>");
            // We removed the auto-clear. The text stays in the box and remains red!
        }
    }

    IEnumerator HandleResolution()
    {
        inputArea.SetActive(false);
        questPanel.SetActive(false);

        introBanner.SetActive(true);
        bannerText.text = "SPELL CAST!";
        yield return new WaitForSeconds(2f);

        introBanner.SetActive(false);
        Debug.Log("Turn Ended. Golem's Turn!");
        // We will loop back to the enemy turn here later
    }

    private void UpdateTimerUI(float currentTime, float maxTime)
    {
        // Prevent errors if UI isn't assigned
        if (timerFillBar == null || timerText == null) return; 

        // Update the bar size (Fill Amount goes from 0.0 to 1.0)
        float fillPercentage = currentTime / maxTime;
        timerFillBar.fillAmount = fillPercentage;

        // Update the text (CeilToInt rounds up so it doesn't show 0 until it's actually over)
        timerText.text = Mathf.CeilToInt(currentTime).ToString() + "s";

        // Thesis UX Touch: Change color to red when time is low (< 25%)
        if (fillPercentage <= 0.25f)
        {
            timerFillBar.color = Color.red;
        }
        else
        {
            timerFillBar.color = Color.green; 
        }
    }

    // Called automatically every time the player types or deletes a letter
    public void OnInputValueChanged(string currentInput)
    {
        // 1. Check if the function is firing at all
        Debug.Log("Typing detected! Current input is: " + currentInput);

        if (string.IsNullOrEmpty(currentInput) || currentQuest == null) 
        {
            wordInputField.textComponent.color = Color.black; // (Change to Color.white if your text is normally white!)
            return;
        }

        string upperInput = currentInput.ToUpper();

        // 2. Check what the game *thinks* it's comparing
        Debug.Log("Comparing '" + upperInput + "' against Target Letter '" + currentQuest.targetLetter + "'");

        if (upperInput.StartsWith(currentQuest.targetLetter.ToString()))
        {
            wordInputField.textComponent.color = Color.black; 
            Debug.Log("Match! Color should be Black.");
        }
        else
        {
            wordInputField.textComponent.color = Color.red;   
            Debug.Log("No Match! Color should be Red.");
        }
    }
}