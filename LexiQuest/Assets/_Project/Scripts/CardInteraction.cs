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

    void Start()
    {
        originalScale = transform.localScale;
        handContainer = transform.parent; 
        cardCanvas = GetComponent<Canvas>();

        // 1. Random Letter Logic
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        int randomLetterIndex = Random.Range(0, alphabet.Length);
        currentLetter = alphabet[randomLetterIndex];
        if (letterTextUI != null) letterTextUI.text = currentLetter.ToString();

        // --- NEW: 2. Random Spell Logic ---
        string[] availableSpells = { "Fireball", "Ice Shards", "Wind Blades", "Bubble Shield", "Revitalize" };
        int randomSpellIndex = Random.Range(0, availableSpells.Length); // Picks a number from 0 to 4
        currentSpell = availableSpells[randomSpellIndex];
        
        // Update the physical text on the card
        if (spellNameTextUI != null) spellNameTextUI.text = currentSpell;
        // ----------------------------------

        inkCost = Random.Range(1, 4); // Assigns a random cost of 1, 2, or 3
        if (inkCostTextUI != null) inkCostTextUI.text = inkCost.ToString();
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
            transform.DOScale(originalScale, 0.2f); 
            
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
}