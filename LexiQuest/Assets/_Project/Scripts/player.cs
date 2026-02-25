using UnityEngine;

public class player : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public int currentShield = 0; 

    public HealthBar healthBar;

    // --- NEW: Status Effects ---
    public bool isStunned = false;
    public bool hasBindingDoT = false;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (currentShield > 0)
        {
            if (damage >= currentShield)
            {
                damage -= currentShield; 
                currentShield = 0;
                Debug.Log("Shield broken!");
            }
            else
            {
                currentShield -= damage; 
                damage = 0;
                Debug.Log("Shield held! Remaining shield: " + currentShield);
            }
        }

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        healthBar.SetHealth(currentHealth);
    }

    public void Heal(int healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth) currentHealth = maxHealth; 
        healthBar.SetHealth(currentHealth);
        Debug.Log("Player healed for " + healAmount + "! Current HP: " + currentHealth);
    }

    public void AddShield(int shieldAmount)
    {
        currentShield += shieldAmount;
        Debug.Log("Player gained " + shieldAmount + " Armor! Total Armor: " + currentShield);
    }

    // --- NEW: Status Effect Methods ---
    public void ApplyBindingStun()
    {
        isStunned = true;
        hasBindingDoT = true;
    }

    public void HandleStartOfTurn()
    {
        if (hasBindingDoT)
        {
            Debug.Log("<color=purple>Binding effect triggers! Player takes 10 damage.</color>");
            TakeDamage(10);
            hasBindingDoT = false; // Removes the DoT so it only hits once
        }
    }

    public void ClearStun()
    {
        if (isStunned)
        {
            isStunned = false;
            Debug.Log("<color=green>Stun has worn off. You can cast spells again.</color>");
        }
    }
}