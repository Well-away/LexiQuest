using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum Tier2Type 
{ 
    None, // NEW: Added for Arcane Bolts!
    ExactLength_4, ExactLength_5, ExactLength_6, ExactLength_7, ExactLength_8, ExactLength_9,
    MinLength_5, MinLength_6, MinLength_7, MinLength_8,
    EndsWith_S, EndsWith_ED, EndsWith_ER, EndsWith_ING, EndsWith_LY, 
    EndsWith_Y, EndsWith_T, EndsWith_N, EndsWith_E, EndsWith_TION,
    No_Letter_A, No_Letter_E, No_Letter_I, No_Letter_O, No_Letter_U,
    No_Letter_T, No_Letter_S, No_Letter_R, No_Letter_N, No_Letter_L,
    No_Letter_P, No_Letter_C
}

public enum Tier3Type { None, FlawlessCasting, SpeedCasting, DoubleCast, BlindCasting }

public enum SpellCategory { Offensive, Utility, Healing }

// THE FINAL SPELL LIST
public enum SpellType 
{ 
    None, 
    // Offensive
    MagicMissiles, WindBlast, FireBlast, FrostSpikes, EarthThrow, ThunderStrike, 
    // Utility
    LesserShield, FlameBarrier, ManaShield, Focus,
    // Healing
    LesserHeal, Purify, GreaterHeal
}

public class QuestData
{
    public string targetLetter; 
    public int targetLength; 
    public Tier2Type tier2Rule;
    public Tier3Type tier3Rule;
    public string tier1Description;
    public string tier2Description;
    public string tier3Description;
}

public class QuestManager : MonoBehaviour
{
    private List<string> group1 = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" };
    private List<string> group2 = new List<string> { "L", "M", "N", "O", "P", "R", "S", "T", "U", "W", "Y", "SH", "CH", "TH", "ST" }; 

    private List<string> currentActiveGroup;
    private List<string> remainingLetters;

    void Awake()
    {
        currentActiveGroup = new List<string>(group1);
        ResetRotation();
    }

    private void ResetRotation()
    {
        remainingLetters = new List<string>(currentActiveGroup);
        for (int i = 0; i < remainingLetters.Count; i++)
        {
            string temp = remainingLetters[i];
            int randomIndex = Random.Range(i, remainingLetters.Count);
            remainingLetters[i] = remainingLetters[randomIndex];
            remainingLetters[randomIndex] = temp;
        }
    }

