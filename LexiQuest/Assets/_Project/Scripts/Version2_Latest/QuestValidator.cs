using UnityEngine;
using System.Text.RegularExpressions;

public static class QuestValidator
{
    // We pass the Tier2Type rule directly from the QuestManager
    public static bool CheckTier2(string word, Tier2Type rule) 
    {
        // Safety check to prevent null errors
        if (string.IsNullOrEmpty(word)) return false;

        int len = word.Length;
        string upperWord = word.ToUpper();

        switch (rule)
        {
            // --- SHAPES & SIZES ---
            case Tier2Type.ExactLength_4: return len == 4;
            case Tier2Type.ExactLength_5: return len == 5;
            case Tier2Type.ExactLength_6: return len == 6;
            case Tier2Type.ExactLength_7: return len == 7;
            case Tier2Type.ExactLength_8: return len == 8;
            case Tier2Type.MinLength_5: return len >= 5;
            case Tier2Type.MinLength_6: return len >= 6;
            case Tier2Type.MinLength_7: return len >= 7;
            case Tier2Type.MinLength_8: return len >= 8;
            
            // --- SUFFIXES ---
            case Tier2Type.EndsWith_S: return upperWord.EndsWith("S");
            case Tier2Type.EndsWith_ED: return upperWord.EndsWith("D"); // Mathematically covers D and ED
            case Tier2Type.EndsWith_ER: return upperWord.EndsWith("R"); // Mathematically covers R and ER
            case Tier2Type.EndsWith_ING: return upperWord.EndsWith("ING");
            case Tier2Type.EndsWith_LY: return upperWord.EndsWith("LY");
            case Tier2Type.EndsWith_Y: return upperWord.EndsWith("Y");
            case Tier2Type.EndsWith_T: return upperWord.EndsWith("T");
            case Tier2Type.EndsWith_N: return upperWord.EndsWith("N");
            case Tier2Type.EndsWith_E: return upperWord.EndsWith("E");
            case Tier2Type.EndsWith_TION: return upperWord.EndsWith("TION");
            
            // --- EXCLUSIONS ---
            case Tier2Type.No_Letter_A: return !upperWord.Contains("A");
            case Tier2Type.No_Letter_E: return !upperWord.Contains("E");
            case Tier2Type.No_Letter_I: return !upperWord.Contains("I");
            case Tier2Type.No_Letter_O: return !upperWord.Contains("O");
            case Tier2Type.No_Letter_U: return !upperWord.Contains("U");
            case Tier2Type.No_Letter_T: return !upperWord.Contains("T");
            case Tier2Type.No_Letter_S: return !upperWord.Contains("S");
            case Tier2Type.No_Letter_R: return !upperWord.Contains("R");
            case Tier2Type.No_Letter_N: return !upperWord.Contains("N");
            case Tier2Type.No_Letter_L: return !upperWord.Contains("L");
            case Tier2Type.No_Letter_P: return !upperWord.Contains("P");
            case Tier2Type.No_Letter_C: return !upperWord.Contains("C");

            // Failsafe
            default: return true;
        }
    }
}