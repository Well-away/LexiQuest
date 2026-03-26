using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// THE MASTER CONDITION LIST (Cleaned up! No more 2-letter inclusions)
public enum Tier2Type 
{ 
    // Shapes & Sizes
    ExactLength_4, ExactLength_5, ExactLength_6, ExactLength_7, ExactLength_8,
    MinLength_5, MinLength_6, MinLength_7, MinLength_8,
    
    // Suffixes
    EndsWith_S, EndsWith_ED, EndsWith_ER, EndsWith_ING, EndsWith_LY, 
    EndsWith_Y, EndsWith_T, EndsWith_N, EndsWith_E, EndsWith_TION,
    
    // Exclusions
    No_Letter_A, No_Letter_E, No_Letter_I, No_Letter_O, No_Letter_U,
    No_Letter_T, No_Letter_S, No_Letter_R, No_Letter_N, No_Letter_L,
    No_Letter_P, No_Letter_C
}

// The Ultimate Boss Mechanics (Only active on the 5th turn)
public enum Tier3Type 
{ 
    None,               // Used for Turns 1-4
    FlawlessCasting,    // No backspace allowed
    SpeedCasting,       // 10-second timer
    DoubleCast,         // Two words separated by space
    BlindCasting        // Asterisks only
}

public class QuestData
{
    public string targetLetter; // <--- UPGRADED TO STRING
    public int targetLength; // Only used if Tier 2 is an Exact Length
    public Tier2Type tier2Rule;
    public Tier3Type tier3Rule;
    public string tier1Description;
    public string tier2Description;
    public string tier3Description;
}

public class QuestManager : MonoBehaviour
{
    // Upgraded to strings! Added the Consonant Clusters to Group 2 to make it harder!
    
    private List<string> group1 = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" };
    private List<string> group2 = new List<string> { "L", "M", "N", "O", "P", "R", "S", "T", "U", "W", "Y", "SH", "CH", "TH", "ST" }; 
    
    /*for testing
    private List<string> group1 = new List<string> { "G" };
    private List<string> group2 = new List<string> { "G" };
    */

    private List<string> currentActiveGroup;
    private List<string> remainingLetters;

    void Awake()
    {
        // Initialize with Group 1
        currentActiveGroup = new List<string>(group1);
        ResetRotation();
    }

    private void ResetRotation()
    {
        remainingLetters = new List<string>(currentActiveGroup);
        // Shuffle the prefixes so they aren't in alphabetical order
        for (int i = 0; i < remainingLetters.Count; i++)
        {
            string temp = remainingLetters[i];
            int randomIndex = Random.Range(i, remainingLetters.Count);
            remainingLetters[i] = remainingLetters[randomIndex];
            remainingLetters[randomIndex] = temp;
        }
    }

