using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum BattleState { MainMenu, Intro, CategorySelect, SpellSelect, QuestIntro, Typing, Resolution, EnemyTurn, GameOver }

public class BattleManager : MonoBehaviour
{
    [Header("Combat Stats (Prototype)")]
    public float playerMaxHP = 100f;
    private float playerCurrentHP;
    public Image playerHealthBar; // Drag the Yellow Bar Image here
    public TextMeshProUGUI playerHPText; // NEW: The text showing Amy's HP

    // --- NEW SHIELD VARIABLES ---
    public float playerCurrentShield = 0f;
    public Image playerShieldBar; // Drag the White Bar Image here!

    public float enemyMaxHP = 300f; // Boss Tier Health!
    private float enemyCurrentHP;
    public Image enemyHealthBar; // Drag the Red Bar Image here
    public TextMeshProUGUI enemyHPText; // NEW: The text showing the Boss's HP

    public float baseSpellPotency = 20f;
    private float currentCastBasePotency; // Holds Base + Flat Length Bonus!
    public float enemyBaseDamage = 25f;
    private float currentSpellPotency = 1.0f;

    [Header("Cinematic UI Toggles")]
    public GameObject mainUICanvas; // Drag the "Canvas" inside LexiQuestUIV2 here

    [Header("Endless Mode UI")]
    public GameObject mainMenuPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI scoreText;
    private int enemiesDefeatedCount = 0;

    [Header("Phase 1 & 2 Spell UI")]
    public GameObject categorySelectPanel;
    public GameObject spellSelectPanel;
    public Button[] spellButtons; // NEW: The actual Button components so we can lock them!
    public TextMeshProUGUI[] spellButtonTexts; 
    
    private SpellType activeSpell;
    private SpellType[] currentlyDisplayedSpells = new SpellType[6]; // Expanded to 6!

    [Header("Cooldown & Status Memory")]
    public Dictionary<SpellType, int> spellCooldowns = new Dictionary<SpellType, int>();
    public bool focusActive = false;
    public float focusDamageBonus = 0f;
    private int lastWordLength = 0;

    [Header("Typing Phase")]
    public TMP_InputField wordInputField;
    private float currentTypingTimer;
    public BattleState currentState;
    private bool isBannerSkipped = false;
    private bool isSubmitLocked = false;

    [Header("Mobile Keyboard Offset")]
    public float keyboardYOffset = 450f; // How high it moves up (Tweak this in Inspector!)
    private RectTransform inputAreaRect;
    private Vector2 originalInputPos;

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

    [Header("Clutch Suggestions")]
    public GameObject suggestionPanel;
    public TextMeshProUGUI suggestionText;

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

    [Header("Status Effects")]
    public int enemyTurnSkipCount = 0;
    public float enemyDamageMultiplier = 1.0f;

