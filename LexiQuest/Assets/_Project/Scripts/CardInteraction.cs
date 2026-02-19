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
    // NEW: Reference to the parent panel
    public GameObject spellInputPanel; 

    private int cardState = 0; 
    private Vector3 originalScale;
    private int originalIndex;

    public Transform inputDisplayArea;
    private Transform handContainer;

    void Start()
    {
        originalScale = transform.localScale;
        handContainer = transform.parent; 

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
            
            cardState = 2; 
            currentlyZoomedCard = null; 
            currentlyPlayedCard = this;

            // --- UPDATED: Turn on the whole panel! ---
            if (spellInputPanel != null)
            {
                spellInputPanel.SetActive(true); // Shows the box AND the button
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
                
                // --- UPDATED: Hide the whole panel if canceled ---
                if (spellInputPanel != null)
                {
                    spellInputPanel.SetActive(false);
                    wordInputField.text = ""; 
                }
            }
        }
    }
}