    public QuestData GenerateQuest(int currentTurn)
    {
        QuestData newQuest = new QuestData();

        // 1. TIER 1: Prefix Rotation
        if (remainingLetters.Count == 0)
        {
            // Swap Groups
            currentActiveGroup = (currentActiveGroup == group1) ? group2 : group1;
            ResetRotation();
            Debug.Log("Swapping to next letter group!");
        }

        newQuest.targetLetter = remainingLetters[0];
        remainingLetters.RemoveAt(0);
        newQuest.tier1Description = "Word starting with '" + newQuest.targetLetter + "'";

        // --- 2. TIER 2: The Optimized Lexical List (No mid-word inclusions) ---
        Tier2Type[] easyQuests = { 
            Tier2Type.ExactLength_4, Tier2Type.ExactLength_5, Tier2Type.MinLength_5,
            Tier2Type.EndsWith_S, Tier2Type.EndsWith_T, Tier2Type.EndsWith_E, Tier2Type.EndsWith_Y,
            Tier2Type.No_Letter_P, Tier2Type.No_Letter_C, Tier2Type.No_Letter_L
        };
        
        Tier2Type[] mediumQuests = { 
            Tier2Type.ExactLength_6, Tier2Type.MinLength_6, 
            Tier2Type.EndsWith_ED, Tier2Type.EndsWith_ER, Tier2Type.EndsWith_ING, Tier2Type.EndsWith_N,
            Tier2Type.No_Letter_A, Tier2Type.No_Letter_I, Tier2Type.No_Letter_O, Tier2Type.No_Letter_U
        };
        
        Tier2Type[] hardQuests = { 
            Tier2Type.ExactLength_7, Tier2Type.ExactLength_8, Tier2Type.MinLength_7, Tier2Type.MinLength_8,
            Tier2Type.EndsWith_LY, Tier2Type.EndsWith_TION,
            Tier2Type.No_Letter_E, Tier2Type.No_Letter_T, Tier2Type.No_Letter_S, Tier2Type.No_Letter_R, Tier2Type.No_Letter_N
        };

        int roll = Random.Range(1, 101); 
        
        // Ensure tricky letters or multi-letter clusters don't get impossible rules
        bool isTrickyLetter = new[] { "J", "K", "W", "Y", "SH", "CH", "TH" }.Contains(newQuest.targetLetter);

        // We use a do-while loop to reroll if the game accidentally bans our starting letter or hits a linguistic void!
        do 
        {
            // LOOPHOLE PATCH: Force Easy Quests for Tricky Letters/Clusters
            if (isTrickyLetter)
            {
                newQuest.tier2Rule = easyQuests[Random.Range(0, easyQuests.Length)];
            }
            // Normal Dynamic Escalation
            else if (currentTurn <= 2)
            {
                newQuest.tier2Rule = easyQuests[Random.Range(0, easyQuests.Length)];
            }
            else if (currentTurn <= 4) // Turns 3 and 4
            {
                if (roll <= 70) newQuest.tier2Rule = easyQuests[Random.Range(0, easyQuests.Length)];
                else newQuest.tier2Rule = mediumQuests[Random.Range(0, mediumQuests.Length)];
            }
            else // Phase 3 (Turns 6+)
            {
                if (roll <= 35) newQuest.tier2Rule = easyQuests[Random.Range(0, easyQuests.Length)];
                else if (roll <= 70) newQuest.tier2Rule = mediumQuests[Random.Range(0, mediumQuests.Length)];
                else newQuest.tier2Rule = hardQuests[Random.Range(0, hardQuests.Length)];
            }
        } while (newQuest.tier2Rule.ToString() == "No_Letter_" + newQuest.targetLetter || IsUnfairCombo(newQuest.targetLetter, newQuest.tier2Rule));

        // --- 3. TIER 3: The Build-Up Feature (Ultimate Boss Turn) ---
        if (currentTurn > 0 && currentTurn % 5 == 0)
        {
            Debug.Log("<color=cyan>TURN 5 REACHED: BOSS MECHANIC TRIGGERED!</color>");
            
            Tier3Type[] bossMechanics = { Tier3Type.FlawlessCasting, Tier3Type.SpeedCasting, Tier3Type.DoubleCast, Tier3Type.BlindCasting };
            newQuest.tier3Rule = bossMechanics[Random.Range(0, bossMechanics.Length)];
            
            // Force a medium/hard base word, but REROLL if it clashes with our prefix!
            do 
            {
                if (isTrickyLetter) newQuest.tier2Rule = easyQuests[Random.Range(0, easyQuests.Length)];
                else newQuest.tier2Rule = mediumQuests[Random.Range(0, mediumQuests.Length)];
            } while (newQuest.tier2Rule.ToString() == "No_Letter_" + newQuest.targetLetter || IsUnfairCombo(newQuest.targetLetter, newQuest.tier2Rule));
            
            switch (newQuest.tier3Rule)
            {
                case Tier3Type.FlawlessCasting: newQuest.tier3Description = "ULTIMATE: Flawless (No Backspace!)"; break;
                case Tier3Type.SpeedCasting: newQuest.tier3Description = "ULTIMATE: Speed (10 Seconds!)"; break;
                case Tier3Type.DoubleCast: newQuest.tier3Description = "ULTIMATE: Double Cast (2 Words!)"; break;
                case Tier3Type.BlindCasting: newQuest.tier3Description = "ULTIMATE: Blind (Hidden Text!)"; break;
            }
        }
        else
        {
            newQuest.tier3Rule = Tier3Type.None;
            newQuest.tier3Description = "";
        }

        // Translate the cleaned-up Enum list to readable text for the UI
        switch (newQuest.tier2Rule)
        {
            case Tier2Type.ExactLength_4: newQuest.tier2Description = "Exactly 4 letters"; break;
            case Tier2Type.ExactLength_5: newQuest.tier2Description = "Exactly 5 letters"; break;
            case Tier2Type.ExactLength_6: newQuest.tier2Description = "Exactly 6 letters"; break;
            case Tier2Type.ExactLength_7: newQuest.tier2Description = "Exactly 7 letters"; break;
            case Tier2Type.ExactLength_8: newQuest.tier2Description = "Exactly 8 letters"; break;
            case Tier2Type.MinLength_5: newQuest.tier2Description = "5 or more letters"; break;
            case Tier2Type.MinLength_6: newQuest.tier2Description = "6 or more letters"; break;
            case Tier2Type.MinLength_7: newQuest.tier2Description = "7 or more letters"; break;
            case Tier2Type.MinLength_8: newQuest.tier2Description = "8 or more letters"; break;
            
            case Tier2Type.EndsWith_S: newQuest.tier2Description = "Ends in -S or -ES"; break;
            case Tier2Type.EndsWith_ED: newQuest.tier2Description = "Ends in -D or -ED"; break;
            case Tier2Type.EndsWith_ER: newQuest.tier2Description = "Ends in -R or -ER"; break;
            case Tier2Type.EndsWith_ING: newQuest.tier2Description = "Ends in -ING"; break;
            case Tier2Type.EndsWith_LY: newQuest.tier2Description = "Ends in -LY"; break;
            case Tier2Type.EndsWith_Y: newQuest.tier2Description = "Ends in -Y"; break;
            case Tier2Type.EndsWith_T: newQuest.tier2Description = "Ends in -T"; break;
            case Tier2Type.EndsWith_N: newQuest.tier2Description = "Ends in -N"; break;
            case Tier2Type.EndsWith_E: newQuest.tier2Description = "Ends in -E"; break;
            case Tier2Type.EndsWith_TION: newQuest.tier2Description = "Ends in -TION"; break;

            case Tier2Type.No_Letter_A: newQuest.tier2Description = "Does NOT contain 'A'"; break;
            case Tier2Type.No_Letter_E: newQuest.tier2Description = "Does NOT contain 'E'"; break;
            case Tier2Type.No_Letter_I: newQuest.tier2Description = "Does NOT contain 'I'"; break;
            case Tier2Type.No_Letter_O: newQuest.tier2Description = "Does NOT contain 'O'"; break;
            case Tier2Type.No_Letter_U: newQuest.tier2Description = "Does NOT contain 'U'"; break;
            case Tier2Type.No_Letter_T: newQuest.tier2Description = "Does NOT contain 'T'"; break;
            case Tier2Type.No_Letter_S: newQuest.tier2Description = "Does NOT contain 'S'"; break;
            case Tier2Type.No_Letter_R: newQuest.tier2Description = "Does NOT contain 'R'"; break;
            case Tier2Type.No_Letter_N: newQuest.tier2Description = "Does NOT contain 'N'"; break;
            case Tier2Type.No_Letter_L: newQuest.tier2Description = "Does NOT contain 'L'"; break;
            case Tier2Type.No_Letter_P: newQuest.tier2Description = "Does NOT contain 'P'"; break;
            case Tier2Type.No_Letter_C: newQuest.tier2Description = "Does NOT contain 'C'"; break;
        }

        return newQuest;
    }

