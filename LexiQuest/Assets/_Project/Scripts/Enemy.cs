using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;

    [Header("Battle Targets")]
    public player playerTarget;
    public WordManager wordManager; // Needed to grab the cards for Ground Slam

    [Header("Golem Cooldowns")]
    public int groundSlamCooldown = 0;
    public int hardenedSkinCooldown = 0;
    public int crashingFistCooldown = 10; // Ult starts on a 10-turn timer!

    private int hardenedSkinTurnsLeft = 0;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(int damage)
    {
        // 1. Hardened Skin Passive Check (Reduces damage by 50%)
        if (hardenedSkinTurnsLeft > 0)
        {
            damage = Mathf.RoundToInt(damage * 0.5f); 
            Debug.Log("<color=grey>Golem's Hardened Skin absorbed 50% of the damage!</color>");
        }

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        healthBar.SetHealth(currentHealth);

        if (currentHealth == 0) Debug.Log("Enemy Defeated!");
    }

    public void TakeTurn()
    {
        // 1. Tick down timers at the start of the turn
        if (hardenedSkinTurnsLeft > 0) hardenedSkinTurnsLeft--;
        if (groundSlamCooldown > 0) groundSlamCooldown--;
        if (hardenedSkinCooldown > 0) hardenedSkinCooldown--;
        if (crashingFistCooldown > 0) crashingFistCooldown--;

        // 2. Decide Action (Priority based)
        if (crashingFistCooldown == 0)
        {
            CastCrashingFist();
        }
        else if (hardenedSkinCooldown == 0 && currentHealth <= maxHealth * 0.75f) // Won't shield if at full HP!
        {
            CastHardenedSkin();
        }
        else if (groundSlamCooldown == 0)
        {
            CastGroundSlam();
        }
        else
        {
            BasicAttack();
        }
    }

    private void BasicAttack()
    {
        int damage = Mathf.RoundToInt(playerTarget.currentHealth * 0.08f);
        if (damage < 1) damage = 1; // Guarantees it always does at least 1 damage
        Debug.Log($"<color=orange>Golem uses Basic Attack!</color> Deals {damage} damage.");
        playerTarget.TakeDamage(damage);
    }

    private void CastGroundSlam()
    {
        groundSlamCooldown = 6;
        int damage = Mathf.RoundToInt(playerTarget.maxHealth * 0.10f);
        Debug.Log($"<color=red>Golem uses Ground Slam!</color> Deals {damage} damage and locks 2 cards.");
        playerTarget.TakeDamage(damage);

        // Lock 2 random unlocked cards in your hand
        List<CardInteraction> unlockedCards = new List<CardInteraction>();
        if (wordManager != null && wordManager.handContainer != null)
        {
            foreach (Transform child in wordManager.handContainer)
            {
                CardInteraction card = child.GetComponent<CardInteraction>();
                if (card != null && card.lockedTurnsLeft == 0 && card.currentLetter != '\0')
                {
                    unlockedCards.Add(card);
                }
            }

            int cardsToLock = Mathf.Min(2, unlockedCards.Count);
            for (int i = 0; i < cardsToLock; i++)
            {
                int randIndex = Random.Range(0, unlockedCards.Count);
                unlockedCards[randIndex].LockCard(2);
                unlockedCards.RemoveAt(randIndex); 
            }
        }
    }

    private void CastHardenedSkin()
    {
        hardenedSkinCooldown = 6;
        hardenedSkinTurnsLeft = 3;
        Debug.Log("<color=grey>Golem uses Hardened Skin!</color> Takes 50% less damage for 3 turns.");
    }

    private void CastCrashingFist()
    {
        crashingFistCooldown = 10;
        int damage = Mathf.RoundToInt(playerTarget.maxHealth * 0.15f);
        Debug.Log($"<color=magenta>Golem uses CRASHING FIST (Ultimate)!</color> Deals {damage} damage.");
        playerTarget.TakeDamage(damage);
    }

    
}