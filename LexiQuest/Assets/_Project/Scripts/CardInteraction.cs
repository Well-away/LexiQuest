using UnityEngine;
using System.Collections; 
using System.Collections.Generic; // NEW: Required for Lists and Dictionaries
using DG.Tweening;
using TMPro; 

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

    [Header("Star System")]
    public bool isStarred = false;
    public GameObject starVisualActive; 

    private float lastClickTime = -10f;
    private float doubleClickThreshold = 0.3f; 
    private Coroutine tapCoroutine; 

    [Header("Status Effects")]
    public int lockedTurnsLeft = 0;

    void Start()
    {
        originalScale = transform.localScale;
        handContainer = transform.parent; 
        cardCanvas = GetComponent<Canvas>();
    }

    public void InitializeCardData(char assignedLetter)
    {
        currentLetter = assignedLetter;
        if (letterTextUI != null) letterTextUI.text = currentLetter.ToString();

        // --- 1. Define Spell Categories ---
        List<string> offensiveSpells = new List<string> { "Fireball", "Ice Shards", "Wind Blades" };
        List<string> nonOffensiveSpells = new List<string> { "Bubble Shield", "Revitalize" };
        
        List<string> allAvailableSpells = new List<string>();
        allAvailableSpells.AddRange(offensiveSpells);
        allAvailableSpells.AddRange(nonOffensiveSpells);

        // --- 2. Enforce the 2-Duplicate Limit ---
        Dictionary<string, int> spellCounts = new Dictionary<string, int>();
        
        if (transform.parent != null) 
        {
            foreach (Transform child in transform.parent)
            {
                CardInteraction card = child.GetComponent<CardInteraction>();
                if (card != null && card != this && !string.IsNullOrEmpty(card.currentSpell))
                {
                    if (spellCounts.ContainsKey(card.currentSpell))
                        spellCounts[card.currentSpell]++;
                    else
                        spellCounts[card.currentSpell] = 1;
                }
            }
        }

        foreach (var kvp in spellCounts)
        {
            if (kvp.Value >= 2)
            {
                allAvailableSpells.Remove(kvp.Key); // Strip it from the pool!
            }
        }

        // Fallback just in case the math gets weird and all spells are maxed out
        if (allAvailableSpells.Count == 0) 
        {
            allAvailableSpells.AddRange(offensiveSpells);
            allAvailableSpells.AddRange(nonOffensiveSpells);
        }

        currentSpell = allAvailableSpells[Random.Range(0, allAvailableSpells.Count)];
        if (spellNameTextUI != null) spellNameTextUI.text = currentSpell;

        // --- 3. Enforce Ink Costs ---
        if (offensiveSpells.Contains(currentSpell))
        {
            inkCost = Random.Range(2, 5); // 2, 3, or 4
        }
        else
        {
            inkCost = Random.Range(1, 4); // 1, 2, or 3
        }

        if (inkCostTextUI != null) inkCostTextUI.text = inkCost.ToString();
    }

    public void ToggleStar()
    {
        if (cardState == 2) return; 
        if (WordManager.instance == null) return;

        if (isStarred)
        {
            isStarred = false;
            if (starVisualActive != null) starVisualActive.SetActive(false);
            
            WordManager.instance.currentInk += 1; 
            WordManager.instance.cardsStarredThisTurn -= 1; 
            WordManager.instance.UpdateInkUI();
        }
        else
        {
            if (WordManager.instance.currentInk >= 1 && WordManager.instance.cardsStarredThisTurn < 2)
            {
                isStarred = true;
                if (starVisualActive != null) starVisualActive.SetActive(true);
                
                WordManager.instance.currentInk -= 1; 
                WordManager.instance.cardsStarredThisTurn += 1; 
                WordManager.instance.UpdateInkUI();
            }
        }
    }

    public void RemoveStar()
    {
        isStarred = false;
        if (starVisualActive != null) starVisualActive.SetActive(false);
    }

    public void OnCardTapped()
    {
        if (lockedTurnsLeft > 0)
        {
            Debug.LogWarning("This card is locked for " + lockedTurnsLeft + " more turns!");
            return; 
        }
        
        if (cardState == 0)
        {
            if (Time.time - lastClickTime < doubleClickThreshold)
            {
                if (tapCoroutine != null)
                {
                    StopCoroutine(tapCoroutine);
                    tapCoroutine = null;
                }
                
                ToggleStar();
                lastClickTime = -10f; 
                return; 
            }
            
            lastClickTime = Time.time; 
            tapCoroutine = StartCoroutine(ProcessSingleTap());
        }
        else 
        {
            ExecuteSingleTapLogic();
        }
    }

    private IEnumerator ProcessSingleTap()
    {
        yield return new WaitForSeconds(doubleClickThreshold);
        tapCoroutine = null; 
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
            if (currentlyPlayedCard != null && currentlyPlayedCard != this)
            {
                currentlyPlayedCard.ReturnToHand();
            }

            if (isStarred)
            {
                isStarred = false;
                if (starVisualActive != null) starVisualActive.SetActive(false);
                
                WordManager.instance.currentInk += 1; 
                WordManager.instance.cardsStarredThisTurn -= 1; 
                WordManager.instance.UpdateInkUI();
            }

            originalIndex = transform.GetSiblingIndex(); 
            transform.SetParent(inputDisplayArea, false);
            transform.DOScale(originalScale * 1.5f, 0.2f); 
            
            if (cardCanvas != null) cardCanvas.sortingOrder = 0; 
            
            cardState = 2; 
            currentlyZoomedCard = null; 
            currentlyPlayedCard = this;

            if (spellInputPanel != null)
            {
                spellInputPanel.SetActive(true);
                wordInputField.text = currentLetter.ToString(); 
                wordInputField.ActivateInputField(); 
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
        
        // Visually tint the card dark gray
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
                // Restore the card to its bright white color
                UnityEngine.UI.Image cardImage = GetComponent<UnityEngine.UI.Image>();
                if (cardImage != null) cardImage.color = Color.white;
            }
        }
    }
}