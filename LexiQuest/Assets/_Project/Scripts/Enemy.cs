using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int maxHealth = 100;
    public int currentHealth;

    public HealthBar healthBar;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // --- NEW: Prevent negative health! ---
        if (currentHealth < 0) currentHealth = 0;
        // -------------------------------------

        healthBar.SetHealth(currentHealth);
        
        // Optional: A quick check to see if the enemy is dead!
        if (currentHealth == 0)
        {
            Debug.Log("Enemy Defeated!");
        }
    }
}
