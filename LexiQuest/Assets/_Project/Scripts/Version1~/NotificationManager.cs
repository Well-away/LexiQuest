using UnityEngine;
using TMPro;
using DG.Tweening; 

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager instance;

    [Header("UI References")]
    public RectTransform bannerPanel; 
    public TextMeshProUGUI bannerText; 

    [Header("Animation Settings")]
    public Vector2 hiddenPosition = new Vector2(0, 150); // Off-screen top
    public Vector2 visiblePosition = new Vector2(0, -50); // On-screen top

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        bannerPanel.anchoredPosition = hiddenPosition;
    }

    public void ShowMessage(string message, float displayTime = 2.0f)
    {
        // 1. Instantly stop any stuck animations
        bannerPanel.DOKill(); 
        
        bannerText.text = message;
        
        // 2. Chained Animation: Slide down -> Wait -> Slide back up!
        bannerPanel.DOAnchorPos(visiblePosition, 0.3f).SetEase(Ease.OutBack).OnComplete(() => 
        {
            bannerPanel.DOAnchorPos(hiddenPosition, 0.3f).SetEase(Ease.InBack).SetDelay(displayTime);
        });
    }

    // You can still call this if you ever need to force it to hide early
    public void HideMessage()
    {
        bannerPanel.DOKill(); 
        bannerPanel.DOAnchorPos(hiddenPosition, 0.3f).SetEase(Ease.InBack);
    }
}