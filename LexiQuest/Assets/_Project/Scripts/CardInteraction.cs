using UnityEngine;
using DG.Tweening;
using TMPro; 

public class CardInteraction : MonoBehaviour
{
    public static CardInteraction currentlyZoomedCard; 
    public static CardInteraction currentlyPlayedCard; 

    public TextMeshProUGUI letterTextUI; 
    public char currentLetter; 

    public TMP_InputField wordInputField; 
    public GameObject spellInputPanel; 

    private int cardState = 0; 
    private Vector3 originalScale;
    private int originalIndex;

    public Transform inputDisplayArea;
    private Transform handContainer;
    
    // NEW: We will grab the Canvas component to fix the rendering depth!
    private Canvas cardCanvas; 

    void Start()
    {
        originalScale = transform.localScale;
        handContainer = transform.parent; 
        
        // Grab the Canvas we just added in the Inspector
        cardCanvas = GetComponent<Canvas>();

        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        int randomIndex = Random.Range(0, alphabet.Length);
        currentLetter = alphabet[randomIndex];
        letterTextUI.text = currentLetter.ToString();
    }

    public void OnCardTapped()
    {
        if (cardState == 0)
        {
            if (currentlyZoomedCard != null && currentlyZoomedCard != this)
            {
                currentlyZoomedCard.Unzoom(); 
            }

            // --- THE FIX: Bring to front visually, not physically! ---
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

            // Save the exact index so we can put it back if canceled
            originalIndex = transform.GetSiblingIndex(); 
            
            transform.SetParent(inputDisplayArea, false);
            transform.DOScale(originalScale, 0.2f); 
            
            // --- Reset the visual sorting order so it looks normal in the spell area ---
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
            
            // --- THE FIX: Reset visual sorting order ---
            if (cardCanvas != null) cardCanvas.sortingOrder = 0; 
            
            cardState = 0;
        }
    }

    public void ReturnToHand()
    {
        if (cardState == 2)
        {
            transform.SetParent(handContainer, false);
            transform.SetSiblingIndex(originalIndex); // Put it exactly back where it belongs!
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