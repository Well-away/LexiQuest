using UnityEngine;
using System.Collections.Generic;

public class DictionaryManager : MonoBehaviour
{
    // A HashSet is insanely fast for checking if something exists
    private HashSet<string> validWords;

    void Awake()
    {
        LoadDictionary();
    }

    private void LoadDictionary()
    {
        validWords = new HashSet<string>();
        
        // 1. Load the text file from the Resources folder (do not include the .txt extension here)
        TextAsset dictionaryAsset = Resources.Load<TextAsset>("words_alpha");

        if (dictionaryAsset == null)
        {
            Debug.LogError("Dictionary file not found! Make sure it's inside the 'Resources' folder and named 'words_alpha'.");
            return;
        }

        // 2. Split the giant block of text into individual lines
        string[] lines = dictionaryAsset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 3. Filter and save the words
        foreach (string line in lines)
        {
            // Remove any accidental spaces and force it to match our game's ALL CAPS style
            string cleanWord = line.Trim().ToUpper();
            
            // THESIS FILTER: Only load words that are 3 letters or longer!
            if (cleanWord.Length >= 3)
            {
                validWords.Add(cleanWord);
            }
        }

        Debug.Log($"<color=cyan>Dictionary Loaded: {validWords.Count} valid English words ready!</color>");
    }

    // Other scripts will call this one function to ask the Judge if a word is real
    public bool IsValidWord(string word)
    {
        if (validWords == null) return false;
        
        return validWords.Contains(word.ToUpper());
    }
}