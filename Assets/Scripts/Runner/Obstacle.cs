using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the other collider belongs to the Player
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null || other.CompareTag("Player"))
        {
            RunnerGameManager.Instance.OnObstacleHit();
        }
    }
}
