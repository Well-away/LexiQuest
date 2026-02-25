using UnityEngine;
using System.Collections.Generic;

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
    public int boulderThrowCooldown = 3; 
    public int bindingCooldown = 5;      

    private int hardenedSkinTurnsLeft = 0;
    public int currentWave = 1;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (hardenedSkinTurnsLeft > 0)
        {
            damage = Mathf.RoundToInt(damage * 0.5f); 
            Debug.Log("<color=grey>Monster's Hardened Skin absorbed 50% of the damage!</color>");
        }

        currentHealth -= damage;
        
        if (currentHealth <= 0) 
        {
            currentHealth = 0;
            healthBar.SetHealth(currentHealth);
            SummonNextMonster(); 
        }
        else
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    private void SummonNextMonster()
    {
        Debug.Log($"<color=yellow>Monster Defeated! You cleared Wave {currentWave}!</color>");

        if (playerTarget != null)
        {
            int healReward = Mathf.RoundToInt(playerTarget.maxHealth * 0.30f);
            playerTarget.Heal(healReward);
        }

        currentWave++;
        maxHealth = Mathf.RoundToInt(maxHealth * 1.30f); 
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);

        groundSlamCooldown = 0;
        hardenedSkinCooldown = 0;
        crashingFistCooldown = 10;
        hardenedSkinTurnsLeft = 0;
        boulderThrowCooldown = 3; 
        bindingCooldown = 5;      

        Renderer enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
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
        if (boulderThrowCooldown > 0) boulderThrowCooldown--;
        if (bindingCooldown > 0) bindingCooldown--;

        float hpPercent = (float)currentHealth / maxHealth;

        // --- NEW: PHASE-BASED AI PRIORITY ---
        if (hpPercent < 0.10f && crashingFistCooldown == 0)
        {
            CastCrashingFist(); // Phase 4: Under 10%
        }
        else if (hpPercent < 0.45f && bindingCooldown == 0)
        {
            CastBinding();      // Phase 3: Under 45%
        }
        else if (hpPercent < 0.70f && groundSlamCooldown == 0)
        {
            CastGroundSlam();   // Phase 2: Under 70%
        }
        // Phase 1: 70% or above (or if ultimate/special skills are on cooldown)
        else if (hardenedSkinCooldown == 0)
        {
            CastHardenedSkin();
        }
        else if (boulderThrowCooldown == 0)
        {
            CastBoulderThrow();
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

    private void CastBoulderThrow()
    {
        boulderThrowCooldown = 4;
        int damage = 10 + Mathf.RoundToInt(playerTarget.maxHealth * 0.10f);
        Debug.Log($"<color=red>Monster uses Boulder Throw!</color> Deals {damage} damage.");
        playerTarget.TakeDamage(damage);
    }

    private void CastBinding()
    {
        bindingCooldown = 6;
        int lostHP = maxHealth - currentHealth;
        
        // PATCH: 25% of Golem's LOST HP
        int damage = Mathf.RoundToInt(lostHP * 0.25f);
        int dotDamage = Mathf.RoundToInt(playerTarget.maxHealth * 0.05f);
        
        Debug.Log($"<color=magenta>Monster uses BINDING!</color> Deals {damage} damage, drains 5 Ink, and applies a {dotDamage} DoT for 2 turns!");
        playerTarget.TakeDamage(damage);

        // PATCH: Drain 5 Ink
        if (wordManager != null)
        {
            wordManager.currentInk -= 5;
            if (wordManager.currentInk < 0) wordManager.currentInk = 0;
            wordManager.UpdateInkUI();
        }

        // PATCH: Apply 2-Turn DoT (No longer stuns)
        if (playerTarget != null)
        {
            playerTarget.ApplyBindingDoT(2, dotDamage);
        }
    }

    private void CastGroundSlam()
    {
        groundSlamCooldown = 6;
        // PATCH: 25% of Amy's CURRENT HP
        int damage = Mathf.RoundToInt(playerTarget.currentHealth * 0.25f);
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
        
        // PATCH: Damage is 50% of Golem's LOST HP
        int lostHP = maxHealth - currentHealth;
        int damage = Mathf.RoundToInt(lostHP * 0.50f);
        
        Debug.Log($"<color=magenta>Monster uses CRASHING FIST (Ultimate)!</color> Deals {damage} damage.");
        playerTarget.TakeDamage(damage);

        // PATCH: Heals for 50% of the damage dealt
        int healAmount = Mathf.RoundToInt(damage * 0.50f);
        currentHealth += healAmount;
        if (currentHealth > maxHealth) currentHealth = maxHealth; // Prevent over-healing
        healthBar.SetHealth(currentHealth);
        Debug.Log($"<color=green>Monster heals for {healAmount} HP from Crashing Fist!</color>");
    }
}