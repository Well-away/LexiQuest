using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 50f;
    public float fadeSpeed = 1f;
    private TextMeshProUGUI textMesh;
    private Color textColor;

    public void Setup(string text, Color color)
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.color = color;
        textColor = color;
        
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        // Float up and fade out over time
        while (textColor.a > 0)
        {
            transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);
            textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;
            yield return null;
        }
        
        Destroy(gameObject); // Clean up the object once it's invisible!
    }
}