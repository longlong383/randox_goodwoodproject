using TMPro;
using UnityEngine;

public class YourScript : MonoBehaviour
{
    [SerializeField] private TMP_Text textField;

    [Tooltip("Pool of words. One is picked at random each time this virus spawns.")]
    
    private string[] wordBank =
    {
        "Anaemia",
        "Thalassaemia",
        "Iron Overload",
        "Familial Hypercholesterollaemia (FH)",
        "Lipoprotein(a)",
        "Cardiovascular Risk",
        "Atherosclerosis",
        "Metabolic Syndrome",
        "Insulin Resistance", 
        "Type 1 diabetes",
        "Iron Deficiency",
        "Vitamin B12 Deficiency",
        "Vitamin D Deficiency"
    };

    // Fires on Instantiate AND every time the object is re-enabled from the
    // obstacle pool (SetActive(true)), so each spawned virus gets a fresh word.
    void OnEnable()
    {
        AssignRandomWord();
    }

    void Start()
    {
        if (textField == null)
        {
            Debug.LogError("Text field is not assigned!");
        }
    }

    /// <summary>Picks a random word from the bank and shows it on the text field.</summary>
    public void AssignRandomWord()
    {
        if (textField == null || wordBank == null || wordBank.Length == 0)
        {
            return;
        }

        textField.text = wordBank[Random.Range(0, wordBank.Length)];
    }

    public void UpdateText(string newText)
    {
        textField.text = newText;
    }
}