    // A dictionary of mathematically unfair "Linguistic Voids"
    private bool IsUnfairCombo(string prefix, Tier2Type rule)
    {
        // 1. The Double Suffix Trap: B, Y, W, K, SH, CH, and TH rarely end in 'TION'
        if (rule == Tier2Type.EndsWith_TION)
        {
            if (prefix == "B" || prefix == "Y" || prefix == "W" || prefix == "K" || 
                prefix == "SH" || prefix == "CH" || prefix == "TH") 
                return true;
        }
        
        // 2. The 'LY' Void: Vowels rarely start words that end in 'LY' (except E and U)
        if (rule == Tier2Type.EndsWith_LY && (prefix == "I" || prefix == "O")) return true;

        // 3. The 'Y' Bookend: Words starting with Y and ending with Y are incredibly rare (YUMMY, YEARLY)
        if (rule == Tier2Type.EndsWith_Y && prefix == "Y") return true;

        // --- THE CLUSTER PARADOXES ---
        // 4. You cannot ban a letter that is already inside the required starting prefix!
        if (rule == Tier2Type.No_Letter_S && (prefix == "SH" || prefix == "ST")) return true;
        if (rule == Tier2Type.No_Letter_C && prefix == "CH") return true;
        if (rule == Tier2Type.No_Letter_T && (prefix == "TH" || prefix == "ST")) return true;

        return false; // If it passes all checks, the combo is fair!
    }
}