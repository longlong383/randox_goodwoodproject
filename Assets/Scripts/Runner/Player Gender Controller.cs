using System.Runtime.Serialization;
using UnityEngine;

public class PlayerGenderController : MonoBehaviour
{
    [SerializeField] private GameObject femalePlayer;
    [SerializeField] private GameObject malePlayer;
    [SerializeField] private GameObject nonBinaryPlayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void selectFemale()
    {
        femalePlayer.SetActive(true);
        malePlayer.SetActive(false);
        nonBinaryPlayer.SetActive(false);
    }

    public void selectMale()
    {
        femalePlayer.SetActive(false);
        malePlayer.SetActive(true);
        nonBinaryPlayer.SetActive(false);
    }

    public void selectNonBinary()
    {
        femalePlayer.SetActive(false);
        malePlayer.SetActive(false);
        nonBinaryPlayer.SetActive(true);
    }
}
