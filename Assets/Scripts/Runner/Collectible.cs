using UnityEngine;

public class Collectible : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the other collider belongs to the Player
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null || other.CompareTag("Player"))
        {
            RunnerGameManager.Instance.OnCollectibleCollected(this);
        }
    }
}
