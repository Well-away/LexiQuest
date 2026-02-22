using UnityEngine;
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
    
    //New Ink Cost
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
    public GameObject starVisualActive; // The yellow star icon to show it is locked in

    void Start()
    {
        originalScale = transform.localScale;
        handContainer = transform.parent; 
        cardCanvas = GetComponent<Canvas>();
    }

    public void InitializeCardData(char assignedLetter)
    {
        // 1. Accept the unique letter from the dealer
        currentLetter = assignedLetter;
        if (letterTextUI != null) letterTextUI.text = currentLetter.ToString();

        // 2. Roll for a random spell
        string[] availableSpells = { "Fireball", "Ice Shards", "Wind Blades", "Bubble Shield", "Revitalize" };
        int randomSpellIndex = Random.Range(0, availableSpells.Length); 
        currentSpell = availableSpells[randomSpellIndex];
        if (spellNameTextUI != null) spellNameTextUI.text = currentSpell;

        // 3. Roll for a random Ink cost
        inkCost = Random.Range(1, 4); 
        if (inkCostTextUI != null) inkCostTextUI.text = inkCost.ToString();
    }

    public void ToggleStar()
    {
        Debug.Log("Star button was clicked on letter: " + currentLetter);

        // Safety Check 1: Is it in the hand?
        if (cardState != 0)
        {
            Debug.LogWarning("Cannot star! Card is currently zoomed or played.");
            return; 
        }

        // Safety Check 2: Did WordManager successfully link up?
        if (WordManager.instance == null)
        {
            Debug.LogError("WordManager instance is missing! Cannot check Ink.");
            return;
        }

        if (isStarred)
        {
            Debug.Log("Un-starring card and refunding 1 Ink.");
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
                Debug.Log("Starring card! Paying 1 Ink.");
                isStarred = true;
                if (starVisualActive != null) starVisualActive.SetActive(true);
                
                WordManager.instance.currentInk -= 1; 
                WordManager.instance.cardsStarredThisTurn += 1; 
                WordManager.instance.UpdateInkUI();
            }
            else
            {
                Debug.LogWarning("Cannot star! Ink: " + WordManager.instance.currentInk + " | Stars this turn: " + WordManager.instance.cardsStarredThisTurn);
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

            originalIndex = transform.GetSiblingIndex(); 
            
            transform.SetParent(inputDisplayArea, false);

            // --- UPDATED: Tell it to stay zoomed (1.5x) in the center slot! ---
            transform.DOScale(originalScale * 1.5f, 0.2f); 
            // ------------------------------------------------------------------
            
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
        }
    }

    public void ReturnToHand()
    {
        if (cardState == 2)
        {
            transform.SetParent(handContainer, false);
            transform.SetSiblingIndex(originalIndex); 
            
            // --- NEW: Shrink the card back down to normal size smoothly! ---
            transform.DOScale(originalScale, 0.2f);
            // ---------------------------------------------------------------

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