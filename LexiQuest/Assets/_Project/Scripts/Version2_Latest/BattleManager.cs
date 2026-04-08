using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum BattleState { MainMenu, Intro, CategorySelect, SpellSelect, QuestIntro, Typing, Resolution, EnemyTurn, GameOver, ScribesReview }

public class BattleManager : MonoBehaviour
{
    [Header("Combat Stats (Prototype)")]
    public GameObject healthUIPanel; // NEW: Container for all HP Bars and Texts
    public float playerMaxHP = 100f;
    private float playerCurrentHP;
    public Image playerHealthBar; // Drag the Yellow Bar Image here
    public TextMeshProUGUI playerHPText; // NEW: The text showing Amy's HP

    // --- NEW SHIELD VARIABLES ---
    public float playerCurrentShield = 0f;
    public Image playerShieldBar; // Drag the White Bar Image here!

    public float enemyMaxHP = 100f; // Boss Tier Health!
    private float enemyCurrentHP;
    public Image enemyHealthBar; // Drag the Red Bar Image here
    public TextMeshProUGUI enemyHPText; // NEW: The text showing the Boss's HP

    public float baseSpellPotency = 10f; // REDUCED TO 10
    private float currentCastBasePotency; // Holds Base + Flat Length Bonus!
    public float enemyBaseDamage = 15f;
    private float currentSpellPotency = 1.0f;
    
    public int questStreak = 0; // NEW: Tracks consecutive double-quest completions!

    [Header("Cinematic UI Toggles")]
    public GameObject mainUICanvas; // Drag the "Canvas" inside LexiQuestUIV2 here

    [Header("Endless Mode UI")]
    public GameObject mainMenuPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI scoreText;
    private int enemiesDefeatedCount = 0;

    [Header("Scribe's Review")]
    public GameObject scribesReviewPanel; // NEW: The panel for the Scribe's Review
    public List<string> scribesReviewWords = new List<string>(); // Tracks all words!
    public TextMeshProUGUI scribesReviewText; // Drag a UI Text here to show the list on Game Over!

    [Header("Floating Combat Text")]
    public GameObject floatingTextPrefab; // Drag your new FCT Prefab here
    public Transform playerFloatingTextSpawn; // Drag an empty GameObject positioned above Amy here
    public Transform enemyFloatingTextSpawn; // Drag an empty GameObject positioned above the Boss here

    [Header("Enemy AI Cooldowns")]
    public int enemyHeavyAttackCD = 0;
    public int enemyHealCD = 0;

    [Header("Phase 1 & 2 Spell UI")]
    public GameObject categorySelectPanel;
    public GameObject spellSelectPanel;
    public Button[] spellButtons; // NEW: The actual Button components so we can lock them!
    public TextMeshProUGUI[] spellButtonTexts; 
    
    private SpellType activeSpell;
    private SpellType[] currentlyDisplayedSpells = new SpellType[6]; // Expanded to 6!

    public Dictionary<SpellType, int> spellCooldowns = new Dictionary<SpellType, int>();

    [Header("Status Memory")]
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

    [Header("Reroll Feature")]
    public Button rerollButton;
    private bool hasUsedReroll = false;
    private bool isWaitingForReroll = false;
    private bool wantsToReroll = false;

    [Header("Quest UI Text")]
    public TextMeshProUGUI bannerText; // The text on your IntroBanner
    public TextMeshProUGUI tier1StatusText; // Persistent "Starts with..." text
    public TextMeshProUGUI tier2StatusText; // Persistent "Length..." text
    public TextMeshProUGUI tier3StatusText; // Persistent "Side Quest..." text
    public TextMeshProUGUI streakText;

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

    [Header("Status Effects (Enemy)")]
    public int enemyTurnSkipCount = 0;
    public float enemyDamageMultiplier = 1.0f; // Electrified / Restraint
    public float enemyVulnerability = 1.0f;    // Frostbite
    public float enemyBurnAmount = 0f;
    public int enemyBurnTurns = 0;
    public TextMeshProUGUI enemyStatusText; // NEW: Drag a Text component here for the UI!

    [Header("Player Status Effects")]
    public float playerHoTAmount = 0f;       
    public int playerHoTTurns = 0;           
    public float playerHoTAmount2 = 0f;      // NEW: Second stack for Lesser Heal
    public int playerHoTTurns2 = 0;          // NEW: Second stack for Lesser Heal
    public int incomingHealBonusTurns = 0;   
    public bool isDamageImmune = false;      
    public int playerBleedTurns = 0;
    public float playerBleedAmount = 0f;
    public int playerDebuffCount = 0;        
    public int playerDebuffImmunityTurns = 0; // For the new Purify!
    public float playerDamageReductionPercent = 0f; // For WindBlast!
    public int playerDamageReductionTurns = 0;
    public float flameBarrierBurnAmount = 0f; 
    public int flameBarrierTurns = 0;
    public float playerDelayedShieldAmount = 0f; // NEW: Lesser Shield Next Turn
    public int playerDelayedShieldTurns = 0;
    public TextMeshProUGUI playerStatusText; // NEW: Drag a Text component here for the UI!

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
        if (scribesReviewPanel != null) scribesReviewPanel.SetActive(false);

