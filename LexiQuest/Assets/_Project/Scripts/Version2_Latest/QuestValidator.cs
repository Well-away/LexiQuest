using UnityEngine;
using System.Text.RegularExpressions;

public static class QuestValidator
{
    // We pass the Tier2Type rule directly now
    public static bool CheckTier2(string word, Tier2Type rule) 
    {
        int len = word.Length;
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
            case Tier2Type.MaxLength_5: return len <= 5;
            case Tier2Type.MaxLength_6: return len <= 6;
            case Tier2Type.MaxLength_7: return len <= 7;
            case Tier2Type.EvenLength: return len > 0 && len % 2 == 0;
            case Tier2Type.OddLength: return len > 0 && len % 2 != 0;
            
            // --- SUFFIXES ---
            case Tier2Type.EndsWith_S: return word.EndsWith("S");
            case Tier2Type.EndsWith_ED: return word.EndsWith("D"); // Covers D and ED
            case Tier2Type.EndsWith_ER: return word.EndsWith("R"); // Covers R and ER
            case Tier2Type.EndsWith_ING: return word.EndsWith("ING");
            case Tier2Type.EndsWith_LY: return word.EndsWith("LY");
            case Tier2Type.EndsWith_Y: return word.EndsWith("Y");
            case Tier2Type.EndsWith_T: return word.EndsWith("T");
            case Tier2Type.EndsWith_N: return word.EndsWith("N");
            case Tier2Type.EndsWith_E: return word.EndsWith("E");
            case Tier2Type.EndsWith_TION: return word.EndsWith("TION");
            
            // --- INCLUSIONS ---
            case Tier2Type.Contains_DoubleLetter: 
                return Regex.IsMatch(word, @"(.)\1"); 
            case Tier2Type.Contains_TH: return word.Contains("TH");
            case Tier2Type.Contains_CH: return word.Contains("CH");
            case Tier2Type.Contains_SH: return word.Contains("SH");
            case Tier2Type.Contains_ST: return word.Contains("ST");
            case Tier2Type.Contains_CK: return word.Contains("CK");
            case Tier2Type.Contains_EA: return word.Contains("EA");
            case Tier2Type.Contains_EE: return word.Contains("EE");
            case Tier2Type.Contains_OO: return word.Contains("OO");
            case Tier2Type.Contains_OU: return word.Contains("OU");
            
            // --- EXCLUSIONS ---
            case Tier2Type.No_Letter_A: return !word.Contains("A");
            case Tier2Type.No_Letter_E: return !word.Contains("E");
            case Tier2Type.No_Letter_I: return !word.Contains("I");
            case Tier2Type.No_Letter_O: return !word.Contains("O");
            case Tier2Type.No_Letter_U: return !word.Contains("U");
            case Tier2Type.No_Letter_T: return !word.Contains("T");
            case Tier2Type.No_Letter_S: return !word.Contains("S");
            case Tier2Type.No_Letter_R: return !word.Contains("R");
            case Tier2Type.No_Letter_N: return !word.Contains("N");
            case Tier2Type.No_Letter_L: return !word.Contains("L");
            case Tier2Type.No_Letter_P: return !word.Contains("P");
            case Tier2Type.No_Letter_C: return !word.Contains("C");
            
            default: return false;
        }
    }
}