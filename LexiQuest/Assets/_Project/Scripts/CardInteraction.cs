using UnityEngine;
using DG.Tweening;

public class CardInteraction : MonoBehaviour
{
    // The MAGIC: A shared 'static' variable that remembers which card is currently zoomed across the whole game
    public static CardInteraction currentlyZoomedCard; 

    // 0 = In Hand, 1 = Zoomed, 2 = Played (In Spelling Area)
    private int cardState = 0; 
    private Vector3 originalScale;
    private int originalIndex;

    public Transform inputDisplayArea;
    private Transform handContainer;

    void Start()
    {
        originalScale = transform.localScale;
        handContainer = transform.parent; 
    }

    public void OnCardTapped()
    {
        if (cardState == 0)
        {
            // NEW LOGIC: Check if another card is already zoomed
            if (currentlyZoomedCard != null && currentlyZoomedCard != this)
            {
                // Tell the other card to shrink back down
                currentlyZoomedCard.Unzoom(); 
            }

            // TAP 1: Zoom In
            transform.DOScale(originalScale * 1.5f, 0.2f);
            cardState = 1;

            // Register THIS card as the official zoomed card
            currentlyZoomedCard = this; 
        }
        else if (cardState == 1)
        {
            // TAP 2: Move to Spelling Area
            originalIndex = transform.GetSiblingIndex(); 
            
            transform.SetParent(inputDisplayArea, false);
            transform.DOScale(originalScale, 0.2f); 
            
            cardState = 2; // Mark as played
            
            // Clear the static slot so the player can pick a new letter from their hand
            currentlyZoomedCard = null; 
        }
        else if (cardState == 2)
        {
            // TAP 3: Undo! Return to Hand
            transform.SetParent(handContainer, false);
            transform.SetSiblingIndex(originalIndex); 
            
            cardState = 0; 
        }
    }

    // A small helper method that other cards can trigger
    public void Unzoom()
    {
        if (cardState == 1)
        {
            transform.DOScale(originalScale, 0.2f);
            cardState = 0;
        }
    }
}