        // Hide Health UI in non-combat states and during Typing
        bool showHealth = (currentState != BattleState.MainMenu && currentState != BattleState.GameOver && currentState != BattleState.ScribesReview && currentState != BattleState.Typing);
        
        if (healthUIPanel != null)
        {
            healthUIPanel.SetActive(showHealth);
        }
        
        // Enforce visibility for individual components in case they are placed outside the healthUIPanel
        if (playerHPText != null) playerHPText.gameObject.SetActive(showHealth);
        if (enemyHPText != null) enemyHPText.gameObject.SetActive(showHealth);
        if (playerShieldBar != null) playerShieldBar.gameObject.SetActive(showHealth);
        if (playerStatusText != null) playerStatusText.gameObject.SetActive(showHealth);
        if (enemyStatusText != null) enemyStatusText.gameObject.SetActive(showHealth);

        if (healthUIPanel == null)
        {
            if (playerHealthBar != null) 
            {
                if (playerHealthBar.transform.parent != null && playerHealthBar.transform.parent.GetComponent<Canvas>() == null) playerHealthBar.transform.parent.gameObject.SetActive(showHealth);
                else playerHealthBar.gameObject.SetActive(showHealth);
            }
            if (enemyHealthBar != null) 
            {
                if (enemyHealthBar.transform.parent != null && enemyHealthBar.transform.parent.GetComponent<Canvas>() == null) enemyHealthBar.transform.parent.gameObject.SetActive(showHealth);
                else enemyHealthBar.gameObject.SetActive(showHealth);
            }
        }

        // Hide Quest Status texts in phases where no quest is active yet (like Category & Spell selection)
        bool showQuestStatus = (currentState == BattleState.QuestIntro || currentState == BattleState.Typing || currentState == BattleState.Resolution || currentState == BattleState.EnemyTurn);
        if (!showQuestStatus)
        {
            if (tier1StatusText != null && tier1StatusText.transform.parent != null) tier1StatusText.transform.parent.gameObject.SetActive(false);
            else if (tier1StatusText != null) tier1StatusText.gameObject.SetActive(false);
            
            if (tier2StatusText != null && tier2StatusText.transform.parent != null) tier2StatusText.transform.parent.gameObject.SetActive(false);
            else if (tier2StatusText != null) tier2StatusText.gameObject.SetActive(false);
            
            if (tier3StatusText != null && tier3StatusText.transform.parent != null) tier3StatusText.transform.parent.gameObject.SetActive(false);
            else if (tier3StatusText != null) tier3StatusText.gameObject.SetActive(false);
            
            if (streakText != null) streakText.gameObject.SetActive(false);
        }

