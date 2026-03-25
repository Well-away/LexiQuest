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
    private bool isBannerSkipped = false;
    private bool isSubmitLocked = false;

    [Header("Battle State")]
    public int currentTurn = 1;

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
    private List<string> recentWords = new List<string>();

    [Header("Ultimate Mechanics")]
    private int previousInputLength = 0;
    private bool isWaitingForSecondWord = false; 
    private string firstDoubleCastWord = "";

    [Header("Spell States")]
    public bool isLesserSpell = false;

    void Start()
    {
        // Start the sequence!
        ChangeState(BattleState.Intro);
    }

    void Update()
    {
        // Allow pressing Enter to cast the spell quickly!
        if (currentState == BattleState.Typing)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SubmitWord();
            }
        }
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
            case BattleState.EnemyTurn:
                StartCoroutine(HandleEnemyTurn()); // <-- ADD THIS CASE
                break;
        }
    }

    // --- PHASE LOGIC ---

    IEnumerator HandleIntro()
    {
        introBanner.SetActive(true);
        
        // Actually set the text instead of just commenting it!
        bannerText.text = "IT'S OUR TURN!"; 
        
        yield return StartCoroutine(WaitOrSkip(1.5f));
        
        // Hide the banner so it doesn't overlap the Category panel
        introBanner.SetActive(false); 
        
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
        // 1. Generate the fresh quest data based on the current turn
        currentQuest = questManager.GenerateQuest(currentTurn);

        // 2. TIER 1 BANNER (The Letter)
        introBanner.SetActive(true);
        bannerText.text = "QUEST: Word starting with '" + currentQuest.targetLetter + "'";
        yield return StartCoroutine(WaitOrSkip(3f));

        // 3. TIER 2 BANNER (The Length)
        bannerText.text = "SIDE QUEST: " + currentQuest.tier2Description;
        yield return StartCoroutine(WaitOrSkip(3f));

        // 4. PREPARE TYPING UI
        // Set the persistent side-panel text so the player doesn't forget
        tier1StatusText.text = "Letter: " + currentQuest.targetLetter;
        tier2StatusText.text = "Rule: " + currentQuest.tier2Description;
        
        // Only show Tier 3 UI if it's an Ultimate Boss Turn
        if (currentQuest.tier3Rule != Tier3Type.None)
        {
            tier3StatusText.transform.parent.gameObject.SetActive(true); // Tries to turn on the background box
            tier3StatusText.gameObject.SetActive(true);
            tier3StatusText.text = currentQuest.tier3Description;
            tier3StatusText.color = Color.black; 
        }
        else
        {
            // Failsafe: Turn off the object, turn off its parent box, AND erase the text!
            tier3StatusText.text = ""; 
            tier3StatusText.gameObject.SetActive(false);
            
            // If the text is inside a white panel, this turns off the panel too:
            if (tier3StatusText.transform.parent != null)
            {
                tier3StatusText.transform.parent.gameObject.SetActive(false);
            }
        }

        introBanner.SetActive(false);
        ChangeState(BattleState.Typing);
    }

    public void SkipBanner()
    {
        // This instantly breaks the WaitOrSkip loop, allowing the game to proceed!
        isBannerSkipped = true; 
        Debug.Log("Banner Skipped!");
    }

    IEnumerator HandleTyping()
    {
        inputArea.SetActive(true);
        questPanel.SetActive(true);
        timerPanel.SetActive(true); 

        isOvertime = false; // Reset overtime flag
        wordInputField.text = ""; 
        previousInputLength = 0; // Reset Flawless tracker
        isWaitingForSecondWord = false; // <--- ADD THIS
        firstDoubleCastWord = "";       // <--- ADD THIS
        isSubmitLocked = false;

        // --- TIER 3 UI FAILSAFE ---
        // Force the Boss text to hide if we are on a normal turn
        if (currentQuest.tier3Rule == Tier3Type.None)
        {
            tier3StatusText.gameObject.SetActive(false);
        }

        // --- ULTIMATE MECHANIC: BLIND CASTING ---
        if (currentQuest.tier3Rule == Tier3Type.BlindCasting)
        {
            // Turn the input field into a Password box (shows asterisks)
            wordInputField.contentType = TMP_InputField.ContentType.Password;
            Debug.Log("<color=magenta>BLIND CASTING ACTIVE! Text is hidden.</color>");
        }
        else
        {
            // Make sure it goes back to normal text for regular turns!
            wordInputField.contentType = TMP_InputField.ContentType.Standard; 
        }

        // Force TMP to update its visuals immediately so the password setting applies before they type
        wordInputField.ForceLabelUpdate();

        wordInputField.ActivateInputField(); 

        // --- ULTIMATE MECHANIC: SPEED CASTING ---
        float startingTime = maxTypingTime; // Default is 60s
        bool allowOvertime = true;

        if (currentQuest.tier3Rule == Tier3Type.SpeedCasting)
        {
            startingTime = 10f; // Boss Override: Only 10 seconds!
            allowOvertime = false; // No mercy. No overtime allowed.
            Debug.Log("<color=red>SPEED CASTING ACTIVE! 10 Seconds Only!</color>");
        }

        currentTypingTimer = startingTime; // Use the class-level variable

        // PHASE 1: The Countdown
        while (currentTypingTimer > 0 && currentState == BattleState.Typing)
        {
            currentTypingTimer -= Time.deltaTime;
            
            // Pass startingTime so the UI bar shrinks correctly relative to 10s or 60s
            UpdateTimerUI(currentTypingTimer, startingTime); 
            
            // UX Flare: If it's Speed Casting, force the UI bar to be RED the entire time to induce panic!
            if (currentQuest.tier3Rule == Tier3Type.SpeedCasting)
            {
                timerFillBar.color = Color.red;
            }

            yield return null;
        }

        // PHASE 2: Overtime (Or Instant Death for Speed Casters)
        if (currentState == BattleState.Typing)
        {
            if (allowOvertime)
            {
                Debug.Log("<color=orange>Entering Overtime! Potency will decay.</color>");
                isOvertime = true;
                currentOvertime = maxOvertime;

                while (currentOvertime > 0 && currentState == BattleState.Typing)
                {
                    currentOvertime -= Time.deltaTime;
                    UpdateTimerUI(currentOvertime, maxOvertime); 
                    timerFillBar.color = Color.red; 
                    yield return null;
                }
            }
            else
            {
                // Speed Casting Time is Up! 
                Debug.Log("<color=red>Speed Casting Timer Exhausted! Spell Failed.</color>");
                ChangeState(BattleState.Resolution); 
                yield break; // Stop the coroutine immediately
            }
        }

        // PHASE 3: Total Failure (Timeout)
        if (currentOvertime <= 0 && currentState == BattleState.Typing && allowOvertime)
        {
            Debug.Log("<color=orange>Time out! Casting weak Arcane Bolt.</color>");
            isLesserSpell = true; // Flag it!
            ChangeState(BattleState.Resolution); 
        }
    }

    // This is called when the player clicks the "Cast Spell" button
    public void SubmitWord()
    {
        if (currentState != BattleState.Typing || isSubmitLocked) return;
        isSubmitLocked = true; // Lock the button immediately!

        // Get the single word, make it uppercase, and remove ALL spaces and non-letter characters
        string rawInput = wordInputField.text.ToUpper();
        string playerWord = System.Text.RegularExpressions.Regex.Replace(rawInput, @"[^A-Z]", "");
        
        if (string.IsNullOrEmpty(playerWord))
        {
            isSubmitLocked = false;
            return;
        }

        // --- POTENCY CALCULATION (Applies to all casts) ---
        float finalPotency = 1.0f;
        if (isOvertime)
        {
            float timeUsed = maxOvertime - currentOvertime; 
            float overtimePercentage = timeUsed / maxOvertime; 
            float penalty = overtimePercentage * maxPenaltyPercent;
            finalPotency -= penalty;
        }

        // ==========================================
        // ULTIMATE MECHANIC: SEQUENTIAL DOUBLE CAST
        // ==========================================
        if (currentQuest.tier3Rule == Tier3Type.DoubleCast)
        {
            if (!isWaitingForSecondWord)
            {
                // --- PART 1: VALIDATE THE FIRST WORD ---
                if (recentWords.Contains(playerWord))
                {
                    Debug.Log($"<color=red>Spam Filter: You used '{playerWord}' recently!</color>");
                    wordInputField.textComponent.color = Color.red; 
                    if (wordInputField.contentType == TMP_InputField.ContentType.Password)
                    {
                        wordInputField.contentType = TMP_InputField.ContentType.Standard;
                        wordInputField.ForceLabelUpdate();
                    }
                    isSubmitLocked = false;
                    return;
                }

                if (!playerWord.StartsWith(currentQuest.targetLetter))
                if (!playerWord.StartsWith(currentQuest.targetLetter))
                {
                    Debug.Log($"<color=red>Failed! Word must start with {currentQuest.targetLetter}</color>");
                    wordInputField.textComponent.color = Color.red;
                    if (wordInputField.contentType == TMP_InputField.ContentType.Password)
                    {
                        wordInputField.contentType = TMP_InputField.ContentType.Standard;
                        wordInputField.ForceLabelUpdate();
                    }
                    isSubmitLocked = false;
                    return;
                }

                bool t2Passed = QuestValidator.CheckTier2(playerWord, currentQuest.tier2Rule);
                if (t2Passed)
                {
                    Debug.Log("<color=cyan>First word accepted! Waiting for second word...</color>");
                    
                    // Reward them with time for getting the first word!
                    currentTypingTimer += 15f; 
                    if (currentTypingTimer > maxTypingTime) currentTypingTimer = maxTypingTime; // Cap it so it doesn't overflow
                    Debug.Log("<color=green>+15 Seconds added to the clock!</color>");
                    
                    // Save the first word and change state
                    firstDoubleCastWord = playerWord;
                    isWaitingForSecondWord = true;
                    
                    // UI UX Updates: Clear the box and update the side panel text
                    wordInputField.text = ""; 
                    wordInputField.textComponent.color = Color.black; 
                    tier3StatusText.text = "ULTIMATE: Double Cast (1/2 Done!)";
                    
                    // Refocus the input field so they don't have to click it again
                    wordInputField.ActivateInputField(); 
                    isSubmitLocked = false; // Unlock so they can submit the second word!
                }
                else
                {
                    Debug.Log("<color=orange>Word failed the Shape/Rule requirement.</color>");
                    wordInputField.textComponent.color = Color.red;
                    if (wordInputField.contentType == TMP_InputField.ContentType.Password)
                    {
                        wordInputField.contentType = TMP_InputField.ContentType.Standard;
                        wordInputField.ForceLabelUpdate();
                    }
                    isSubmitLocked = false;
                }
                
                return; // Stop here! Do not end the turn! Let the timer keep ticking!
            }
            else
            {
                // --- PART 2: VALIDATE THE SECOND WORD ---
                
                // Anti-Cheese check: They cannot use the exact same word they just used for Part 1!
                if (recentWords.Contains(playerWord) || playerWord == firstDoubleCastWord)
                {
                    Debug.Log("<color=red>Spam Filter: Cannot use a recent word OR your first combo word again!</color>");
                    wordInputField.textComponent.color = Color.red; 
                    if (wordInputField.contentType == TMP_InputField.ContentType.Password)
                    {
                        wordInputField.contentType = TMP_InputField.ContentType.Standard;
                        wordInputField.ForceLabelUpdate();
                    }
                    isSubmitLocked = false;
                    return;
                }

                if (!playerWord.StartsWith(currentQuest.targetLetter.ToString()))
                {
                    Debug.Log($"<color=red>Failed! Word must start with {currentQuest.targetLetter}</color>");
                    wordInputField.textComponent.color = Color.red;
                    if (wordInputField.contentType == TMP_InputField.ContentType.Password)
                    {
                        wordInputField.contentType = TMP_InputField.ContentType.Standard;
                        wordInputField.ForceLabelUpdate();
                    }
                    isSubmitLocked = false;
                    return;
                }

                bool t2Passed = QuestValidator.CheckTier2(playerWord, currentQuest.tier2Rule);
                if (t2Passed)
                {
                    Debug.Log($"<color=cyan>DOUBLE CAST SUCCESS! Ultimate Spell Activated at {(finalPotency * 100).ToString("F1")}% Power!</color>");
                    
                    // Save ONLY the second word for the next turn's spam filter
                    RecordSuccessfulWord(playerWord); 
                    
                    // Reset variables for safety
                    isWaitingForSecondWord = false;
                    firstDoubleCastWord = "";
                    
                    ChangeState(BattleState.Resolution);
                }
                else
                {
                    Debug.Log("<color=orange>Second word failed the Shape/Rule requirement.</color>");
                    wordInputField.textComponent.color = Color.red;
                    if (wordInputField.contentType == TMP_InputField.ContentType.Password)
                    {
                        wordInputField.contentType = TMP_InputField.ContentType.Standard;
                        wordInputField.ForceLabelUpdate();
                    }
                    isSubmitLocked = false;
                }
                
                return; // Stop here so it doesn't run the normal single-word code
            }
        }

        // ==========================================
        // NORMAL CASTING (1 Word - Turns 1 to 4)
        // ==========================================
        if (recentWords.Contains(playerWord))
        {
            Debug.Log($"<color=red>Spam Filter: You used '{playerWord}' recently!</color>");
            wordInputField.textComponent.color = Color.red; 
            if (wordInputField.contentType == TMP_InputField.ContentType.Password)
            {
                wordInputField.contentType = TMP_InputField.ContentType.Standard;
                wordInputField.ForceLabelUpdate();
            }
            isSubmitLocked = false;
            return; 
        }

        // 1. Mandatory Tier 1 Check
        if (!playerWord.StartsWith(currentQuest.targetLetter))
        {
            Debug.Log($"<color=red>Failed! Word must start with {currentQuest.targetLetter}</color>");
            wordInputField.textComponent.color = Color.red; 
            if (wordInputField.contentType == TMP_InputField.ContentType.Password)
            {
                wordInputField.contentType = TMP_InputField.ContentType.Standard;
                wordInputField.ForceLabelUpdate();
            }
            isSubmitLocked = false;
            return; // STOP! Let them keep typing.
        }

        // 2. Mandatory Tier 2 Check
        bool tier2Passed = QuestValidator.CheckTier2(playerWord, currentQuest.tier2Rule);
        if (!tier2Passed)
        {
            Debug.Log("<color=orange>Tier 2 Rule Failed. You must fulfill the side quest!</color>");
            wordInputField.textComponent.color = Color.red;
            if (wordInputField.contentType == TMP_InputField.ContentType.Password)
            {
                wordInputField.contentType = TMP_InputField.ContentType.Standard;
                wordInputField.ForceLabelUpdate();
            }
            isSubmitLocked = false;
            return; // STOP! Let them keep typing.
        }

        // 3. SUCCESS! Both tiers passed.
        Debug.Log($"<color=green>Spell Activated at {(finalPotency * 100).ToString("F1")}% Power.</color>");
        RecordSuccessfulWord(playerWord);
        
        isLesserSpell = false; // Normal spell achieved!
        ChangeState(BattleState.Resolution);
    }

    IEnumerator HandleResolution()
    {
        inputArea.SetActive(false);
        questPanel.SetActive(false);

        introBanner.SetActive(true);
        
        // Dynamically change the banner text based on how they performed
        if (isLesserSpell)
        {
            bannerText.text = "ARCANE BOLT! (Weak)";
            bannerText.color = Color.gray; // Make it look weak
        }
        else
        {
            bannerText.text = "SPELL CAST!";
            bannerText.color = Color.black; // <--- CHANGE TO BLACK
        }
        
        yield return StartCoroutine(WaitOrSkip(2f)); 

        introBanner.SetActive(false);
        bannerText.color = Color.black; // <--- CHANGE TO BLACK (Reset for next time)
        
        Debug.Log("Turn Ended. Golem's Turn!");
        ChangeState(BattleState.EnemyTurn);
    }

    IEnumerator HandleEnemyTurn()
    {
        Debug.Log("Enemy Turn Started.");
        
        // Show Enemy Banner
        introBanner.SetActive(true);
        bannerText.text = "ENEMY TURN...";
        yield return StartCoroutine(WaitOrSkip(1.5f));
        
        // Simulate Attack
        bannerText.text = "GOLEM ATTACKS!";
        // Later, you will trigger damage numbers and animations here
        yield return StartCoroutine(WaitOrSkip(2.0f));
        
        introBanner.SetActive(false);
        
        // LOOP BACK TO PLAYER
        Debug.Log("Enemy Turn Ended. Player's Turn!");
        currentTurn++; // <--- ADD THIS LINE
        ChangeState(BattleState.Intro); // Loop back to the player's turn
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
        if (string.IsNullOrEmpty(currentInput) || currentQuest == null) 
        {
            wordInputField.textComponent.color = Color.black; 
            previousInputLength = 0; 
            return;
        }

        // ==========================================
        // GRACEFUL DEGRADATION: FLAWLESS & BLIND
        // ==========================================
        if (currentQuest.tier3Rule == Tier3Type.FlawlessCasting || currentQuest.tier3Rule == Tier3Type.BlindCasting)
        {
            // If the input is SHORTER, they hit backspace!
            if (currentInput.Length < previousInputLength)
            {
                Debug.Log("<color=orange>Ultimate Spell Broken! Downgraded to a Normal Turn.</color>");
                
                // 1. Remove the Boss Mechanic rule so they don't get bonus points later
                currentQuest.tier3Rule = Tier3Type.None;
                
                // 2. If it was Blind Casting, reveal the text again!
                if (wordInputField.contentType == TMP_InputField.ContentType.Password)
                {
                    wordInputField.contentType = TMP_InputField.ContentType.Standard;
                    wordInputField.ForceLabelUpdate();
                }
                
                // 3. Update the UI to show they lost the bonus
                tier3StatusText.text = "ULTIMATE BROKEN!";
                tier3StatusText.color = Color.gray; 
                
                // Note: We DO NOT return here. We let the code continue so they can finish typing!
            }
        }
        
        // Always update the length tracker for the next keystroke
        previousInputLength = currentInput.Length;

        string upperInput = currentInput.ToUpper();

        // 2. Check what the game *thinks* it's comparing
        Debug.Log("Comparing '" + upperInput + "' against Target Letter '" + currentQuest.targetLetter + "'");

        if (upperInput.StartsWith(currentQuest.targetLetter))
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

    // Custom timer that stops early if the player clicks the Skip button
    private IEnumerator WaitOrSkip(float duration)
    {
        isBannerSkipped = false; // Reset the flag
        float timer = 0f;
        
        while (timer < duration && !isBannerSkipped)
        {
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        
        isBannerSkipped = false; // Reset it again for the next banner!
    }

    private void RecordSuccessfulWord(string word)
    {
        recentWords.Add(word);
        
        // If the list gets larger than 5, delete the oldest word (index 0)
        if (recentWords.Count > 5)
        {
            recentWords.RemoveAt(0);
        }
    }
}