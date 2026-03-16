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
    // PATCH: Excluded X, Q, V, Z from the pool!
    private string allowedAlphabet = "ABCDEFGHIJKLMNOPRSTUWY";

    [Header("Ink System")]
    public int maxInk = 15;
    public int currentInk;
    public TextMeshProUGUI totalInkTextUI;

    [Header("Battle Targets")]
    public player playerTarget;
    public Enemy enemyTarget;

    [Header("Timer System")]
    public Slider timerSlider;
    public float maxTurnTime = 60f;
    private float currentTimer;
    private bool isTimerRunning = false;
    private bool hasStartedTurnTimer = false; // PATCH: Planning Phase Tracker

    [Header("Grimoire System")]
    public int grimoireCooldown = 5;
    private Queue<string> recentWords = new Queue<string>();
    private HashSet<string> discoveredWords = new HashSet<string>();

    [Header("Discard & Shuffle System")]
    public int discardCooldownTurns = 0;
    private bool shouldDoubleDrawNextTurn = false;
    public TextMeshProUGUI discardCooldownTextUI;

    [Header("Reroll System")]
    public int currentRerollCost = 1; // Starts at 1 Ink
    public TextMeshProUGUI rerollButtonTextUI; // PATCH: UI hook for the button text

    void Start()
    {
        if (spellInputPanel != null) spellInputPanel.SetActive(false);
        if (timerSlider != null) timerSlider.gameObject.SetActive(false);

        currentInk = 4;
        discardCooldownTurns = 0;
        UpdateInkUI();
        UpdateDiscardUI();
        UpdateRerollUI();

        for (int i = 0; i < startingHandSize; i++)
        {
            DrawNewCard();
        }
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

    // PATCH: Called by CardInteraction when a card is clicked for the first time
    public void NotifyCardClicked()
    {
        if (!hasStartedTurnTimer)
        {
            StartTurnTimer();
            hasStartedTurnTimer = true;
            Debug.Log("<color=yellow>Planning Phase over! 60-second timer started.</color>");
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
            string submittedWord = wordInputField.text;

            if (submittedWord.Length <= 2)
            {
                Debug.LogWarning("Word is too short! Spells require at least 3 letters.");
                NotificationManager.instance.ShowMessage("Word too short! Need 3+ letters.");
                return;
            }

            int costOfSpell = CardInteraction.currentlyPlayedCard.inkCost;
            if (currentInk < costOfSpell)
            {
                Debug.LogWarning("Not enough Ink to cast this spell!");
                NotificationManager.instance.ShowMessage("Not enough Ink to cast this spell!");
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

            // Replace your old Debug.Log with this:
            string discoveryLog = isNewDiscovery ? " (New Discovery!)" : "";
            NotificationManager.instance.ShowMessage($"Cast {spellName}! {finalPower} Power!{discoveryLog}");
            

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

            Destroy(CardInteraction.currentlyPlayedCard.gameObject);
            CardInteraction.currentlyPlayedCard = null;

            wordInputField.text = "";
            if (spellInputPanel != null)
            {
                spellInputPanel.SetActive(false);
            }
            NotificationManager.instance.Invoke("HideMessage", 2.0f);
        }
    }

    public void DrawNewCard()
    {
        List<char> availableLetters = new List<char>(allowedAlphabet.ToCharArray());

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
            availableLetters = new List<char>(allowedAlphabet.ToCharArray());
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

    public void EndTurn()
    {
        Debug.Log("Player ended their turn!");

        StopTimer();
        hasStartedTurnTimer = false; // Reset the planning phase for next turn
        currentRerollCost = 1; // PATCH: Reset the escalating reroll cost back to 1
        UpdateRerollUI();

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
                if (card.lockedTurnsLeft > 0) card.DecreaseLock();
                survivingCards++;
            }
        }

        if (currentInk == 0) { currentInk += 5; NotificationManager.instance.ShowMessage("Gained 5 Ink!"); }
        else if (currentInk >= 8) { currentInk += 3; NotificationManager.instance.ShowMessage("Gained 3 Ink!"); }
        else { currentInk += 4; NotificationManager.instance.ShowMessage("Gained 4 Ink!"); }
        if (currentInk > maxInk) currentInk = maxInk;
        UpdateInkUI();

        int cardsToDraw = 0;

        if (survivingCards <= 1) cardsToDraw = 2;
        else cardsToDraw = 1;

        // PATCH: Double Draw Mechanic Check
        if (shouldDoubleDrawNextTurn)
        {
            cardsToDraw *= 2;
            shouldDoubleDrawNextTurn = false;
            Debug.Log("<color=cyan>Double Draw Active!</color>");
            NotificationManager.instance.ShowMessage("Double Draw Activated!");
        }

        if (survivingCards + cardsToDraw > maxHandSize)
        {
            cardsToDraw = maxHandSize - survivingCards;
        }

        for (int i = 0; i < cardsToDraw; i++)
        {
            DrawNewCard();
        }

        // PATCH: Reduce discard cooldown
        if (discardCooldownTurns > 0) discardCooldownTurns--;
        UpdateDiscardUI();

        if (enemyTarget != null) enemyTarget.TakeTurn();
        if (playerTarget != null) playerTarget.HandleStartOfTurn();
    }

    public void DiscardSelectedCard()
    {
        if (CardInteraction.currentlyPlayedCard != null)
        {
            if (discardCooldownTurns > 0)
            {
                Debug.LogWarning($"Discard on cooldown! Wait {discardCooldownTurns} more turn(s).");
                NotificationManager.instance.ShowMessage($"Discard on cooldown! Wait {discardCooldownTurns} more turn(s).");
                return;
            }

            NotifyCardClicked();
            CardInteraction cardToDiscard = CardInteraction.currentlyPlayedCard;

            if (cardToDiscard.inkCost == 1)
            {
                shouldDoubleDrawNextTurn = true;
                Debug.Log("<color=orange>Discarded 1-Ink card: Double Draw activated for next turn!</color>");
            }
            else
            {
                int refund = Mathf.FloorToInt(cardToDiscard.inkCost * 0.5f);
                currentInk += refund;
                if (currentInk > maxInk) currentInk = maxInk;
                UpdateInkUI();
                NotificationManager.instance.ShowMessage($"Discard Refund: +{refund} Ink!");
                Debug.Log($"<color=orange>Discarded {cardToDiscard.inkCost}-Ink card: Refunded {refund} Ink.</color>");
            }

            discardCooldownTurns = 2;
            UpdateDiscardUI();

            Destroy(cardToDiscard.gameObject);
            CardInteraction.currentlyPlayedCard = null;

            if (spellInputPanel != null) spellInputPanel.SetActive(false);
            wordInputField.text = "";
            NotificationManager.instance.Invoke("HideMessage", 1.0f);
        }
    }

    public void ShuffleLetter()
    {
        if (CardInteraction.currentlyPlayedCard != null)
        {
            // 1. Check if the player has enough Ink
            if (currentInk < currentRerollCost)
            {
                Debug.LogWarning($"Not enough Ink to reroll! You need {currentRerollCost} Ink.");
                NotificationManager.instance.ShowMessage($"Not enough Ink! Need {currentRerollCost}.");
                return;
            }

            NotifyCardClicked(); // Ensures timer starts

            // 2. Deduct the Ink and update the UI
            currentInk -= currentRerollCost;
            UpdateInkUI();

            Debug.Log($"<color=green>Rerolled letter for {currentRerollCost} Ink!</color>");

            // 3. Increment the cost for the next use, capping it at 3
            if (currentRerollCost < 3)
            {
                currentRerollCost++;
            }
            UpdateRerollUI();

            // 4. Perform the letter swap
            List<char> availableLetters = new List<char>(allowedAlphabet.ToCharArray());

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
                availableLetters = new List<char>(allowedAlphabet.ToCharArray());
            }

            char newLetter = availableLetters[Random.Range(0, availableLetters.Count)];

            CardInteraction.currentlyPlayedCard.ChangeLetter(newLetter);

            wordInputField.text = newLetter.ToString();
            wordInputField.MoveTextEnd(false);
        }
    }

    public void UpdateDiscardUI()
    {
        if (discardCooldownTextUI != null)
        {
            if (discardCooldownTurns > 0)
            {
                discardCooldownTextUI.text = $"Cooldown ({discardCooldownTurns})";
                discardCooldownTextUI.color = new Color(0.8f, 0.4f, 0.4f); // A soft red
            }
            else
            {
                discardCooldownTextUI.text = "Discard";
                //discardCooldownTextUI.color = Color.white;
            }
        }
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

    public void UpdateRerollUI()
    {
        if (rerollButtonTextUI != null)
        {
            rerollButtonTextUI.text = $"Reroll ({currentRerollCost})";
        }
    }
}