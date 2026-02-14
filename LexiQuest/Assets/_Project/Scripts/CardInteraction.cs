using UnityEngine;
using DG.Tweening;

public class CardInteraction : MonoBehaviour
{
    // 0 = In Hand, 1 = Zoomed, 2 = Played (In Spelling Area)
    private int cardState = 0; 
    private Vector3 originalScale;
    private int originalIndex; // This remembers its exact slot in your hand!

    public Transform inputDisplayArea;
    private Transform handContainer; // We will grab this automatically in Start()

    void Start()
    {
        originalScale = transform.localScale;
        // Automatically save the Hand Container so we know where to return it
        handContainer = transform.parent; 
    }

    public void OnCardTapped()
    {
        if (cardState == 0)
        {
            // TAP 1: Zoom In
            transform.DOScale(originalScale * 1.5f, 0.2f);
            cardState = 1;
        }
        else if (cardState == 1)
        {
            // TAP 2: Move to Spelling Area
            // Save its exact position in the hand before moving it
            originalIndex = transform.GetSiblingIndex(); 
            
            transform.SetParent(inputDisplayArea, false);
            transform.DOScale(originalScale, 0.2f); 
            
            cardState = 2; // Mark as played
        }
        else if (cardState == 2)
        {
            // TAP 3: Undo! Return to Hand
            transform.SetParent(handContainer, false);
            
            // Put it exactly back where it belongs, not just at the end!
            transform.SetSiblingIndex(originalIndex); 
            
            cardState = 0; // Reset back to the starting state
        }
    }
}