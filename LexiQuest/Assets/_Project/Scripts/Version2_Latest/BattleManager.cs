using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum BattleState { Intro, CategorySelect, SpellSelect, QuestIntro, Typing, Resolution, EnemyTurn }

public class BattleManager : MonoBehaviour
{
    [Header("Combat Stats (Prototype)")]
    public float playerMaxHP = 100f;
    private float playerCurrentHP;
    public Image playerHealthBar; // Drag the Yellow Bar Image here

    public float enemyMaxHP = 300f; // Boss Tier Health!
    private float enemyCurrentHP;
    public Image enemyHealthBar; // Drag the Red Bar Image here

    public float baseSpellDamage = 20f;
    public float enemyBaseDamage = 25f;
    private float currentSpellPotency = 1.0f;

    [Header("Cinematic UI Toggles")]
    public GameObject mainUICanvas; // Drag the "Canvas" inside LexiQuestUIV2 here

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
    public DictionaryManager dictionaryManager;
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
    // A dictionary that links a Prefix (string) to a List of used words (List<string>)
    private Dictionary<string, List<string>> usedWordsPerPrefix = new Dictionary<string, List<string>>();
    private int maxMemoryPerPrefix = 2; // The sweet spot!

    [Header("Ultimate Mechanics")]
    private int previousInputLength = 0;
    private bool isWaitingForSecondWord = false; 
    private string firstDoubleCastWord = "";

    [Header("Spell States")]
    public bool isLesserSpell = false;

