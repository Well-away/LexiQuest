using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;

public class CardInteraction : MonoBehaviour
{
    public static CardInteraction currentlyZoomedCard;
    public static CardInteraction currentlyPlayedCard;

    [Header("Card Data")]
    public TextMeshProUGUI letterTextUI;
    public char currentLetter;
    public TextMeshProUGUI spellNameTextUI;
    public string currentSpell;
    public TextMeshProUGUI inkCostTextUI;
    public int inkCost;

    [Header("UI References")]
    public TMP_InputField wordInputField;
    public GameObject spellInputPanel;

    private int cardState = 0;
    private Vector3 originalScale;
    private int originalIndex;

    public Transform inputDisplayArea;
    private Transform handContainer;
    private Canvas cardCanvas;

    [Header("Status Effects")]
    public int lockedTurnsLeft = 0;

    void Start()
    {
        originalScale = transform.localScale;
        handContainer = transform.parent;
        cardCanvas = GetComponent<Canvas>();
    }

    public void InitializeCardData(char assignedLetter, string assignedSpell = "")
    {
        currentLetter = assignedLetter;
        if (letterTextUI != null) letterTextUI.text = currentLetter.ToString();

        List<string> offensiveSpells = new List<string> { "Fireball", "Ice Shards", "Wind Blades" };
        List<string> nonOffensiveSpells = new List<string> { "Bubble Shield", "Revitalize" };

        if (!string.IsNullOrEmpty(assignedSpell))
        {
            currentSpell = assignedSpell;
        }
        else
        {
            List<string> potentialSpellsToDraw = new List<string>();
            potentialSpellsToDraw.AddRange(offensiveSpells); // Always include offensive spells

            // Track spells currently in hand and their counts
            Dictionary<string, int> currentHandSpellCounts = new Dictionary<string, int>();
            bool revitalizeAlreadyInHand = false;

            if (transform.parent != null)
            {
                foreach (Transform child in transform.parent)
                {
                    CardInteraction cardInHand = child.GetComponent<CardInteraction>();
                    if (cardInHand != null && cardInHand != this && !string.IsNullOrEmpty(cardInHand.currentSpell))
                    {
                        if (cardInHand.currentSpell == "Revitalize")
                        {
                            revitalizeAlreadyInHand = true;
                        }

                        if (currentHandSpellCounts.ContainsKey(cardInHand.currentSpell))
                            currentHandSpellCounts[cardInHand.currentSpell]++;
                        else
                            currentHandSpellCounts[cardInHand.currentSpell] = 1;
                    }
                }
            }

            // Conditionally add "Revitalize" based on cooldown and if not already in hand
            if (WordManager.instance.healDrawLockoutTurns <= 0 && !revitalizeAlreadyInHand)
            {
                potentialSpellsToDraw.Add("Revitalize");
            }

            // Add other non-offensive spells (assuming Bubble Shield is unique and has no cooldown)
            if (nonOffensiveSpells.Contains("Bubble Shield") && (!currentHandSpellCounts.ContainsKey("Bubble Shield") || currentHandSpellCounts["Bubble Shield"] == 0))
            {
                potentialSpellsToDraw.Add("Bubble Shield");
            }

            // Filter out spells that exceed their allowed count (e.g., more than 2 offensive spells)
            List<string> finalAvailableSpells = new List<string>();
            foreach (string spell in potentialSpellsToDraw)
            {
                int maxAllowed = 1; // Default for unique spells like Revitalize, Bubble Shield
                if (offensiveSpells.Contains(spell))
                {
                    maxAllowed = 2; // Allow up to 2 offensive spells
                }

                int currentCount = currentHandSpellCounts.ContainsKey(spell) ? currentHandSpellCounts[spell] : 0;

                if (currentCount < maxAllowed)
                {
                    finalAvailableSpells.Add(spell);
                }
            }

            // Fallback: If no spells can be drawn due to restrictions, ensure at least one offensive spell is drawn.
            if (finalAvailableSpells.Count == 0)
            {
                // This ensures the player always draws a card, even if it's a duplicate offensive spell.
                // This is a design choice to prevent an empty draw pool.
                finalAvailableSpells.Add(offensiveSpells[UnityEngine.Random.Range(0, offensiveSpells.Count)]);
            }

            currentSpell = finalAvailableSpells[UnityEngine.Random.Range(0, finalAvailableSpells.Count)];
        }

        if (spellNameTextUI != null) spellNameTextUI.text = currentSpell;

        if (offensiveSpells.Contains(currentSpell))
        {
            inkCost = Random.Range(2, 5);
        }
        else
        {
            inkCost = Random.Range(1, 4);
        }

        if (inkCostTextUI != null) inkCostTextUI.text = inkCost.ToString();
    }

