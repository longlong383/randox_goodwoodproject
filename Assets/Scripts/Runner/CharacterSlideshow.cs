using System.Collections;
using UnityEngine;

public class CharacterSlideshow : MonoBehaviour
{
    [Tooltip("Assign your 3 original characters here.")]
    public Transform[] originalCharacters = new Transform[3];

    public float slideDuration = 0.5f;
    public float waitTime = 2.0f;
    public bool playOnAwake = true; // Set to false if you want it hidden by default

    // Based on your provided coordinates: 514.48, 516.48, 518.48
    private float startX = -2f;
    private float spacing = 2.0f;

    private Transform[] carouselCharacters = new Transform[6];
    private bool isPlaying = false;
    private bool isInitialized = false;

    private void Start()
    {
        // 1. Setup the Conveyor Belt
        for (int i = 0; i < 3; i++)
        {
            carouselCharacters[i] = originalCharacters[i];

            GameObject clone = Instantiate(originalCharacters[i].gameObject, originalCharacters[i].parent);
            clone.name = originalCharacters[i].name + "_Clone";
            carouselCharacters[i + 3] = clone.transform;
        }

        // 2. Space them all out
        for (int i = 0; i < 6; i++)
        {
            Vector3 pos = carouselCharacters[i].position;
            pos.x = startX + (i * spacing);
            carouselCharacters[i].position = pos;
        }

        isInitialized = true;

        StartCoroutine(SlideshowRoutine());

        // 3. Handle initial state
        if (playOnAwake)
        {
            ShowAndStart();
        }
        else
        {
            StopAndHide();
        }
    }

    /// <summary>
    /// Call this from your UI Buttons or Game Manager to display the players and start sliding
    /// </summary>
    public void ShowAndStart()
    {
        if (!isInitialized) return;

        isPlaying = true;
        ToggleCharacterVisibility(true);
    }

    /// <summary>
    /// Call this from your UI Buttons or Game Manager to hide the players and pause sliding
    /// </summary>
    public void StopAndHide()
    {
        if (!isInitialized) return;

        isPlaying = false;
        ToggleCharacterVisibility(false);
    }

    private void ToggleCharacterVisibility(bool isVisible)
    {
        for (int i = 0; i < carouselCharacters.Length; i++)
        {
            if (carouselCharacters[i] != null)
            {
                carouselCharacters[i].gameObject.SetActive(isVisible);
            }
        }
    }

    private IEnumerator SlideshowRoutine()
    {
        float wrapThreshold = startX - (spacing * 0.5f);
        float wrapDistance = 6 * spacing;

        while (true)
        {
            // 1. Custom Timer Loop
            // This pauses the countdown if isPlaying is false, rather than breaking the Coroutine entirely
            float timer = 0f;
            while (timer < waitTime)
            {
                if (isPlaying)
                {
                    timer += Time.deltaTime;
                }
                yield return null;
            }

            // 2. Setup Slide Targets
            Vector3[] startPositions = new Vector3[6];
            Vector3[] endPositions = new Vector3[6];

            for (int i = 0; i < 6; i++)
            {
                startPositions[i] = carouselCharacters[i].position;
                endPositions[i] = startPositions[i] - new Vector3(spacing, 0, 0);
            }

            float elapsedTime = 0f;

            // 3. Execute the Slide
            while (elapsedTime < slideDuration)
            {
                // Notice we still advance elapsedTime even if isPlaying becomes false mid-slide.
                // This guarantees they finish their movement mathematically while invisible.
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / slideDuration;
                t = t * t * (3f - 2f * t);

                for (int i = 0; i < 6; i++)
                {
                    carouselCharacters[i].position = Vector3.Lerp(startPositions[i], endPositions[i], t);
                }

                yield return null;
            }

            // 4. Snap to Grid & Wrap Around
            for (int i = 0; i < 6; i++)
            {
                Vector3 finalPos = endPositions[i];

                if (finalPos.x < wrapThreshold)
                {
                    finalPos.x += wrapDistance;
                }

                carouselCharacters[i].position = finalPos;
            }
        }
    }
}