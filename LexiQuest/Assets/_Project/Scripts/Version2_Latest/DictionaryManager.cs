using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DictionaryManager : MonoBehaviour
{
    [Header("Global Discovery Memory")]
    public int globalTurnCount = 0;
    private Dictionary<string, int> wordDiscoveryMemory = new Dictionary<string, int>();

    private HashSet<string> validWords;
    private HashSet<string> bannedWords; // THESIS UPGRADE: The manual Blacklist!

    void Awake()
    {
        LoadDictionaries();
        LoadDiscoveryMemory(); // THESIS: Load the player's global vocabulary memory!
    }

    // ==========================================
    // WORD DISCOVERY BONUS SYSTEM
    // ==========================================
    public float RegisterWordAndGetBonus(string word)
    {
        string upperWord = word.ToUpper();
        float bonus = 1.0f;

        // Check if it's the very first time, OR if 50 turns have passed!
        if (!wordDiscoveryMemory.ContainsKey(upperWord))
        {
            bonus = 1.20f;
        }
        else if (globalTurnCount - wordDiscoveryMemory[upperWord] >= 50)
        {
            bonus = 1.20f;
        }

        // Record the exact global turn this word was just used on
        wordDiscoveryMemory[upperWord] = globalTurnCount;
        
        // Save to device hard drive immediately so it persists across stages
        SaveDiscoveryMemory(); 
        
        return bonus;
    }

    private void SaveDiscoveryMemory()
    {
        List<string> entries = new List<string>();
        foreach (var kvp in wordDiscoveryMemory)
        {
            entries.Add(kvp.Key + ":" + kvp.Value); // Format: "WORD:TURN"
        }
        
        PlayerPrefs.SetString("LexiQuest_WordMemory", string.Join(",", entries));
        PlayerPrefs.SetInt("LexiQuest_GlobalTurns", globalTurnCount);
        PlayerPrefs.Save();
    }

    private void LoadDiscoveryMemory()
    {
        globalTurnCount = PlayerPrefs.GetInt("LexiQuest_GlobalTurns", 0);
        string data = PlayerPrefs.GetString("LexiQuest_WordMemory", "");
        
        wordDiscoveryMemory.Clear();
        if (!string.IsNullOrEmpty(data))
        {
            string[] entries = data.Split(',');
            foreach (string entry in entries)
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int turnUsed))
                {
                    wordDiscoveryMemory[parts[0]] = turnUsed;
                }
            }
        }
        Debug.Log($"<color=cyan>Global Memory Loaded: {wordDiscoveryMemory.Count} words discovered over {globalTurnCount} total turns.</color>");
    }

    private void LoadDictionaries()
    {
        validWords = new HashSet<string>();
        bannedWords = new HashSet<string>();
        
        // 1. LOAD THE BAN LIST FIRST
        TextAsset banAsset = Resources.Load<TextAsset>("banned_words");
        if (banAsset != null)
        {
            string[] banLines = banAsset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in banLines)
            {
                bannedWords.Add(line.Trim().ToUpper());
            }
            Debug.Log($"<color=orange>Blacklist Loaded: {bannedWords.Count} regional/obscure words banned.</color>");
        }

        // 2. LOAD THE ENABLE DICTIONARY
        TextAsset dictionaryAsset = Resources.Load<TextAsset>("enable_words");
        if (dictionaryAsset != null)
        {
            string[] lines = dictionaryAsset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string cleanWord = line.Trim().ToUpper();
                
                // Only add the word if it is 3+ letters AND it is NOT in our Blacklist!
                if (cleanWord.Length >= 3 && !bannedWords.Contains(cleanWord))
                {
                    validWords.Add(cleanWord);
                }
            }
            Debug.Log($"<color=cyan>ENABLE Dictionary Loaded: {validWords.Count} professional English words ready!</color>");
        }
    }

    public bool IsValidWord(string word)
    {
        if (validWords == null) return false;
        return validWords.Contains(word.ToUpper());
    }

    public List<string> GetClutchSuggestions(string prefix, Tier2Type rule, List<string> blockedWords, int count = 2)
    {
        List<string> matches = new List<string>();
        if (validWords == null) return matches;

        foreach (string word in validWords)
        {
            if (word.Length >= 4 && word.Length <= 9 && 
                word.StartsWith(prefix) && 
                QuestValidator.CheckTier2(word, rule) && 
                !blockedWords.Contains(word))
            {
                matches.Add(word);
            }
        }

        System.Random rng = new System.Random();
        return matches.OrderBy(w => rng.Next()).Take(count).ToList();
    }
}