    void Start()
    {
        playerCurrentHP = playerMaxHP;
        enemyCurrentHP = enemyMaxHP;
        
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
        // Kill any running timers/waits from the previous state
        StopAllCoroutines(); 

        currentState = newState;

        // Hide all the specific interaction panels for a clean transition.
        // But do NOT hide the main Canvas, so our Health Bars stay visible!
        if (introBanner != null) introBanner.SetActive(false);
        if (categoryPanel != null) categoryPanel.SetActive(false);
        if (spellPanel != null) spellPanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        if (inputArea != null) inputArea.SetActive(false);
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
                StartCoroutine(HandleSpellSelection()); 
                break;
            case BattleState.QuestIntro:
                StartCoroutine(HandleQuestIntro());
                break;
            case BattleState.Typing:
                if (inputArea != null) inputArea.SetActive(true);
                if (questPanel != null) questPanel.SetActive(true);
                StartCoroutine(HandleTyping());
                break;
            case BattleState.Resolution:
                // Resolution hides the UI panels (handled above), but leaves the health bars alone!
                StartCoroutine(HandleResolution());
                break;
            case BattleState.EnemyTurn:
                // Enemy turn also leaves health bars alone!
                StartCoroutine(HandleEnemyTurn()); 
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

        // ==========================================
        // 3. NEW: THE DYNAMIC BLOCKED WORDS BANNER
        // ==========================================
        if (usedWordsPerPrefix.ContainsKey(currentQuest.targetLetter) && usedWordsPerPrefix[currentQuest.targetLetter].Count > 0)
        {
            // Find out how many words we should actually block for this specific turn
            int currentLimit = GetDynamicMemoryLimit(currentQuest.tier2Rule);
            List<string> history = usedWordsPerPrefix[currentQuest.targetLetter];
            
            // Grab only the most recent 'X' words based on the limit
            var activelyBlockedWords = history.Skip(Mathf.Max(0, history.Count - currentLimit)).ToList();

            if (activelyBlockedWords.Count > 0)
            {
                string blockedString = string.Join(", ", activelyBlockedWords);
                bannerText.text = "BLOCKED: " + blockedString;
                bannerText.color = Color.red; 
                
                yield return StartCoroutine(WaitOrSkip(2.5f));
                bannerText.color = Color.black; 
            }
        }

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

        // ==========================================
        // GLOBAL RULE: MINIMUM 3 LETTERS
        // ==========================================
        if (playerWord.Length < 3)
        {
            Debug.Log("<color=red>Failed! Spells must be at least 3 letters long.</color>");
            wordInputField.textComponent.color = Color.red; 
            
            // Unmask if it's a Blind Cast
            if (wordInputField.contentType == TMP_InputField.ContentType.Password)
            {
                wordInputField.contentType = TMP_InputField.ContentType.Standard;
                wordInputField.ForceLabelUpdate();
            }
            isSubmitLocked = false;
            return; 
        }

        // ==========================================
        // DICTIONARY CHECK: IS IT A REAL WORD?
        // ==========================================
        if (!dictionaryManager.IsValidWord(playerWord))
        {
            Debug.Log($"<color=red>Failed! '{playerWord}' is not a recognized English word.</color>");
            wordInputField.textComponent.color = Color.red; 
            
            // Unmask if it's a Blind Cast
            if (wordInputField.contentType == TMP_InputField.ContentType.Password)
            {
                wordInputField.contentType = TMP_InputField.ContentType.Standard;
                wordInputField.ForceLabelUpdate();
            }
            isSubmitLocked = false;
            return; 
        }

        // --- POTENCY CALCULATION ---
        float finalPotency = 1.0f;

        // 1. Length Multipliers (The Core Thesis Mechanic)
        int len = playerWord.Length;
        if (len == 3) finalPotency = 0.8f;
        else if (len >= 4 && len <= 5) finalPotency = 1.0f;
        else if (len >= 6 && len <= 7) finalPotency = 1.25f;
        else if (len >= 8) finalPotency = 1.5f;

        // 2. Ultimate Boss Bonus
        // If they just successfully finished Part 2 of a Double Cast, give them massive damage!
        if (currentQuest.tier3Rule == Tier3Type.DoubleCast && isWaitingForSecondWord)
        {
            finalPotency = 2.0f;
        }

        // 3. Overtime Penalty (Your existing logic)
        if (isOvertime)
        {
            float timeUsed = maxOvertime - currentOvertime; 
            float overtimePercentage = timeUsed / maxOvertime; 
            float penalty = overtimePercentage * maxPenaltyPercent;
            finalPotency -= penalty;
        }

        // Save it so the Resolution state can calculate the final HP reduction!
        currentSpellPotency = finalPotency;

        // ==========================================
        // ULTIMATE MECHANIC: SEQUENTIAL DOUBLE CAST
        // ==========================================
        if (currentQuest.tier3Rule == Tier3Type.DoubleCast)
        {
            if (!isWaitingForSecondWord)
            {
                // --- PART 1: VALIDATE THE FIRST WORD ---
                // Dynamic Spam Filter Check
                bool isSpamPart1 = false;
                if (usedWordsPerPrefix.ContainsKey(currentQuest.targetLetter))
                {
                    int limit = GetDynamicMemoryLimit(currentQuest.tier2Rule);
                    var history = usedWordsPerPrefix[currentQuest.targetLetter];
                    var activelyBlocked = history.Skip(Mathf.Max(0, history.Count - limit)).ToList();
                    
                    if (activelyBlocked.Contains(playerWord)) isSpamPart1 = true;
                }

                if (isSpamPart1)
                {
                    Debug.Log($"<color=red>Spam Filter: '{playerWord}' is blocked for this difficulty!</color>");
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
                // Dynamic Spam Filter Check
                bool isSpamPart2 = false;
                if (usedWordsPerPrefix.ContainsKey(currentQuest.targetLetter))
                {
                    int limit = GetDynamicMemoryLimit(currentQuest.tier2Rule);
                    var history = usedWordsPerPrefix[currentQuest.targetLetter];
                    var activelyBlocked = history.Skip(Mathf.Max(0, history.Count - limit)).ToList();
                    
                    if (activelyBlocked.Contains(playerWord)) isSpamPart2 = true;
                }
                
                // Anti-Cheese check: They cannot use the exact same word they just used for Part 1!
                if (isSpamPart2 || playerWord == firstDoubleCastWord)
                {
                    Debug.Log($"<color=red>Spam Filter: '{playerWord}' is blocked for this difficulty!</color>");
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
                    RecordSuccessfulWord(playerWord, currentQuest.targetLetter); 
                    
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
        // Dynamic Spam Filter Check
        bool isSpamNormal = false;
        if (usedWordsPerPrefix.ContainsKey(currentQuest.targetLetter))
        {
            int limit = GetDynamicMemoryLimit(currentQuest.tier2Rule);
            var history = usedWordsPerPrefix[currentQuest.targetLetter];
            var activelyBlocked = history.Skip(Mathf.Max(0, history.Count - limit)).ToList();
            
            if (activelyBlocked.Contains(playerWord)) isSpamNormal = true;
        }

        if (isSpamNormal)
        {
            Debug.Log($"<color=red>Spam Filter: '{playerWord}' is blocked for this difficulty!</color>");
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
        RecordSuccessfulWord(playerWord, currentQuest.targetLetter);
        
        isLesserSpell = false; // Normal spell achieved!
        ChangeState(BattleState.Resolution);
    }

    IEnumerator HandleResolution()
    {
        // 1. Calculate Damage based on the Potency Math
        float damageDealt = baseSpellDamage * currentSpellPotency;
        
        // If they timed out and cast a weak Arcane Bolt, cut it in half!
        if (isLesserSpell) damageDealt *= 0.5f; 

        // 2. Apply Damage to the Wind Serpent
        enemyCurrentHP -= damageDealt;
        if (enemyCurrentHP < 0) enemyCurrentHP = 0;
        
        // Update the visual Red Bar
        enemyHealthBar.fillAmount = enemyCurrentHP / enemyMaxHP;

        // Print the math to the console so you can prove it works!
        Debug.Log($"<color=cyan>Spell Power: {currentSpellPotency}x | Damage Dealt: {damageDealt} | Serpent HP: {enemyCurrentHP}</color>");

        // 3. Pause so the player can watch the 3D scene
        yield return new WaitForSeconds(3.5f); 

        // 4. Check for Boss Death
        if (enemyCurrentHP <= 0)
        {
            Debug.Log("<color=green>VICTORY! The Wind Serpent is defeated.</color>");
            yield break; 
        }
        ChangeState(BattleState.EnemyTurn);
    }

    IEnumerator HandleEnemyTurn()
    {
        Debug.Log("<color=red>Wind Serpent attacks!</color>");
        
        // Apply Damage to Amy
        playerCurrentHP -= enemyBaseDamage;
        if (playerCurrentHP < 0) playerCurrentHP = 0;

        // Update the visual Yellow Bar
        playerHealthBar.fillAmount = playerCurrentHP / playerMaxHP;

        Debug.Log($"<color=orange>Serpent hits Amy for {enemyBaseDamage} damage! Amy HP: {playerCurrentHP}</color>");

        // Pause to watch the enemy attack
        yield return new WaitForSeconds(3.0f);

        // Check for Player Death
        if (playerCurrentHP <= 0)
        {
            Debug.Log("<color=red>GAME OVER! Amy has fallen.</color>");
            yield break; 
        }
        
        // LOOP BACK TO PLAYER
        currentTurn++; 
        ChangeState(BattleState.Intro); 
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

    private void RecordSuccessfulWord(string word, string prefix)
    {
        if (!usedWordsPerPrefix.ContainsKey(prefix))
        {
            usedWordsPerPrefix[prefix] = new List<string>();
        }
        
        usedWordsPerPrefix[prefix].Add(word);
        
        // If the list gets larger than the max memory, delete the oldest word (index 0)
        if (usedWordsPerPrefix[prefix].Count > maxMemoryPerPrefix)
        {
            usedWordsPerPrefix[prefix].RemoveAt(0);
        }
    }

    private int GetDynamicMemoryLimit(Tier2Type rule)
    {
        // Define the Easy rules (These keep the 2-word memory)
        Tier2Type[] easyRules = { 
            Tier2Type.ExactLength_4, Tier2Type.ExactLength_5, Tier2Type.MinLength_5,
            Tier2Type.EndsWith_S, Tier2Type.EndsWith_T, Tier2Type.EndsWith_E, Tier2Type.EndsWith_Y,
            Tier2Type.No_Letter_P, Tier2Type.No_Letter_C, Tier2Type.No_Letter_L
        };

        // If it's an Easy rule, block 2 words. If Medium/Hard, only block 1!
        if (easyRules.Contains(rule)) return 2;
        return 1; 
    }
}