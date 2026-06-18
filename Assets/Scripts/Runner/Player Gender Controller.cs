using System.Runtime.Serialization;
using UnityEngine;

public class PlayerGenderController : MonoBehaviour
{
    [SerializeField] private GameObject femalePlayer;
    [SerializeField] private GameObject malePlayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void selectFemale()
    {
        femalePlayer.SetActive(true);
        malePlayer.SetActive(false);
    }

    public void selectMale()
    {
        femalePlayer.SetActive(false);
        malePlayer.SetActive(true);
    }
}