    void Start()
    {
        if (inputArea != null)
        {
            inputAreaRect = inputArea.GetComponent<RectTransform>();
            originalInputPos = inputAreaRect.anchoredPosition;
        }

        // Go to Main Menu instead of Intro!
        ChangeState(BattleState.MainMenu); 
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

        // ==========================================
        // MOBILE KEYBOARD AVOIDANCE
        // ==========================================
        if (inputAreaRect != null && inputArea.activeSelf)
        {
            // If the player is actively typing, target the shifted position. Otherwise, target the original.
            float targetY = wordInputField.isFocused ? (originalInputPos.y + keyboardYOffset) : originalInputPos.y;
            
            Vector2 targetPos = new Vector2(originalInputPos.x, targetY);

            // Vector2.Lerp smoothly glides the panel to the target position instead of teleporting it instantly
            inputAreaRect.anchoredPosition = Vector2.Lerp(inputAreaRect.anchoredPosition, targetPos, Time.deltaTime * 10f);
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
        if (suggestionPanel != null) suggestionPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (categorySelectPanel != null) categorySelectPanel.SetActive(false);
        if (spellSelectPanel != null) spellSelectPanel.SetActive(false);

        switch (currentState)
        {
            case BattleState.MainMenu:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                break;
            case BattleState.Intro:
                // Reduce all active cooldowns by 1!
                List<SpellType> keys = new List<SpellType>(spellCooldowns.Keys);
                foreach (SpellType key in keys)
                {
                    if (spellCooldowns[key] > 0) spellCooldowns[key]--;
                }

                StartCoroutine(WaitAndChangeState(2f, BattleState.CategorySelect));
                break;
            case BattleState.CategorySelect:
                StartCoroutine(HandleCategorySelection());
                break;
            case BattleState.SpellSelect:
                StartCoroutine(HandleSpellSelection());
                break;
            case BattleState.QuestIntro:
                // Advance the global memory clock by 1!
                if (dictionaryManager != null) dictionaryManager.globalTurnCount++; 
                
                StartCoroutine(HandleQuestIntro());
                break;
            case BattleState.Typing:
                // --- THESIS GOAL 3: DYNAMIC TIMERS ---
                float calculatedTimer = 30f; // Base time for standard constraints
                
                switch(currentQuest.tier2Rule)
                {
                    case Tier2Type.ExactLength_4: calculatedTimer = 40f; break; // 30 + 10
                    case Tier2Type.ExactLength_5: calculatedTimer = 50f; break; // 30 + 20
                    case Tier2Type.ExactLength_6: calculatedTimer = 60f; break; // 30 + 30
                    case Tier2Type.ExactLength_7: calculatedTimer = 70f; break; // 30 + 40
                    case Tier2Type.ExactLength_8: calculatedTimer = 80f; break; // 30 + 50
                    case Tier2Type.ExactLength_9: calculatedTimer = 90f; break; // 30 + 60
                }

                // Set your typing timer variables here!
                maxTypingTime = calculatedTimer;
                currentTypingTimer = calculatedTimer;

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
            case BattleState.GameOver:
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                if (scoreText != null) scoreText.text = "Enemies Defeated: " + enemiesDefeatedCount;
                break;
        }
    }

    private IEnumerator WaitAndChangeState(float delay, BattleState nextState)
    {
        yield return new WaitForSeconds(delay);
        ChangeState(nextState);
    }

    public void StartEndlessGame()
    {
        playerCurrentHP = playerMaxHP;
        enemyCurrentHP = enemyMaxHP;
        playerCurrentShield = 0f;
        enemiesDefeatedCount = 0;
        enemyTurnSkipCount = 0;
        enemyDamageMultiplier = 1.0f;
        
        UpdateHealthUI();
        
        ChangeState(BattleState.Intro);
    }

    public void RestartGame()
    {
        // Reset the dictionary memory so they can use old words again
        usedWordsPerPrefix.Clear(); 
        
        StartEndlessGame();
    }

    public void SelectCategory(int categoryIndex)
    {
        SpellCategory chosenCategory = (SpellCategory)categoryIndex;
        PopulateSpellMenu(chosenCategory);
        ChangeState(BattleState.SpellSelect);
    }

    private void PopulateSpellMenu(SpellCategory category)
    {
        List<SpellType> pool = new List<SpellType>();

        if (category == SpellCategory.Offensive)
            pool = new List<SpellType> { SpellType.ArcaneBolts, SpellType.GaleBurst, SpellType.FlameBlast, SpellType.FrostSpikes, SpellType.EarthThrow, SpellType.ThunderStrike };
        else if (category == SpellCategory.Utility)
            pool = new List<SpellType> { SpellType.FlameBarrier, SpellType.WindVeil, SpellType.ManaShield, SpellType.Restraint, SpellType.Focus };
        else if (category == SpellCategory.Healing)
            pool = new List<SpellType> { SpellType.SoothingWaters, SpellType.Cleanse, SpellType.PurifyingFlames, SpellType.Revitalize, SpellType.WinterEmbrace };

        // Loop through all 6 UI buttons
        for (int i = 0; i < 6; i++)
        {
            if (i < pool.Count)
            {
                spellButtons[i].gameObject.SetActive(true);
                currentlyDisplayedSpells[i] = pool[i];
                SpellType s = pool[i];

                // Check cooldowns
                int cd = spellCooldowns.ContainsKey(s) ? spellCooldowns[s] : 0;
                spellButtons[i].interactable = (cd == 0); // Lock button if CD > 0

                // Format the text
                string spellName = string.Concat(s.ToString().Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
                if (cd > 0) spellButtonTexts[i].text = $"{spellName} (CD: {cd})";
                else spellButtonTexts[i].text = spellName;
            }
            else
            {
                // Hide extra buttons (e.g., Healing only has 5 spells)
                spellButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void SelectDisplayedSpell(int buttonIndex)
    {
        activeSpell = currentlyDisplayedSpells[buttonIndex];
        
        // BUG FIX: Pass 'currentTurn' so the Ultimate Boss mechanic triggers exactly on Turn 5!
        currentQuest = questManager.GenerateQuest(currentTurn, activeSpell); 
        
        ChangeState(BattleState.QuestIntro);
    }


    // Called by the new "Back" UI Button
    public void ReturnToCategorySelect()
    {
        // Go back to the previous phase!
        ChangeState(BattleState.CategorySelect);
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
        if (categorySelectPanel != null) categorySelectPanel.SetActive(true);
        if (timerPanel != null) timerPanel.SetActive(false); // Turn off the timer UI!
        
        // The game will now just sit here forever until the player clicks a button.
        yield break; 
    }

    public void OnCategorySelected(string category)
    {
        Debug.Log("Selected: " + category);
        ChangeState(BattleState.SpellSelect);
    }

    IEnumerator HandleSpellSelection()
    {
        if (spellSelectPanel != null) spellSelectPanel.SetActive(true);
        if (timerPanel != null) timerPanel.SetActive(false); // Turn off the timer UI!
        
        // Wait forever for a button click!
        yield break; 
    }

    public void OnSpellSelected(string spellName)
    {
        Debug.Log("Spell chosen: " + spellName);
        ChangeState(BattleState.QuestIntro);
    }

    IEnumerator HandleQuestIntro()
    {
        
        introBanner.SetActive(true);
        bannerText.text = "QUEST: Word starting with '" + currentQuest.targetLetter + "'";
        yield return StartCoroutine(WaitOrSkip(3f));

        
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
        if (suggestionPanel != null) suggestionPanel.SetActive(false);

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

                // ==========================================
                // CLUTCH SUGGESTION FEATURE
                // ==========================================
                if (dictionaryManager != null && suggestionPanel != null)
                {
                    // 1. Get the currently blocked words so we don't suggest a banned word!
                    List<string> activeBlockedWords = new List<string>();
                    if (usedWordsPerPrefix.ContainsKey(currentQuest.targetLetter))
                    {
                        int limit = GetDynamicMemoryLimit(currentQuest.tier2Rule);
                        var history = usedWordsPerPrefix[currentQuest.targetLetter];
                        activeBlockedWords = history.Skip(Mathf.Max(0, history.Count - limit)).ToList();
                    }

                    // 2. Ask the dictionary for 2 short words
                    List<string> hints = dictionaryManager.GetClutchSuggestions(currentQuest.targetLetter, currentQuest.tier2Rule, activeBlockedWords, 2);
                    
                    // 3. Display them!
                    if (hints.Count > 0)
                    {
                        suggestionPanel.SetActive(true);
                        suggestionText.text = "CLUTCH HINT: Try '" + string.Join("' or '", hints) + "'";
                    }
                }

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
                yield break; 
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

        lastWordLength = playerWord.Length; // Save the length for Focus scaling!

        // --- POTENCY CALCULATION ---
        
        // 1. Spell Base Multiplier
        float spellMultiplier = 1.0f;
        switch(activeSpell)
        {
            case SpellType.FlameBlast: spellMultiplier = 1.2f; break;
            case SpellType.FrostSpikes: spellMultiplier = 1.3f; break;
            case SpellType.ThunderStrike: spellMultiplier = 2.0f; break;
            case SpellType.GaleBurst: spellMultiplier = 1.0f; break;
            case SpellType.EarthThrow: spellMultiplier = 1.5f; break;
            case SpellType.ArcaneBolts: spellMultiplier = 0.75f; break;
            
            case SpellType.WindVeil: spellMultiplier = 0.75f; break;
            case SpellType.FlameBarrier: spellMultiplier = 0.5f; break;
            case SpellType.ManaShield: spellMultiplier = 1.5f; break;
            case SpellType.Restraint: spellMultiplier = 1.0f; break;
            
            case SpellType.Revitalize: spellMultiplier = 0.5f; break;
            case SpellType.Cleanse: spellMultiplier = 0.25f; break;
            case SpellType.PurifyingFlames: spellMultiplier = 1.0f; break; 
            case SpellType.WinterEmbrace: spellMultiplier = 0.8f; break;
            case SpellType.SoothingWaters: spellMultiplier = 0.5f; break;
        }

        // 2. GOAL 1: Flat Potency Bonus Based on Category & Word Length!
        int len = playerWord.Length;
        float flatBonus = 0f;
        
        bool isOffensive = (activeSpell == SpellType.FlameBlast || activeSpell == SpellType.FrostSpikes || activeSpell == SpellType.ThunderStrike || activeSpell == SpellType.GaleBurst || activeSpell == SpellType.EarthThrow || activeSpell == SpellType.ArcaneBolts);
        bool isUtility = (activeSpell == SpellType.WindVeil || activeSpell == SpellType.FlameBarrier || activeSpell == SpellType.ManaShield || activeSpell == SpellType.Restraint || activeSpell == SpellType.Focus);
        bool isHealing = (activeSpell == SpellType.Revitalize || activeSpell == SpellType.Cleanse || activeSpell == SpellType.PurifyingFlames || activeSpell == SpellType.WinterEmbrace || activeSpell == SpellType.SoothingWaters);

        // e.g. 5 letter Offensive word = 5 * 2 = +10 Potency to the base stat!
        if (isOffensive && len >= 3) flatBonus = len * 2f;
        else if (isUtility && len >= 4) flatBonus = len * 3f;
        else if (isHealing && len >= 5) flatBonus = len * 4f;

        currentCastBasePotency = baseSpellPotency + flatBonus; // Save this for HandleResolution!

        // 3. GOAL 2: Exact Length Constraint Multipliers
        float constraintMultiplier = 1.0f;
        switch(currentQuest.tier2Rule)
        {
            case Tier2Type.ExactLength_4: constraintMultiplier = 1.30f; break; // +30%
            case Tier2Type.ExactLength_5: constraintMultiplier = 1.35f; break; // +35%
            case Tier2Type.ExactLength_6: constraintMultiplier = 1.40f; break; // +40%
            case Tier2Type.ExactLength_7: constraintMultiplier = 1.45f; break; // +45%
            case Tier2Type.ExactLength_8: constraintMultiplier = 1.50f; break; // +50%
            case Tier2Type.ExactLength_9: constraintMultiplier = 1.60f; break; // +60%
        }

        float finalPotency = spellMultiplier * constraintMultiplier;

        // 4. Overtime Penalty
        if (isOvertime)
        {
            Debug.Log("<color=orange>Overtime Cast! Suffered a time penalty!</color>");
            float timeUsed = maxOvertime - currentOvertime; 
            float overtimePercentage = timeUsed / maxOvertime; 
            float penalty = overtimePercentage * maxPenaltyPercent;
            
            finalPotency *= (1.0f - penalty); 
        }

        // 5. Consume Focus Buff
        if (isOffensive && focusActive)
        {
            finalPotency *= (1.0f + focusDamageBonus);
            Debug.Log($"<color=yellow>FOCUS CONSUMED! Damage increased by {focusDamageBonus * 100}%!</color>");
            focusActive = false; 
        }

        currentSpellPotency = finalPotency;

        // ==========================================
        // THESIS GOAL 3: WORD DISCOVERY BONUS
        // ==========================================
        if (dictionaryManager != null && !isOvertime) // (Optional: Don't give bonus if they are in overtime!)
        {
            float discoveryBonus = dictionaryManager.RegisterWordAndGetBonus(playerWord);
            if (discoveryBonus > 1.0f)
            {
                Debug.Log($"<color=yellow>✨ DISCOVERY BONUS! '{playerWord}' grants 1.20x Damage! ✨</color>");
                currentSpellPotency *= discoveryBonus;
            }
        }

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
        // 1. Put the cast spell on cooldown immediately!
        spellCooldowns[activeSpell] = GetMaxCooldown(activeSpell);

        // 2. Is it an Offensive Spell?
        bool isOffensive = (activeSpell == SpellType.FlameBlast || activeSpell == SpellType.FrostSpikes || 
                            activeSpell == SpellType.ThunderStrike || activeSpell == SpellType.GaleBurst || 
                            activeSpell == SpellType.EarthThrow || activeSpell == SpellType.ArcaneBolts);

        if (isOffensive)
        {
            // OFFENSIVE MATH: Deal Damage!
            // NEW: Use the dynamic base potency that includes the word length bonus!
            float damageDealt = currentCastBasePotency * currentSpellPotency;
            if (isLesserSpell) damageDealt *= 0.5f; 

            enemyCurrentHP -= damageDealt;
            if (enemyCurrentHP < 0) enemyCurrentHP = 0;
            
            UpdateHealthUI();
            Debug.Log($"<color=cyan>Offensive Spell: {activeSpell} | Power: {currentSpellPotency}x | Damage: {damageDealt} | Serpent HP: {enemyCurrentHP}</color>");
        }
        else
        {
            // UTILITY & HEALING MATH: Do not deal damage!
            Debug.Log($"<color=cyan>Cast Non-Offensive Spell: {activeSpell} at {currentSpellPotency}x Potency!</color>");
            
            // --- THESIS MECHANIC: SHIELDS ---
            if (activeSpell == SpellType.WindVeil || activeSpell == SpellType.FlameBarrier || activeSpell == SpellType.ManaShield)
            {
                // NEW: Shields now scale with the flat length bonus!
                float shieldAmount = currentCastBasePotency * currentSpellPotency;
                playerCurrentShield += shieldAmount; // Stack the shield!
                Debug.Log($"<color=cyan>Shield Applied! Amy gains {shieldAmount} Shield. Total Shield: {playerCurrentShield}</color>");
            }

            // --- THESIS MECHANIC: RESTRAINT ---
            if (activeSpell == SpellType.Restraint)
            {
                Debug.Log("<color=yellow>Restraint applied! Serpent skips a turn and is weakened by 40%.</color>");
                enemyTurnSkipCount = 1; 
                enemyDamageMultiplier = 0.6f; 
            }

            // --- THESIS MECHANIC: FOCUS ---
            if (activeSpell == SpellType.Focus)
            {
                float bonus = 0.50f; // Base 50% for 4 letters
                if (lastWordLength == 5) bonus = 0.55f;
                else if (lastWordLength == 6) bonus = 0.60f;
                else if (lastWordLength == 7) bonus = 0.65f;
                else if (lastWordLength >= 8) bonus = 0.75f; // Max 75% for 8+ letters

                focusActive = true;
                focusDamageBonus = bonus;

                // Reduce all active CDs to 1!
                List<SpellType> keys = new List<SpellType>(spellCooldowns.Keys);
                foreach (SpellType key in keys)
                {
                    if (spellCooldowns[key] > 1) spellCooldowns[key] = 1;
                }

                Debug.Log($"<color=yellow>FOCUS CAST! All CDs set to 1. Next attack gains +{bonus * 100}% Potency!</color>");
            }

            UpdateHealthUI(); // Update the visual bars instantly!
        }

        // 3. Pause so the player can watch the 3D scene
        yield return new WaitForSeconds(3.5f); 

        // Check for Boss Death
        if (enemyCurrentHP <= 0)
        {
            Debug.Log("<color=green>Enemy Defeated! A new challenger appears!</color>");
            enemiesDefeatedCount++;
            
            enemyCurrentHP = enemyMaxHP; 
            UpdateHealthUI();
            
            ChangeState(BattleState.QuestIntro);
            yield break; 
        }
        
        ChangeState(BattleState.EnemyTurn);
    }

    IEnumerator HandleEnemyTurn()
    {
        // --- CHECK STATUS EFFECTS FIRST ---
        if (enemyTurnSkipCount > 0)
        {
            Debug.Log("<color=cyan>The Wind Serpent is Restrained and cannot attack this turn!</color>");
            enemyTurnSkipCount--; // Decrease the skip counter
            yield return new WaitForSeconds(2.0f); // Pause so the player can read it!
            
            // Loop back to Amy's turn immediately!
            currentTurn++;
            ChangeState(BattleState.Intro);
            yield break;
        }

        // --- NORMAL ENEMY ATTACK ---
        Debug.Log("<color=red>Wind Serpent attacks!</color>");
        
        // Calculate damage taking any active weakness multipliers into account
        float finalEnemyDamage = enemyBaseDamage * enemyDamageMultiplier;
        enemyDamageMultiplier = 1.0f; // Reset weakness immediately

        // 1. SHIELD ABSORPTION MATH
        if (playerCurrentShield > 0)
        {
            if (playerCurrentShield >= finalEnemyDamage)
            {
                // The shield survives the hit!
                playerCurrentShield -= finalEnemyDamage;
                Debug.Log($"<color=cyan>Shield absorbed all {finalEnemyDamage} damage! Shield remaining: {playerCurrentShield}</color>");
                finalEnemyDamage = 0; // No damage left to hit Amy
            }
            else
            {
                // The shield shatters, and the leftover damage bleeds through!
                Debug.Log($"<color=cyan>Shield absorbed {playerCurrentShield} damage and shattered!</color>");
                finalEnemyDamage -= playerCurrentShield;
                playerCurrentShield = 0; // Shield is gone
            }
        }

        // 2. APPLY LEFTOVER DAMAGE TO AMY'S HP
        if (finalEnemyDamage > 0)
        {
            playerCurrentHP -= finalEnemyDamage;
            if (playerCurrentHP < 0) playerCurrentHP = 0;
            Debug.Log($"<color=orange>Serpent hits Amy for {finalEnemyDamage} damage! Amy HP: {playerCurrentHP}</color>");
        }

        UpdateHealthUI(); // Update the visual yellow and white bars!

        yield return new WaitForSeconds(3.0f);

        if (playerCurrentHP <= 0)
        {
            Debug.Log("<color=red>Amy has fallen...</color>");
            ChangeState(BattleState.GameOver);
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

    private void UpdateHealthUI()
    {
        // 1. Update Boss Health Bar
        if (enemyHealthBar != null) enemyHealthBar.fillAmount = enemyCurrentHP / enemyMaxHP;

        // 2. Update Amy's Health & Shield Bars
        if (playerHealthBar != null && playerShieldBar != null) 
        {
            float hpPercent = playerCurrentHP / playerMaxHP;
            float shieldPercent = playerCurrentShield / playerMaxHP;

            if (hpPercent + shieldPercent > 1.0f)
            {
                playerShieldBar.fillAmount = 1.0f; 
                float overflowAmount = (hpPercent + shieldPercent) - 1.0f;
                playerHealthBar.fillAmount = Mathf.Clamp01(hpPercent - overflowAmount); 
            }
            else
            {
                playerShieldBar.fillAmount = hpPercent + shieldPercent;
                playerHealthBar.fillAmount = hpPercent;
            }
        }

        // 3. NEW: UPDATE THE HP NUMBERS!
        if (enemyHPText != null)
        {
            // CeilToInt prevents weird decimals like "24.6 / 300"
            enemyHPText.text = Mathf.CeilToInt(enemyCurrentHP).ToString() + " / " + enemyMaxHP.ToString();
        }

        if (playerHPText != null)
        {
            string hpString = Mathf.CeilToInt(playerCurrentHP).ToString() + " / " + playerMaxHP.ToString();
            
            // If they have a shield, append it to the text! (e.g., "100 / 100 (+15)")
            if (playerCurrentShield > 0)
            {
                hpString += " (+" + Mathf.CeilToInt(playerCurrentShield).ToString() + ")";
            }
            
            playerHPText.text = hpString;
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

    private int GetMaxCooldown(SpellType spell)
    {
        switch(spell) {
            case SpellType.ArcaneBolts: return 0;
            case SpellType.GaleBurst: return 1;
            case SpellType.FlameBlast: return 2;
            case SpellType.FrostSpikes: return 2;
            case SpellType.EarthThrow: return 3;
            case SpellType.FlameBarrier: return 2;
            case SpellType.WindVeil: return 3;
            case SpellType.SoothingWaters: return 3;
            case SpellType.Cleanse: return 3;
            case SpellType.ManaShield: return 4;
            case SpellType.PurifyingFlames: return 4;
            case SpellType.ThunderStrike: return 5;
            case SpellType.Revitalize: return 5;
            case SpellType.Restraint: return 6;
            case SpellType.WinterEmbrace: return 6;
            case SpellType.Focus: return 7;
            default: return 0;
        }
    }
}