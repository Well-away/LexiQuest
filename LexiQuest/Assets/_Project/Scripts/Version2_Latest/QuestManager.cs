using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum Tier2Type 
{ 
    ExactLength_4, ExactLength_5, ExactLength_6, ExactLength_7, ExactLength_8,
    MinLength_5, MinLength_6, MinLength_7, MinLength_8,
    EndsWith_S, EndsWith_ED, EndsWith_ER, EndsWith_ING, EndsWith_LY, 
    EndsWith_Y, EndsWith_T, EndsWith_N, EndsWith_E, EndsWith_TION,
    No_Letter_A, No_Letter_E, No_Letter_I, No_Letter_O, No_Letter_U,
    No_Letter_T, No_Letter_S, No_Letter_R, No_Letter_N, No_Letter_L,
    No_Letter_P, No_Letter_C
}

public enum Tier3Type { None, FlawlessCasting, SpeedCasting, DoubleCast, BlindCasting }

public enum SpellCategory { Offensive, Utility, Healing }

// NEW: All 15 Spells added to the Enum!
public enum SpellType 
{ 
    None, 
    // Offensive
    FlameBlast, FrostSpikes, ThunderStrike, GaleBurst, EarthThrow, ArcaneBolts,
    // Utility
    WindVeil, FlameBarrier, ManaShield, Restraint,
    // Healing
    Revitalize, Cleanse, PurifyingFlames, WinterEmbrace, SoothingWaters
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

        // --- THE FULL SPELL CONSTRAINT POOL ---
        Tier2Type[] spellRules;
        switch (chosenSpell)
        {
            // OFFENSIVE
            case SpellType.FlameBlast: spellRules = new[] { Tier2Type.ExactLength_5, Tier2Type.ExactLength_6, Tier2Type.ExactLength_7, Tier2Type.ExactLength_8 }; break;
            case SpellType.FrostSpikes: spellRules = new[] { Tier2Type.No_Letter_T, Tier2Type.No_Letter_R, Tier2Type.No_Letter_S, Tier2Type.No_Letter_U }; break;
            case SpellType.ThunderStrike: spellRules = new[] { Tier2Type.MinLength_5, Tier2Type.MinLength_6, Tier2Type.MinLength_7, Tier2Type.MinLength_8 }; break;
            case SpellType.GaleBurst: spellRules = new[] { Tier2Type.EndsWith_S, Tier2Type.EndsWith_ED, Tier2Type.EndsWith_ER }; break;
            case SpellType.EarthThrow: spellRules = new[] { Tier2Type.No_Letter_U, Tier2Type.No_Letter_O, Tier2Type.No_Letter_C, Tier2Type.No_Letter_P }; break;
            case SpellType.ArcaneBolts: spellRules = new[] { Tier2Type.EndsWith_TION, Tier2Type.EndsWith_Y, Tier2Type.EndsWith_S, Tier2Type.EndsWith_ER, Tier2Type.EndsWith_LY }; break;
            
            // UTILITY
            case SpellType.WindVeil: spellRules = new[] { Tier2Type.No_Letter_A, Tier2Type.No_Letter_O, Tier2Type.No_Letter_P, Tier2Type.No_Letter_C, Tier2Type.No_Letter_N }; break;
            case SpellType.FlameBarrier: spellRules = new[] { Tier2Type.EndsWith_Y, Tier2Type.EndsWith_E, Tier2Type.EndsWith_LY, Tier2Type.EndsWith_N }; break;
            case SpellType.ManaShield: spellRules = new[] { Tier2Type.MinLength_5, Tier2Type.MinLength_6, Tier2Type.MinLength_7, Tier2Type.MinLength_8 }; break;
            case SpellType.Restraint: spellRules = new[] { Tier2Type.No_Letter_O, Tier2Type.No_Letter_U, Tier2Type.No_Letter_R, Tier2Type.No_Letter_S, Tier2Type.No_Letter_T, Tier2Type.No_Letter_L }; break;

            // HEALING
            case SpellType.Revitalize: spellRules = new[] { Tier2Type.EndsWith_S, Tier2Type.EndsWith_ING }; break;
            case SpellType.Cleanse: spellRules = new[] { Tier2Type.ExactLength_4, Tier2Type.ExactLength_5, Tier2Type.ExactLength_6 }; break;
            case SpellType.PurifyingFlames: spellRules = new[] { Tier2Type.No_Letter_A, Tier2Type.No_Letter_R, Tier2Type.No_Letter_N, Tier2Type.No_Letter_T }; break;
            case SpellType.WinterEmbrace: spellRules = new[] { Tier2Type.EndsWith_Y, Tier2Type.EndsWith_ED, Tier2Type.EndsWith_ER }; break;
            case SpellType.SoothingWaters: spellRules = new[] { Tier2Type.MinLength_5 }; break; // WAITING FOR YOUR CONSTRAINT!

            default: spellRules = new[] { Tier2Type.MinLength_5 }; break;
        }

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
        return false; 
    }
}