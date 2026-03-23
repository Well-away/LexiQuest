using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestData
{
    public char targetLetter;
    public int targetLength;
    public string tier3Description;
    public string tier3Type; // e.g., "vowels", "suffix", "consonant"
}

public class QuestManager : MonoBehaviour
{
    private List<char> group1 = new List<char> { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K' };
    private List<char> group2 = new List<char> { 'L', 'M', 'N', 'O', 'P', 'R', 'S', 'T', 'U', 'W', 'Y' }; // Excludes Q, V, X, Z
    
    private List<char> currentActiveGroup;
    private List<char> remainingLetters;

    void Awake()
    {
        // Initialize with Group 1
        currentActiveGroup = new List<char>(group1);
        ResetRotation();
    }

    private void ResetRotation()
    {
        remainingLetters = new List<char>(currentActiveGroup);
        // Shuffle the letters so they aren't in alphabetical order
        for (int i = 0; i < remainingLetters.Count; i++)
        {
            char temp = remainingLetters[i];
            int randomIndex = Random.Range(i, remainingLetters.Count);
            remainingLetters[i] = remainingLetters[randomIndex];
            remainingLetters[randomIndex] = temp;
        }
    }

    public QuestData GenerateQuest()
    {
        QuestData newQuest = new QuestData();

        // 1. TIER 1: Letter Rotation
        if (remainingLetters.Count == 0)
        {
            // Swap Groups
            currentActiveGroup = (currentActiveGroup == group1) ? group2 : group1;
            ResetRotation();
            Debug.Log("Swapping to next letter group!");
        }

        newQuest.targetLetter = remainingLetters[0];
        remainingLetters.RemoveAt(0);

        // 2. TIER 2: Exact Length (between 4 and 8)
        newQuest.targetLength = Random.Range(4, 9);

        // 3. TIER 3: Special Constraint
        string[] tier3Options = { "2+ Vowels", "Contains 'T' or 'R'", "Ends in 'S' or 'D'" };
        int randomIndex = Random.Range(0, tier3Options.Length);
        newQuest.tier3Description = tier3Options[randomIndex];
        newQuest.tier3Type = tier3Options[randomIndex]; // You can use this for logic later

        return newQuest;
    }
}