    public void OnCardTapped()
    {
        if (lockedTurnsLeft > 0)
        {
            Debug.LogWarning("This card is locked for " + lockedTurnsLeft + " more turns!");
            return;
        }

        // --- PATCH: No more Star double-tap delay! Executes instantly ---
        ExecuteSingleTapLogic();
    }

    private void ExecuteSingleTapLogic()
    {
        if (cardState == 0)
        {
            if (currentlyZoomedCard != null && currentlyZoomedCard != this)
            {
                currentlyZoomedCard.Unzoom();
            }

            if (cardCanvas != null) cardCanvas.sortingOrder = 10;

            transform.DOScale(originalScale * 1.5f, 0.2f);
            cardState = 1;
            currentlyZoomedCard = this;
        }
        else if (cardState == 1)
        {
            // 1. Notify the timer FIRST
            WordManager.instance.NotifyCardClicked();

            // 2. THEN do the normal card checks
            if (currentlyPlayedCard != null && currentlyPlayedCard != this)
            {
                currentlyPlayedCard.ReturnToHand();
            }

            originalIndex = transform.GetSiblingIndex();
            transform.SetParent(inputDisplayArea, false);
            transform.DOScale(originalScale * 1.5f, 0.2f);

            if (cardCanvas != null) cardCanvas.sortingOrder = 0;

            cardState = 2;
            currentlyZoomedCard = null;
            currentlyPlayedCard = this;

            // DELETED: WordManager.instance.StartTimer(); 

            if (spellInputPanel != null)
            {
                spellInputPanel.SetActive(true);
                wordInputField.text = currentLetter.ToString();

                // --- PATCH: Force Focus and Keyboard Activation ---
                EventSystem.current.SetSelectedGameObject(wordInputField.gameObject, null);
                // 1. Force the UI system to highlight this box
                wordInputField.Select(); 
                
                // 2. Tell the mobile OS to bring up the keyboard
                wordInputField.ActivateInputField(); 
                
                // 3. Ensure the blinking cursor is AFTER the starting letter
                wordInputField.MoveTextEnd(false);
            }
        }
        else if (cardState == 2)
        {
            ReturnToHand();
        }
    }

    public void Unzoom()
    {
        if (cardState == 1)
        {
            transform.DOScale(originalScale, 0.2f);
            if (cardCanvas != null) cardCanvas.sortingOrder = 0;
            cardState = 0;

            if (currentlyZoomedCard == this) currentlyZoomedCard = null;
        }
    }

    public void ReturnToHand()
    {
        if (cardState == 2)
        {
            transform.SetParent(handContainer, false);
            transform.SetSiblingIndex(originalIndex);

            transform.DOScale(originalScale, 0.2f);

            cardState = 0;

            // DELETED: WordManager.instance.StopTimer(); 

            if (currentlyPlayedCard == this)
            {
                currentlyPlayedCard = null;

                if (spellInputPanel != null)
                {
                    spellInputPanel.SetActive(false);
                    wordInputField.text = "";
                }
            }
        }
    }

    public void ChangeLetter(char newLetter)
    {
        currentLetter = newLetter;
        if (letterTextUI != null) letterTextUI.text = currentLetter.ToString();
    }

    public void LockCard(int turns)
    {
        lockedTurnsLeft = turns;

        UnityEngine.UI.Image cardImage = GetComponent<UnityEngine.UI.Image>();
        if (cardImage != null) cardImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
    }

    public void DecreaseLock()
    {
        if (lockedTurnsLeft > 0)
        {
            lockedTurnsLeft--;
            if (lockedTurnsLeft == 0)
            {
                UnityEngine.UI.Image cardImage = GetComponent<UnityEngine.UI.Image>();
                if (cardImage != null) cardImage.color = Color.white;
            }
        }
    }

    private void OnDestroy()
    {
        if (currentlyPlayedCard == this)
        {
            currentlyPlayedCard = null;
        }

        if (currentlyZoomedCard == this)
        {
            currentlyZoomedCard = null;
        }
    }
}