        switch (currentState)
        {
            case BattleState.MainMenu:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                break;
            case BattleState.Intro:
                // --- REDUCE COOLDOWNS ---
                List<SpellType> keys = new List<SpellType>(spellCooldowns.Keys);
                foreach (SpellType key in keys)
                {
                    if (spellCooldowns[key] > 0) spellCooldowns[key]--;
                }

                // --- PROCESS HOTS (Heal Over Time) ---
                float totalTickHeal = 0f;
                int maxHoTTurns = 0;

                if (playerHoTTurns > 0)
                {
                    totalTickHeal += playerHoTAmount;
                    playerHoTTurns--;
                    if (playerHoTTurns > maxHoTTurns) maxHoTTurns = playerHoTTurns;
                }

                if (playerHoTTurns2 > 0)
                {
                    totalTickHeal += playerHoTAmount2;
                    playerHoTTurns2--;
                    if (playerHoTTurns2 > maxHoTTurns) maxHoTTurns = playerHoTTurns2;
                }

                if (totalTickHeal > 0)
                {
                    float tickHeal = totalTickHeal;
                    if (incomingHealBonusTurns > 0) tickHeal *= 1.5f; // Revitalize bonus

                    playerCurrentHP += tickHeal;
                    if (playerCurrentHP > playerMaxHP) playerCurrentHP = playerMaxHP;
                    
                    UpdateHealthUI();
                    Debug.Log($"<color=green>HoT Tick! Amy healed {tickHeal} HP. {maxHoTTurns} turns of HoT remaining.</color>");
                    ShowFloatingText($"+{Mathf.RoundToInt(tickHeal)}", playerFloatingTextSpawn, Color.green);
                }

                // --- PROCESS DELAYED SHIELD ---
                if (playerDelayedShieldTurns > 0)
                {
                    if (playerDelayedShieldAmount >= playerCurrentShield) {
                        playerCurrentShield = playerDelayedShieldAmount; 
                    } else {
                        playerCurrentShield += (playerDelayedShieldAmount * 0.20f); 
                    }
                    if (playerCurrentShield > 70f) playerCurrentShield = 70f; 
                    
                    playerDelayedShieldTurns--;
                    UpdateHealthUI();
                    Debug.Log($"<color=cyan>Delayed Shield Tick! Amy gained Shield.</color>");
                    ShowFloatingText($"Shield", playerFloatingTextSpawn, Color.cyan);
                }

        if (playerBleedTurns > 0)
        {
            playerCurrentHP -= playerBleedAmount;
            if (playerCurrentHP < 0) playerCurrentHP = 0;
            
            playerBleedTurns--;
            UpdateHealthUI();
            Debug.Log($"<color=purple>Poison Tick! Amy took {playerBleedAmount} damage. {playerBleedTurns} turns remaining.</color>");
            
            if (playerCurrentHP <= 0)
            {
                ChangeState(BattleState.ScribesReview);
                return;
            }
        }

                if (incomingHealBonusTurns > 0) incomingHealBonusTurns--;
                isDamageImmune = false; // Immunity expires at the start of Amy's next turn!

            StartCoroutine(HandleIntro());
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
                float calculatedTimer = 40f; // Base time for standard constraints
                
                switch(currentQuest.tier2Rule)
                {
                    case Tier2Type.ExactLength_4: calculatedTimer = 50f; break; // 40 + 10
                    case Tier2Type.ExactLength_5: calculatedTimer = 60f; break; // 40 + 20
                    case Tier2Type.ExactLength_6: calculatedTimer = 70f; break; // 40 + 30
                    case Tier2Type.ExactLength_7: calculatedTimer = 80f; break; // 40 + 40
                    case Tier2Type.ExactLength_8: calculatedTimer = 90f; break; // 40 + 50
                    case Tier2Type.ExactLength_9: calculatedTimer = 100f; break; // 40 + 60
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
            case BattleState.ScribesReview:
                if (scribesReviewPanel != null) scribesReviewPanel.SetActive(true);
                
                if (scribesReviewText != null) 
                {
                    scribesReviewText.text = scribesReviewWords.Count > 0 ? string.Join("\n", scribesReviewWords) : "No words cast this run."; 
                }
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
        enemyMaxHP = 100f;
        enemyBaseDamage = 15f;
        baseSpellPotency = 10f;

        playerCurrentHP = playerMaxHP;
        enemyCurrentHP = enemyMaxHP;
        playerCurrentShield = 0f;
        enemiesDefeatedCount = 0;
        enemyTurnSkipCount = 0;
        enemyDamageMultiplier = 1.0f;
        enemyVulnerability = 1.0f;
        enemyBurnTurns = 0;
        enemyBurnAmount = 0f;
        
        playerHoTTurns = 0;
        playerHoTAmount = 0f;
        playerHoTTurns2 = 0;
        playerHoTAmount2 = 0f;
        playerDelayedShieldAmount = 0f;
        playerDelayedShieldTurns = 0;

        scribesReviewWords.Clear();
        enemyHeavyAttackCD = 0;
        enemyHealCD = 0;
        
        spellCooldowns.Clear();

        questStreak = 0;
        if (streakText != null)
        {
            streakText.text = "";
            streakText.gameObject.SetActive(false);
        }
        
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
            pool = new List<SpellType> { SpellType.ArcaneShot, SpellType.WindBlast, SpellType.FireBlast, SpellType.FrostSpikes, SpellType.EarthThrow, SpellType.ThunderStrike };
        else if (category == SpellCategory.Utility)
            pool = new List<SpellType> { SpellType.LesserShield, SpellType.FlameBarrier, SpellType.ManaShield, SpellType.Focus };
        else if (category == SpellCategory.Healing)
            pool = new List<SpellType> { SpellType.LesserHeal, SpellType.Purify, SpellType.GreaterHeal };

        // Loop through all 6 UI buttons
        for (int i = 0; i < 6; i++)
        {
            if (i < pool.Count)
            {
                spellButtons[i].gameObject.SetActive(true);
                currentlyDisplayedSpells[i] = pool[i];
                SpellType s = pool[i];

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

    // Called by the new "Next" UI Button on the Scribe's Review Panel
    public void ProceedToGameOver()
    {
        ChangeState(BattleState.GameOver);
    }

    // --- PHASE LOGIC ---

    IEnumerator HandleIntro()
    {
        introBanner.SetActive(true);
        if (rerollButton != null) rerollButton.gameObject.SetActive(false);
        
        // Actually set the text instead of just commenting it!
        bannerText.text = "YOUR TURN!"; 
        
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
        
        hasUsedReroll = false;
        if (rerollButton != null) 
        {
            rerollButton.gameObject.SetActive(true);
            rerollButton.interactable = true;
        }

        // --- QUEST 1: THE LETTER ---
        yield return StartCoroutine(RollAnimation("QUEST 1: Starts with '", currentQuest.targetLetter, "'"));
        bannerText.text = "QUEST 1: Starts with '" + currentQuest.targetLetter + "'";
        
        yield return StartCoroutine(WaitForRerollWindow());

        if (wantsToReroll)
        {
            wantsToReroll = false;
            hasUsedReroll = true;
            if (rerollButton != null) rerollButton.interactable = false;

            currentQuest.targetLetter = questManager.RerollLetter(currentQuest.targetLetter);
            yield return StartCoroutine(RollAnimation("QUEST 1: Starts with '", currentQuest.targetLetter, "'"));
            bannerText.text = "QUEST 1: Starts with '" + currentQuest.targetLetter + "'";
            yield return StartCoroutine(WaitOrSkip(1.5f)); // Give a brief moment to process the new letter
        }

        // --- QUEST 2: THE RULE ---
        if (currentQuest.tier2Rule != Tier2Type.None)
        {
            // Give a fresh reroll for Q2!
            hasUsedReroll = false;
            if (rerollButton != null) rerollButton.interactable = true;

            yield return StartCoroutine(RollAnimation("QUEST 2: ", currentQuest.tier2Description, ""));
            bannerText.text = "QUEST 2: " + currentQuest.tier2Description;

            yield return StartCoroutine(WaitForRerollWindow());

            if (wantsToReroll)
            {
                wantsToReroll = false;
                hasUsedReroll = true;
                if (rerollButton != null) rerollButton.interactable = false;

                currentQuest.tier2Rule = questManager.RerollTier2Rule(activeSpell, currentQuest.tier2Rule, currentQuest.targetLetter);
                currentQuest.tier2Description = questManager.GetTier2Description(currentQuest.tier2Rule);

                yield return StartCoroutine(RollAnimation("QUEST 2: ", currentQuest.tier2Description, ""));
                bannerText.text = "QUEST 2: " + currentQuest.tier2Description;
                yield return StartCoroutine(WaitOrSkip(1.5f));
            }
        }

        if (rerollButton != null) rerollButton.gameObject.SetActive(false);

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
        if (tier1StatusText != null)
        {
            if (tier1StatusText.transform.parent != null) tier1StatusText.transform.parent.gameObject.SetActive(true);
            tier1StatusText.gameObject.SetActive(true);
            tier1StatusText.text = "Letter: " + currentQuest.targetLetter;
        }

        if (tier2StatusText != null)
        {
            if (tier2StatusText.transform.parent != null) tier2StatusText.transform.parent.gameObject.SetActive(true);
            tier2StatusText.gameObject.SetActive(true);
            tier2StatusText.text = "Rule: " + currentQuest.tier2Description;
        }
        
        // Show Quest Streak if applicable
        if (streakText != null)
        {
            if (questStreak >= 3)
            {
                streakText.gameObject.SetActive(true);
                streakText.text = $"Quest Streak: {questStreak}";
            }
            else
            {
                streakText.text = "";
                streakText.gameObject.SetActive(false);
            }
        }

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

        // ==========================================
        // THESIS MECHANIC: OPTIONAL QUESTS & STREAKS
        // ==========================================
        
        int questsCompleted = 0;
        bool passedQ1 = playerWord.StartsWith(currentQuest.targetLetter);
        bool passedQ2 = QuestValidator.CheckTier2(playerWord, currentQuest.tier2Rule);

        if (passedQ1) questsCompleted++;
        
        // Arcane bolts has no Q2, so it maxes out at 1 quest completed!
        if (passedQ2 && currentQuest.tier2Rule != Tier2Type.None) questsCompleted++;

        // Calculate Streak! (Only triggers if they complete BOTH quests)
        if (questsCompleted == 2)
        {
            questStreak++;
            Debug.Log($"<color=green>Perfect Cast! Quest Streak: {questStreak}</color>");
        }
        else
        {
            questStreak = 0;
            Debug.Log($"<color=orange>Streak Broken! Quests Completed: {questsCompleted}</color>");
        }

        if (streakText != null)
        {
            if (questStreak >= 3) 
            {
                streakText.gameObject.SetActive(true);
                streakText.text = $"Streak: {questStreak} 🔥";
            }
            else 
            {
                streakText.text = "";
                streakText.gameObject.SetActive(false); // Fully hide it from the UI layout!
            }
        }

        // --- NEW POTENCY CALCULATION ---
        
        // 1. Core Spell Multipliers
        float spellMultiplier = 1.0f;
        switch(activeSpell)
        {
            case SpellType.ArcaneShot: spellMultiplier = 0.75f; break;
            case SpellType.WindBlast: spellMultiplier = 1.0f; break;
            case SpellType.FireBlast: spellMultiplier = 1.2f; break;
            case SpellType.FrostSpikes: spellMultiplier = 1.3f; break;
            case SpellType.EarthThrow: spellMultiplier = 1.5f; break; 
            case SpellType.ThunderStrike: spellMultiplier = 2.0f; break;
            
            case SpellType.LesserShield: spellMultiplier = 0.75f; break;
            case SpellType.FlameBarrier: spellMultiplier = 0.5f; break;
            case SpellType.ManaShield: spellMultiplier = 1.5f; break;
            case SpellType.Focus: spellMultiplier = 1.0f; break;
            
            case SpellType.LesserHeal: spellMultiplier = 0.4f; break;
            case SpellType.Purify: spellMultiplier = 0.25f; break;
            case SpellType.GreaterHeal: spellMultiplier = 1.0f; break;
        }

        // 2. Goal 4: Word Length Flat Bonus (+2 per letter starting at 4)
        int len = playerWord.Length;
        float lengthBonus = 0f;
        if (len >= 4)
        {
            lengthBonus = (len - 3) * 2f; // 4 letters = +2, 5 letters = +4, 6 letters = +6...
        }

        // 3. Goal 6: Quest Streak Flat Bonus
        float streakBonus = 0f;
        if (questStreak >= 20) streakBonus = 15f;
        else if (questStreak >= 10) streakBonus = 9f;
        else if (questStreak >= 5) streakBonus = 4f;
        else if (questStreak >= 3) streakBonus = 3f;

        // Save the dynamic base for the Resolution state
        currentCastBasePotency = baseSpellPotency + lengthBonus + streakBonus; 

        // 4. Goal 3: Optional Quest Completion Multiplier
        float questMultiplier = 1.0f;
        if (questsCompleted == 1) questMultiplier = 1.30f;       // +30%
        else if (questsCompleted == 2) questMultiplier = 1.70f;  // +70%

        float finalPotency = spellMultiplier * questMultiplier;

        // 5. Overtime Penalty
        if (isOvertime)
        {
            float timeUsed = maxOvertime - currentOvertime; 
            float overtimePercentage = timeUsed / maxOvertime; 
            float penalty = overtimePercentage * maxPenaltyPercent;
            finalPotency *= (1.0f - penalty); 
        }

        // 6. Focus Buff
        bool isOffensive = (activeSpell == SpellType.FireBlast || activeSpell == SpellType.FrostSpikes || activeSpell == SpellType.ThunderStrike || activeSpell == SpellType.WindBlast || activeSpell == SpellType.EarthThrow || activeSpell == SpellType.ArcaneShot);
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
        if (dictionaryManager != null && !isOvertime) 
        {
            float discoveryBonus = dictionaryManager.RegisterWordAndGetBonus(playerWord);
            if (discoveryBonus > 1.0f)
            {
                // UPDATED: Now says "Potency" instead of "Damage"!
                Debug.Log($"<color=yellow>✨ DISCOVERY BONUS! '{playerWord}' grants 1.20x Potency! ✨</color>");
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

                // SUCCESS! First word accepted. (Tiers are now optional!)
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

                // SUCCESS! Second word accepted. (Tiers are now optional!)
                // NEW: Apply the 200% Damage Multiplier for successfully Double Casting!
                currentSpellPotency *= 2.0f; 

                Debug.Log($"<color=cyan>DOUBLE CAST SUCCESS! Ultimate Spell Activated at {(currentSpellPotency * 100).ToString("F1")}% Power!</color>");
                
                // Save ONLY the second word for the next turn's spam filter
                RecordSuccessfulWord(playerWord, currentQuest.targetLetter); 
                
                // ADD TO SCRIBE'S REVIEW
                if (!scribesReviewWords.Contains(playerWord)) scribesReviewWords.Add(playerWord);
                
                // Reset variables for safety
                isWaitingForSecondWord = false;
                firstDoubleCastWord = "";
                
                isLesserSpell = false; // Normal spell achieved!
                ChangeState(BattleState.Resolution);
                
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

        // 3. SUCCESS! Spell Cast. (Tiers are now optional!)
        Debug.Log($"<color=green>Spell Activated at {(finalPotency * 100).ToString("F1")}% Power.</color>");
        RecordSuccessfulWord(playerWord, currentQuest.targetLetter);
        
        // ADD TO SCRIBE'S REVIEW
        if (!scribesReviewWords.Contains(playerWord)) scribesReviewWords.Add(playerWord);
        
        isLesserSpell = false; // Normal spell achieved!
        ChangeState(BattleState.Resolution);
    }

    IEnumerator HandleResolution()
    {
        // 1. Put the cast spell on cooldown!
        if (activeSpell != SpellType.ArcaneShot)
        {
            spellCooldowns[activeSpell] = 2;
        }

        // 2. Is it an Offensive Spell?
        bool isOffensive = (activeSpell == SpellType.FireBlast || activeSpell == SpellType.FrostSpikes || 
                            activeSpell == SpellType.ThunderStrike || activeSpell == SpellType.WindBlast || 
                            activeSpell == SpellType.EarthThrow || activeSpell == SpellType.ArcaneShot);

        introBanner.SetActive(true); // TURN ON THE BANNER FOR COMBAT TEXT!

        if (isOffensive)
        {
            float damageDealt = currentCastBasePotency * currentSpellPotency;
            
            if (activeSpell == SpellType.ArcaneShot && Random.value <= 0.25f)
            {
                damageDealt *= 1.5f;
                Debug.Log("CRIT!");
            }

            if (isLesserSpell) damageDealt *= 0.5f; 
            damageDealt *= enemyVulnerability; 
            enemyVulnerability = 1.0f; // Consume the Frostbite so it resets AFTER Amy hits!

            // Convert to an integer so the UI looks clean!
            int displayDamage = Mathf.RoundToInt(damageDealt);
            enemyCurrentHP -= displayDamage;
            if (enemyCurrentHP < 0) enemyCurrentHP = 0;
            
            // Format the spell name nicely for the banner!
            string formattedSpellName = string.Concat(activeSpell.ToString().Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
            bannerText.text = $"{formattedSpellName.ToUpper()}!";
            bannerText.color = Color.red;
            
            // Trigger Floating Text above the Enemy!
            ShowFloatingText($"-{displayDamage}", enemyFloatingTextSpawn, Color.red);

            // Apply Status Effects
            if (activeSpell == SpellType.FireBlast)
            {
                enemyBurnAmount = currentCastBasePotency * currentSpellPotency * 0.15f;
                enemyBurnTurns = 3;
            }
            else if (activeSpell == SpellType.FrostSpikes) enemyVulnerability = 1.2f; 
            else if (activeSpell == SpellType.ThunderStrike) enemyDamageMultiplier = 0.8f; 
            else if (activeSpell == SpellType.WindBlast)
            {
                playerDamageReductionPercent = 0.20f; // Wind blast 20%
                playerDamageReductionTurns = 1;
            }

            UpdateHealthUI();
        }
        else
        {
            // UTILITY & HEALING
            int displayHeal = 0;
            
            if (activeSpell == SpellType.LesserShield)
            {
                float shieldAmount = currentCastBasePotency * currentSpellPotency * 0.5f;
                if (shieldAmount >= playerCurrentShield) {
                    playerCurrentShield = shieldAmount; // Replace if greater
                } else {
                    playerCurrentShield += (shieldAmount * 0.20f); // Add 20% if lesser
                }
                
                if (playerCurrentShield > 70f) playerCurrentShield = 70f; // Hard cap at 70!

                playerDelayedShieldAmount = shieldAmount;
                playerDelayedShieldTurns = 1;

                bannerText.text = "LESSER SHIELD ACTIVATED!";
                ShowFloatingText($"Shield", playerFloatingTextSpawn, Color.cyan);
            }
            else if (activeSpell == SpellType.FlameBarrier)
            {
                flameBarrierBurnAmount = currentCastBasePotency * currentSpellPotency * 0.25f;
                flameBarrierTurns = 2;
                bannerText.text = "FLAME BARRIER ACTIVATED!";
            }
            else if (activeSpell == SpellType.ManaShield) 
            {
                // PATCH 2: SHIELD CAP & OVERLAP MATH
                float shieldAmount = currentCastBasePotency * currentSpellPotency * 1.5f;
                
                if (shieldAmount >= playerCurrentShield) {
                    playerCurrentShield = shieldAmount; // Replace if greater
                } else {
                    playerCurrentShield += (shieldAmount * 0.20f); // Add 20% if lesser
                }
                
                if (playerCurrentShield > 70f) playerCurrentShield = 70f; // Hard cap at 70!
                
                bannerText.text = $"SHIELD SECURED!";
                ShowFloatingText($"Shield", playerFloatingTextSpawn, Color.cyan);
            }
            else if (activeSpell == SpellType.LesserHeal)
            {
                float newHoT = currentCastBasePotency * currentSpellPotency;

                // --- IMMEDIATE HEAL ---
                playerCurrentHP += newHoT;
                if (playerCurrentHP > playerMaxHP) playerCurrentHP = playerMaxHP;
                ShowFloatingText($"+{Mathf.RoundToInt(newHoT)} HP", playerFloatingTextSpawn, Color.green);
                Debug.Log($"<color=green>Lesser Heal provides an initial heal of {newHoT}.</color>");

                // --- SETUP HoT FOR FUTURE TURNS ---
                if (playerHoTTurns == 0)
                {
                    playerHoTAmount = newHoT;
                    playerHoTTurns = 4; // 4 remaining turns
                }
                else if (playerHoTTurns2 == 0)
                {
                    playerHoTAmount2 = newHoT;
                    playerHoTTurns2 = 4; // 4 remaining turns
                }
                else
                {
                    // Overwrite the one with the lowest turns left
                    if (playerHoTTurns <= playerHoTTurns2)
                    {
                        playerHoTAmount = newHoT;
                        playerHoTTurns = 4;
                    }
                    else
                    {
                        playerHoTAmount2 = newHoT;
                        playerHoTTurns2 = 4;
                    }
                }

                bannerText.text = "REGEN ACTIVATED FOR 4 TURNS!";
            }
            else if (activeSpell == SpellType.GreaterHeal)
            {
                displayHeal = Mathf.RoundToInt(currentCastBasePotency * currentSpellPotency * 1.0f);
                bannerText.text = "GREATER HEAL ACTIVATED!";
            }
            else if (activeSpell == SpellType.Purify)
            {
                // PATCH 1: PURIFY MATH
                playerDebuffImmunityTurns = 2; 
                playerDebuffCount = 0; // Wipe generic debuffs
                // (If you add Bleed or Poison later, reset those variables to 0 right here!)
                bannerText.text = "PURIFIED! DEBUFF IMMUNITY SECURED.";
                ShowFloatingText($"Cleanse", playerFloatingTextSpawn, Color.green);
            }
            else if (activeSpell == SpellType.Focus)
            {
                // Keep your existing Focus logic here!
                float bonus = 0.50f; // Base 50% for 4 letters
                if (lastWordLength == 5) bonus = 0.55f;
                else if (lastWordLength == 6) bonus = 0.60f;
                else if (lastWordLength == 7) bonus = 0.65f;
                else if (lastWordLength >= 8) bonus = 0.75f; // Max 75% for 8+ letters

                focusActive = true;
                focusDamageBonus = bonus;

                Debug.Log($"<color=yellow>FOCUS CAST! Next attack gains +{bonus * 100}% Potency!</color>");
                
                bannerText.text = "FOCUS! DAMAGE BOOSTED.";
            }

            // Apply the instant heals
            if (displayHeal > 0)
            {
                playerCurrentHP += displayHeal;
                if (playerCurrentHP > playerMaxHP) playerCurrentHP = playerMaxHP;
                
                // Trigger Floating Text above Amy!
                ShowFloatingText($"+{displayHeal} HP", playerFloatingTextSpawn, Color.green);
            }

            UpdateHealthUI(); 
        }

        // 3. Pause so the player can read the banner and watch the scene
        yield return new WaitForSeconds(3.5f); 
        introBanner.SetActive(false); // Hide the banner before moving on
        bannerText.color = Color.black; // Reset text color

        // Check for Boss Death
        if (enemyCurrentHP <= 0)
        {
            Debug.Log("<color=green>Enemy Defeated! A new challenger appears!</color>");
            enemiesDefeatedCount++;
            
            enemyMaxHP += 20f;
            enemyBaseDamage += 3f;
            baseSpellPotency += 1f;

            enemyCurrentHP = enemyMaxHP; 
            
            // Clear enemy debuffs and cooldowns for the new boss
            enemyBurnTurns = 0;
            enemyBurnAmount = 0f;
            enemyVulnerability = 1.0f;
            enemyDamageMultiplier = 1.0f;
            enemyTurnSkipCount = 0;
            enemyHeavyAttackCD = 0;
            enemyHealCD = 0;

            UpdateHealthUI();
            
            ChangeState(BattleState.Intro);
            yield break; 
        }
        
        ChangeState(BattleState.EnemyTurn);
    }

    IEnumerator HandleEnemyTurn()
    {
        // 1. TICK ENEMY STATUS EFFECTS
        if (enemyBurnTurns > 0)
        {
            enemyCurrentHP -= enemyBurnAmount;
            enemyBurnTurns--;
            Debug.Log($"<color=red>Enemy takes {enemyBurnAmount} Burn damage! Turns left: {enemyBurnTurns}</color>");
            if (enemyCurrentHP <= 0) 
            {
                // Died to burn!
                Debug.Log("<color=green>Enemy Defeated by Burn! A new challenger appears!</color>");
                enemiesDefeatedCount++;
                
                enemyMaxHP += 20f;
                enemyBaseDamage += 3f;
                baseSpellPotency += 1f;
                
                enemyCurrentHP = enemyMaxHP;
                UpdateHealthUI();
                ChangeState(BattleState.Intro);
                yield break;
            }
        }

        if (enemyTurnSkipCount > 0)
        {
            Debug.Log("<color=cyan>The Wind Serpent is Restrained and cannot attack this turn!</color>");
            enemyTurnSkipCount--; 
            yield return new WaitForSeconds(2.0f); 
            currentTurn++;
            ChangeState(BattleState.Intro);
            yield break;
        }

        // TICK COOLDOWNS
        if (enemyHeavyAttackCD > 0) enemyHeavyAttackCD--;
        if (enemyHealCD > 0) enemyHealCD--;

        introBanner.SetActive(true); // Turn on the banner for the Boss attack!

        float finalEnemyDamage = 0f;
        
        // 2. WIND SERPENT AI (Cooldown Based!)
        if (enemyHealCD == 0 && enemyCurrentHP < enemyMaxHP * 0.5f) // Will only heal if below 50% HP!
        {
            float healAmount = enemyMaxHP * 0.1f; 
            enemyCurrentHP += healAmount;
            if (enemyCurrentHP > enemyMaxHP) enemyCurrentHP = enemyMaxHP;
            
            enemyHealCD = 5; // Base 3 + 2 increase
            bannerText.text = "BOSS USED MENDING WINDS!";
            bannerText.color = Color.black;
            ShowFloatingText($"+{Mathf.RoundToInt(healAmount)} HP", enemyFloatingTextSpawn, Color.green);
        }
        else if (enemyHeavyAttackCD == 0) 
        {
            finalEnemyDamage = (enemyBaseDamage * 1.5f) * enemyDamageMultiplier;
            enemyHeavyAttackCD = 4; // Base 2 + 2 increase
            bannerText.text = "BOSS USED RAGING TEMPEST!";
            bannerText.color = Color.red;
        }
        else 
        {
            finalEnemyDamage = enemyBaseDamage * enemyDamageMultiplier;
            bannerText.text = "BOSS USED SONIC TAIL.";
            bannerText.color = Color.red;
        }

        // Reset weakness/vuln at the end of its turn
        enemyDamageMultiplier = 1.0f; 
        enemyVulnerability = 1.0f; 

        // 3. APPLY DAMAGE TO AMY
        if (finalEnemyDamage > 0)
        {
            // A. Check Immunity
            if (isDamageImmune)
            {
                Debug.Log("<color=cyan>Winter Embrace deflects the attack! Amy takes 0 damage.</color>");
                finalEnemyDamage = 0;
            }

            // NEW: Apply % Damage Reduction (Lesser Shield / Wind Blast)
            if (playerDamageReductionTurns > 0)
            {
                finalEnemyDamage *= (1.0f - playerDamageReductionPercent);
                playerDamageReductionTurns--;
            }

            // C. Shield & HP Math
            if (finalEnemyDamage > 0)
            {
                if (playerCurrentShield > 0)
                {
                    if (playerCurrentShield >= finalEnemyDamage)
                    {
                        playerCurrentShield -= finalEnemyDamage;
                        finalEnemyDamage = 0; 
                    }
                    else
                    {
                        finalEnemyDamage -= playerCurrentShield;
                        playerCurrentShield = 0; 
                    }
                }

                if (finalEnemyDamage > 0)
                {
                    int displayEnemyDamage = Mathf.RoundToInt(finalEnemyDamage);
                    playerCurrentHP -= displayEnemyDamage;
                    if (playerCurrentHP < 0) playerCurrentHP = 0;
                    
                    bannerText.color = Color.black; // Keep it black!
                    
                    // Spawn Floating Text over Amy!
                    ShowFloatingText($"-{displayEnemyDamage}", playerFloatingTextSpawn, Color.red);
                }
            }

            // D. Flame Barrier Retaliation!
            if (flameBarrierTurns > 0)
            {
                // --- IMMEDIATE BURN ---
                float burnDamage = flameBarrierBurnAmount;
                enemyCurrentHP -= burnDamage;
                ShowFloatingText($"-{Mathf.RoundToInt(burnDamage)} Burn", enemyFloatingTextSpawn, Color.red);
                Debug.Log($"<color=red>Flame Barrier retaliates with an initial burn of {burnDamage}.</color>");

                // --- SETUP DoT FOR FUTURE TURNS ---
                enemyBurnAmount = flameBarrierBurnAmount; // Set the amount for subsequent ticks
                enemyBurnTurns = 1; // 1 remaining turn
                flameBarrierTurns--;
            }
        }

        UpdateHealthUI();
        yield return new WaitForSeconds(3.0f);
        introBanner.SetActive(false);

        // Check for Boss Death from retaliation
        if (enemyCurrentHP <= 0)
        {
            Debug.Log("<color=green>Enemy Defeated by Retaliation! A new challenger appears!</color>");
            enemiesDefeatedCount++;
            
            enemyMaxHP += 20f;
            enemyBaseDamage += 3f;
            baseSpellPotency += 1f;

            enemyCurrentHP = enemyMaxHP; 
            
            // Clear enemy debuffs and cooldowns for the new boss
            enemyBurnTurns = 0;
            enemyBurnAmount = 0f;
            enemyVulnerability = 1.0f;
            enemyDamageMultiplier = 1.0f;
            enemyTurnSkipCount = 0;
            enemyHeavyAttackCD = 0;
            enemyHealCD = 0;

            UpdateHealthUI();
            ChangeState(BattleState.Intro);
            yield break; 
        }

        if (playerCurrentHP <= 0)
        {
            ChangeState(BattleState.ScribesReview);
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

        // Update Status Texts
        if (enemyStatusText != null)
        {
            string eStatus = "";
            if (enemyBurnTurns > 0) eStatus += $"Burn ({enemyBurnTurns}) ";
            if (enemyVulnerability > 1.0f) eStatus += "Frostbite ";
            if (enemyDamageMultiplier < 1.0f) eStatus += "Electrified ";
            enemyStatusText.text = eStatus;
        }

        if (playerStatusText != null)
        {
            string pStatus = "";
            int maxHoT = Mathf.Max(playerHoTTurns, playerHoTTurns2);
            if (maxHoT > 0) pStatus += $"Regen ({maxHoT}) ";
            if (playerBleedTurns > 0) pStatus += $"Poison ({playerBleedTurns}) ";
            if (playerDamageReductionTurns > 0) pStatus += $"WindVeil ({playerDamageReductionTurns}) ";
            if (flameBarrierTurns > 0) pStatus += $"FlameBar ({flameBarrierTurns}) ";
            if (isDamageImmune) pStatus += "Immune! ";
            if (playerDelayedShieldTurns > 0) pStatus += "EchoShield ";
            if (focusActive) pStatus += "Focused! ";
            playerStatusText.text = pStatus;
        }
    }

    private void ShowFloatingText(string msg, Transform spawnPoint, Color color)
    {
        if (floatingTextPrefab && spawnPoint)
        {
            GameObject fct = Instantiate(floatingTextPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);
            fct.GetComponent<FloatingText>().Setup(msg, color);
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

    public void OnRerollClicked()
    {
        if (isWaitingForReroll && !hasUsedReroll)
        {
            wantsToReroll = true;
        }
    }

    private IEnumerator RollAnimation(string prefix, string finalValue, string suffix)
    {
        float rollDuration = 1.5f;
        float elapsed = 0f;
        float tickRate = 0.05f;

        while (elapsed < rollDuration && !isBannerSkipped)
        {
            string randomVal = "";
            if (finalValue.Length <= 2) 
            {
                char randomChar = (char)Random.Range(65, 91);
                randomVal = randomChar.ToString();
            }
            else 
            {
                string[] fakeRules = { "Exactly 5 letters", "Ends in -S", "Does NOT contain 'A'", "6 or more letters", "Ends in -ING" };
                randomVal = fakeRules[Random.Range(0, fakeRules.Length)];
            }

            bannerText.text = prefix + randomVal + suffix;
            
            float tickElapsed = 0f;
            while(tickElapsed < tickRate && !isBannerSkipped)
            {
                tickElapsed += Time.deltaTime;
                yield return null;
            }
            elapsed += tickRate;
        }
        isBannerSkipped = false;
    }

    private IEnumerator WaitForRerollWindow()
    {
        isWaitingForReroll = true;
        wantsToReroll = false;
        
        float waitTime = 10.0f;
        float elapsed = 0f;
        
        while (elapsed < waitTime && !isBannerSkipped && !wantsToReroll)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        isWaitingForReroll = false;
        isBannerSkipped = false;
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