    public QuestData GenerateQuest(int currentTurn, SpellType chosenSpell)
    {
        QuestData newQuest = new QuestData();

        if (remainingLetters.Count == 0)
        {
            currentActiveGroup = (currentActiveGroup == group1) ? group2 : group1;
            ResetRotation();
        }

        newQuest.targetLetter = remainingLetters[0];
        remainingLetters.RemoveAt(0);
        newQuest.tier1Description = "Word starting with '" + newQuest.targetLetter + "'";

        Tier2Type[] spellRules = GetSpellRules(chosenSpell);

        do 
        {
            newQuest.tier2Rule = spellRules[Random.Range(0, spellRules.Length)];
        } while (newQuest.tier2Rule.ToString() == "No_Letter_" + newQuest.targetLetter || IsUnfairCombo(newQuest.targetLetter, newQuest.tier2Rule));


        if (currentTurn > 0 && currentTurn % 5 == 0)
        {
            Tier3Type[] bossMechanics = { Tier3Type.FlawlessCasting, Tier3Type.SpeedCasting, Tier3Type.DoubleCast, Tier3Type.BlindCasting };
            newQuest.tier3Rule = bossMechanics[Random.Range(0, bossMechanics.Length)];
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

        newQuest.tier2Description = GetTier2Description(newQuest.tier2Rule);

        return newQuest;
    }

    private bool IsUnfairCombo(string prefix, Tier2Type rule)
    {
        if (rule == Tier2Type.EndsWith_TION)
        {
            if (prefix == "B" || prefix == "Y" || prefix == "W" || prefix == "K" || 
                prefix == "SH" || prefix == "CH" || prefix == "TH") 
                return true;
        }
        if (rule == Tier2Type.EndsWith_LY && (prefix == "I" || prefix == "O")) return true;
        if (rule == Tier2Type.EndsWith_Y && prefix == "Y") return true;
        if (rule == Tier2Type.No_Letter_S && (prefix == "SH" || prefix == "ST")) return true;
        if (rule == Tier2Type.No_Letter_C && prefix == "CH") return true;
        if (rule == Tier2Type.No_Letter_T && (prefix == "TH" || prefix == "ST")) return true;
        if (rule == Tier2Type.EndsWith_ER && prefix == "K") return true;

        // NEW: Goal 2 - Exclude J, K, and Y from 6+ Exact Length constraints!
        if (rule == Tier2Type.ExactLength_6 || rule == Tier2Type.ExactLength_7 || 
            rule == Tier2Type.ExactLength_8 || rule == Tier2Type.ExactLength_9)
        {
            if (prefix == "J" || prefix == "K" || prefix == "Y") return true;
        }

        return false; 
    }

    public Tier2Type[] GetSpellRules(SpellType chosenSpell)
    {
        switch (chosenSpell)
        {
            // OFFENSIVE
            case SpellType.MagicMissiles: return new[] { Tier2Type.None }; // Very Easy
            case SpellType.WindBlast: return new[] { Tier2Type.EndsWith_S, Tier2Type.EndsWith_ED, Tier2Type.ExactLength_5 }; // Easy
            case SpellType.FireBlast: return new[] { Tier2Type.EndsWith_ER, Tier2Type.EndsWith_ING, Tier2Type.ExactLength_6 }; // Medium
            case SpellType.FrostSpikes: return new[] { Tier2Type.No_Letter_A, Tier2Type.No_Letter_E, Tier2Type.ExactLength_7 }; // Hard
            case SpellType.EarthThrow: return new[] { Tier2Type.EndsWith_LY, Tier2Type.ExactLength_8, Tier2Type.No_Letter_I }; // Very Hard
            case SpellType.ThunderStrike: return new[] { Tier2Type.EndsWith_TION, Tier2Type.ExactLength_9, Tier2Type.MinLength_8 }; // Extreme
            
            // UTILITY
            case SpellType.LesserShield: return new[] { Tier2Type.MinLength_5, Tier2Type.ExactLength_5 }; // Easy
            case SpellType.FlameBarrier: return new[] { Tier2Type.ExactLength_6, Tier2Type.EndsWith_Y, Tier2Type.No_Letter_O }; // Medium
            case SpellType.ManaShield: return new[] { Tier2Type.ExactLength_8, Tier2Type.EndsWith_TION, Tier2Type.No_Letter_E }; // Hard
            case SpellType.Focus: return new[] { Tier2Type.ExactLength_8, Tier2Type.EndsWith_LY, Tier2Type.MinLength_8 }; // Hard

            // HEALING
            case SpellType.LesserHeal: return new[] { Tier2Type.MinLength_5, Tier2Type.ExactLength_5 }; // Easy
            case SpellType.Purify: return new[] { Tier2Type.ExactLength_6, Tier2Type.EndsWith_ING }; // Medium
            case SpellType.GreaterHeal: return new[] { Tier2Type.ExactLength_8, Tier2Type.EndsWith_TION, Tier2Type.EndsWith_LY }; // Hard

            default: return new[] { Tier2Type.MinLength_5 };
        }
    }

    public string GetTier2Description(Tier2Type rule)
    {
        switch (rule)
        {
            case Tier2Type.None: return "No secondary constraint";
            case Tier2Type.ExactLength_4: return "Exactly 4 letters";
            case Tier2Type.ExactLength_5: return "Exactly 5 letters";
            case Tier2Type.ExactLength_6: return "Exactly 6 letters";
            case Tier2Type.ExactLength_7: return "Exactly 7 letters";
            case Tier2Type.ExactLength_8: return "Exactly 8 letters";
            case Tier2Type.ExactLength_9: return "Exactly 9 letters";
            case Tier2Type.MinLength_5: return "5 or more letters";
            case Tier2Type.MinLength_6: return "6 or more letters";
            case Tier2Type.MinLength_7: return "7 or more letters";
            case Tier2Type.MinLength_8: return "8 or more letters";
            case Tier2Type.EndsWith_S: return "Ends in -S or -ES";
            case Tier2Type.EndsWith_ED: return "Ends in -D or -ED";
            case Tier2Type.EndsWith_ER: return "Ends in -R or -ER";
            case Tier2Type.EndsWith_ING: return "Ends in -ING";
            case Tier2Type.EndsWith_LY: return "Ends in -LY";
            case Tier2Type.EndsWith_Y: return "Ends in -Y";
            case Tier2Type.EndsWith_T: return "Ends in -T";
            case Tier2Type.EndsWith_N: return "Ends in -N";
            case Tier2Type.EndsWith_E: return "Ends in -E";
            case Tier2Type.EndsWith_TION: return "Ends in -TION";
            case Tier2Type.No_Letter_A: return "Does NOT contain 'A'";
            case Tier2Type.No_Letter_E: return "Does NOT contain 'E'";
            case Tier2Type.No_Letter_I: return "Does NOT contain 'I'";
            case Tier2Type.No_Letter_O: return "Does NOT contain 'O'";
            case Tier2Type.No_Letter_U: return "Does NOT contain 'U'";
            case Tier2Type.No_Letter_T: return "Does NOT contain 'T'";
            case Tier2Type.No_Letter_S: return "Does NOT contain 'S'";
            case Tier2Type.No_Letter_R: return "Does NOT contain 'R'";
            case Tier2Type.No_Letter_N: return "Does NOT contain 'N'";
            case Tier2Type.No_Letter_L: return "Does NOT contain 'L'";
            case Tier2Type.No_Letter_P: return "Does NOT contain 'P'";
            case Tier2Type.No_Letter_C: return "Does NOT contain 'C'";
            default: return "";
        }
    }

    public string RerollLetter(string currentLetter)
    {
        if (remainingLetters.Count == 0)
        {
            currentActiveGroup = (currentActiveGroup == group1) ? group2 : group1;
            ResetRotation();
        }

        string newLetter = remainingLetters[0];
        remainingLetters.RemoveAt(0);
        remainingLetters.Add(currentLetter); // Place the discarded letter back at the bottom
        return newLetter;
    }

    public Tier2Type RerollTier2Rule(SpellType chosenSpell, Tier2Type currentRule, string targetLetter)
    {
        Tier2Type[] spellRules = GetSpellRules(chosenSpell);
        List<Tier2Type> validRules = new List<Tier2Type>();

        foreach (var r in spellRules)
        {
            // Make sure the new rule is completely different, isn't blocking the new letter, and isn't a known unfair combo
            if (r != currentRule && r.ToString() != "No_Letter_" + targetLetter && !IsUnfairCombo(targetLetter, r))
            {
                validRules.Add(r);
            }
        }

        if (validRules.Count > 0)
        {
            return validRules[Random.Range(0, validRules.Count)];
        }
        
        // Fallback if no other valid rules exist for that spell!
        return currentRule;
    }
}