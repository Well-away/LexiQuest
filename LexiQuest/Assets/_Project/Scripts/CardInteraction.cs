using UnityEngine;
using System.Collections; // NEW: Required for Coroutines
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

    // --- UPDATED: Double Tap Timers & Logic ---
    private float lastClickTime = -10f;
    private float doubleClickThreshold = 0.3f; // Lowered to 0.3s for better responsiveness
    private Coroutine tapCoroutine; // The "Wait and See" timer
    // ------------------------------------------

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

        string[] availableSpells = { "Fireball", "Ice Shards", "Wind Blades", "Bubble Shield", "Revitalize" };
        int randomSpellIndex = Random.Range(0, availableSpells.Length); 
        currentSpell = availableSpells[randomSpellIndex];
        if (spellNameTextUI != null) spellNameTextUI.text = currentSpell;

        inkCost = Random.Range(1, 4); 
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

    // --- UPDATED: Tap Logic with Delay ---
    public void OnCardTapped()
    {
        // 1. If the card is in the hand, we wait to see if it's a double-tap (Star)
        if (cardState == 0)
        {
            if (Time.time - lastClickTime < doubleClickThreshold)
            {
                // We caught a double tap! Stop the single tap from happening.
                if (tapCoroutine != null)
                {
                    StopCoroutine(tapCoroutine);
                    tapCoroutine = null;
                }
                
                ToggleStar();
                lastClickTime = -10f; // Force a perfect math reset so the next card works!
                return; 
            }
            
            lastClickTime = Time.time; 
            tapCoroutine = StartCoroutine(ProcessSingleTap());
        }
        // 2. If the card is already zoomed (1) or played (2), NO DELAY! Execute instantly.
        else 
        {
            ExecuteSingleTapLogic();
        }
    }

    private IEnumerator ProcessSingleTap()
    {
        // Wait just enough time to give the player a chance to tap again
        yield return new WaitForSeconds(doubleClickThreshold);
        
        tapCoroutine = null; // Clear the coroutine memory!
        
        // If we make it here without being interrupted, it was definitely a single tap!
        ExecuteSingleTapLogic();
    }
    // --------------------------------------

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

            // --- NEW: Clear the static reference so clicking background works perfectly ---
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
}