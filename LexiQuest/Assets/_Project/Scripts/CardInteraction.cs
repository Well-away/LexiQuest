using UnityEngine;
using DG.Tweening;
using TMPro;

public class CardInteraction : MonoBehaviour
{
    // The MAGIC: Shared variables across all cards
    public static CardInteraction currentlyZoomedCard; 
    public static CardInteraction currentlyPlayedCard; 

    //TMPro Letter randomizer
    public TextMeshProUGUI letterTextUI;
    public char currentLetter;
    // 0 = In Hand, 1 = Zoomed, 2 = Played

    public TMP_InputField wordInputField;

    private int cardState = 0; 
    private Vector3 originalScale;
    private int originalIndex;

    public Transform inputDisplayArea;
    private Transform handContainer;

    void Start()
    {
        originalScale = transform.localScale;
        handContainer = transform.parent; 

        string alphabet = "ABCDEFGHIJKLMNOPRSTUVWY";
        int randomIndex = Random.Range(0, alphabet.Length);
        
        // Grab the letter at that random slot
        currentLetter = alphabet[randomIndex];

        // Change the actual text on the physical card!
        letterTextUI.text = currentLetter.ToString();
    }

    public void OnCardTapped()
    {
        if (cardState == 0)
        {
            // TAP 1: Zoom In
            if (currentlyZoomedCard != null && currentlyZoomedCard != this)
            {
                currentlyZoomedCard.Unzoom(); 
            }

            transform.DOScale(originalScale * 1.5f, 0.2f);
            cardState = 1;
            currentlyZoomedCard = this; 
        }
        else if (cardState == 1)
        {
            // TAP 2: Move to Display Area (Only 1 allowed!)
            
            // NEW: If there is ALREADY a card in the display area, send it back!
            if (currentlyPlayedCard != null && currentlyPlayedCard != this)
            {
                currentlyPlayedCard.ReturnToHand();
            }

            originalIndex = transform.GetSiblingIndex(); 
            
            transform.SetParent(inputDisplayArea, false);
            transform.DOScale(originalScale, 0.2f); 
            
            cardState = 2; 
            currentlyZoomedCard = null; 
            
            // Register THIS card as the official played card
            currentlyPlayedCard = this;

            if (wordInputField != null)
            {
                wordInputField.gameObject.SetActive(true); // Turn it on
                wordInputField.text = currentLetter.ToString(); // Pre-fill the first letter
                wordInputField.ActivateInputField(); // Put the blinking cursor inside it automatically
                wordInputField.MoveTextEnd(false); // Move cursor to the end of the letter
            }
        }
        else if (cardState == 2)
        {
            // TAP 3: Undo!
            ReturnToHand();
        }
    }

    // Helper method to shrink back down
    public void Unzoom()
    {
        if (cardState == 1)
        {
            transform.DOScale(originalScale, 0.2f);
            cardState = 0;
        }
    }

    // NEW: Helper method so cards can safely kick each other back to the hand
    public void ReturnToHand()
    {
        if (cardState == 2)
        {
            transform.SetParent(handContainer, false);
            transform.SetSiblingIndex(originalIndex); 
            cardState = 0;
            
            // If this was the official played card, clear the slot so it's empty
            if (currentlyPlayedCard == this)
            {
                currentlyPlayedCard = null;

                if (wordInputField != null)
                {
                    wordInputField.gameObject.SetActive(false);
                    wordInputField.text = ""; // Clear the text
                }
            }
        }
    }
}