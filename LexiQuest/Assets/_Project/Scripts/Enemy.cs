using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // NEW: Allows us to tint the monster's color!

public class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;

    [Header("Battle Targets")]
    public player playerTarget;
    public WordManager wordManager; 

    [Header("Golem Cooldowns")]
    public int groundSlamCooldown = 0;
    public int hardenedSkinCooldown = 0;
    public int crashingFistCooldown = 10; 

    private int hardenedSkinTurnsLeft = 0;

    [Header("Roguelike Progression")]
    public int currentWave = 1;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(int damage)
    {
        // 1. Hardened Skin Passive Check 
        if (hardenedSkinTurnsLeft > 0)
        {
            damage = Mathf.RoundToInt(damage * 0.5f); 
            Debug.Log("<color=grey>Monster's Hardened Skin absorbed 50% of the damage!</color>");
        }

        currentHealth -= damage;
        
        // 2. Check for Death!
        if (currentHealth <= 0) 
        {
            currentHealth = 0;
            healthBar.SetHealth(currentHealth);
            SummonNextMonster(); // Trigger the Roguelike loop!
        }
        else
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    private void SummonNextMonster()
    {
        Debug.Log($"<color=yellow>Monster Defeated! You cleared Wave {currentWave}!</color>");

        // --- ROGUELIKE REWARD ---
        // Heal Amy for 30% of her max HP so she can survive the endless gauntlet
        if (playerTarget != null)
        {
            int healReward = Mathf.RoundToInt(playerTarget.maxHealth * 0.30f);
            playerTarget.Heal(healReward);
        }

        // --- SCALE DIFFICULTY ---
        currentWave++;
        maxHealth = Mathf.RoundToInt(maxHealth * 1.30f); // HP increases by 30% every wave!
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);

        // --- RESET AI ---
        groundSlamCooldown = 0;
        hardenedSkinCooldown = 0;
        crashingFistCooldown = 10;
        hardenedSkinTurnsLeft = 0;

        // --- VISUAL JUICE (3D Version) ---
        // Grab the 3D Renderer instead of a UI Image
        Renderer enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            // Changes the color of the cube's material
            enemyRenderer.material.color = new Color(Random.value, Random.value, Random.value, 1f);
        }

        Debug.Log($"<color=red>A new Variant appears! Wave {currentWave} begins! (Monster HP: {maxHealth})</color>");
    }

    public void TakeTurn()
    {
        if (hardenedSkinTurnsLeft > 0) hardenedSkinTurnsLeft--;
        if (groundSlamCooldown > 0) groundSlamCooldown--;
        if (hardenedSkinCooldown > 0) hardenedSkinCooldown--;
        if (crashingFistCooldown > 0) crashingFistCooldown--;

        if (crashingFistCooldown == 0)
        {
            CastCrashingFist();
        }
        else if (hardenedSkinCooldown == 0 && currentHealth <= maxHealth * 0.75f) 
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
        if (damage < 1) damage = 1; 
        Debug.Log($"<color=orange>Monster uses Basic Attack!</color> Deals {damage} damage.");
        playerTarget.TakeDamage(damage);
    }

    private void CastGroundSlam()
    {
        groundSlamCooldown = 6;
        int damage = Mathf.RoundToInt(playerTarget.maxHealth * 0.10f);
        Debug.Log($"<color=red>Monster uses Ground Slam!</color> Deals {damage} damage and locks 2 cards.");
        playerTarget.TakeDamage(damage);

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
        Debug.Log("<color=grey>Monster uses Hardened Skin!</color> Takes 50% less damage for 3 turns.");
    }

    private void CastCrashingFist()
    {
        crashingFistCooldown = 10;
        int damage = Mathf.RoundToInt(playerTarget.maxHealth * 0.15f);
        Debug.Log($"<color=magenta>Monster uses CRASHING FIST (Ultimate)!</color> Deals {damage} damage.");
        playerTarget.TakeDamage(damage);
    }
}