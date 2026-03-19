using UnityEngine;

public class player : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public int currentShield = 0;

    public HealthBar healthBar;

    // --- UPDATED: Status Effects ---
    public int bindingDoTTurnsLeft = 0;
    public int bindingDoTDamage = 0;

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

    public void ApplyBubbleShield(int incomingAmount)
    {
        // PATCH 1: Static 50 HP Cap
        int maxShieldCap = 50; 

        if (incomingAmount > currentShield)
        {
            currentShield = incomingAmount;
            NotificationManager.instance.ShowMessage($"Greater Shield Cast! {currentShield} HP!");
        }
        else
        {
            int bonus = Mathf.RoundToInt(incomingAmount * 0.20f);
            currentShield += bonus;
            NotificationManager.instance.ShowMessage($"Shield Fortified! +{bonus} HP");
        }

        // Enforce the 50 HP cap
        if (currentShield > maxShieldCap)
        {
            currentShield = maxShieldCap;
            NotificationManager.instance.ShowMessage("Shield at Max Capacity!");
        }
    }

    // --- UPDATED: New DoT Logic ---
    public void ApplyBindingDoT(int turns, int damagePerTurn)
    {
        bindingDoTTurnsLeft = turns;
        bindingDoTDamage = damagePerTurn;
    }

    public void HandleStartOfTurn()
    {
        if (bindingDoTTurnsLeft > 0)
        {
            Debug.Log($"<color=purple>Binding effect triggers! Player takes {bindingDoTDamage} damage from DoT.</color>");
            NotificationManager.instance.ShowMessage("Bound! Taking DoT Damage!");
            TakeDamage(bindingDoTDamage);
            bindingDoTTurnsLeft--;
        }
        else
        {
            // If no DoT, just announce the turn!
            NotificationManager.instance.ShowMessage("Player Turn!");
        }
    }
}