using UnityEngine;

public class player : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public int currentShield = 0; // NEW: Armor variable

    public HealthBar healthBar;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }


    public void TakeDamage(int damage)
    {
        // --- NEW: Shield absorbs damage first! ---
        if (currentShield > 0)
        {
            if (damage >= currentShield)
            {
                damage -= currentShield; // Shield breaks, leftover damage hurts player
                currentShield = 0;
                Debug.Log("Shield broken!");
            }
            else
            {
                currentShield -= damage; // Shield absorbs all of it
                damage = 0;
                Debug.Log("Shield held! Remaining shield: " + currentShield);
            }
        }

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        healthBar.SetHealth(currentHealth);
    }

    // --- NEW: Spell Functions ---
    public void Heal(int healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth) currentHealth = maxHealth; // Cannot heal over 100
        healthBar.SetHealth(currentHealth);
        Debug.Log("Player healed for " + healAmount + "! Current HP: " + currentHealth);
    }

    public void AddShield(int shieldAmount)
    {
        currentShield += shieldAmount;
        Debug.Log("Player gained " + shieldAmount + " Armor! Total Armor: " + currentShield);
    }
}