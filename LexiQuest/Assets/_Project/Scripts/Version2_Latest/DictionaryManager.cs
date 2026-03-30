using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DictionaryManager : MonoBehaviour
{
    private HashSet<string> validWords;
    private HashSet<string> bannedWords; // THESIS UPGRADE: The manual Blacklist!

    void Awake()
    {
        LoadDictionaries();
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