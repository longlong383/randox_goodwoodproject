using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Drives the HUD HealthMeter: lights up meter segments based on a value and
/// rotates the needle to point at the corresponding spot on the arc.
///
/// Hierarchy expected under this GameObject:
///   meter1 .. meter10   (meter1 = lowest / red, meter10 = highest / green)
///   needle
///   heart               (decorative, untouched)
/// </summary>
public class HealthMeterFinal : MonoBehaviour
{
    [Header("Meter Segments")]
    [Tooltip("Ordered lowest -> highest meter1 thru meter4 " +
             "Leave empty to auto-find meter1...meter4 by name at Awake.")]
    [SerializeField] private GameObject[] meters = new GameObject[5];

    [Header("Needle")]
    [SerializeField] private GameObject needle;
    [Tooltip("Local Z angle of the needle at value 0 (points to the red / meter1 side).")]
    [SerializeField] private float minAngle = -90f;
    [Tooltip("Local Z angle of the needle at max value (points to the green / meter10 side).")]
    [SerializeField] private float maxAngle = 90f;

    [Header("Range")]
    [SerializeField] private float maxValue = 5f;

    [Header("Smoothing (optional)")]
    [Tooltip("If on, the needle eases toward its target instead of snapping.")]
    [SerializeField] private bool smoothNeedle = true;
    [Tooltip("Needle rotation speed in degrees per second when smoothing is on.")]
    [SerializeField] private float needleSpeed = 360f;

    private float currentValue = 5f;

    private float targetAngle;

    private int maxIndex = 0;
    private void Awake()
    {
        foreach (Transform child in transform)
        {
            if (child.name.Contains("meter"))
                maxIndex++;
        }
        Debug.Log("Max index: " + maxIndex);
        // Auto-wire the segments by name if they weren't assigned in the Inspector.
        if (meters == null || meters.Length == 0)
        {
            meters = new GameObject[maxIndex];
            for (int i = 0; i < meters.Length; i++)
            {
                Transform seg = transform.Find("meter" + (i + 1));
                if (seg != null) meters[i] = seg.gameObject;
            }
        }


        if (needle == null)
        {
            GameObject n = GameObject.Find("needle");
            if (n != null) needle = n;
            else Debug.LogWarning("needle not found!!");
        }
        Debug.Log("needle status: " + needle);
        // Initialise the display to the starting value.
        SetValue(currentValue);
        for (int i = 0; i < meters.Length; i++)
        {
            if (meters[i] != null)
                meters[i].SetActive(false);
        }
        if (needle != null)
            needle.SetActive(false);

    }

    /// <summary>
    /// Public entry point. Pass the current health/score and the meter updates.
    /// Value is clamped to [0, maxValue].
    /// </summary>
    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, 0f, maxValue);
        Debug.Log("HealthMeterController: currentValue after clamping: " + currentValue);
        UpdateSegments();
        UpdateNeedleTarget();

        // Snap immediately if smoothing is disabled.
        if (!smoothNeedle && needle != null)
        {
            Debug.Log("needle status: " + needle);
            needle.transform.localRotation = Quaternion.Euler(0f, 0f, -targetAngle);
        }
    }

    /// <summary>Show every segment whose threshold has been reached; hide the rest.</summary>
    private void UpdateSegments()
    {
        if (meters == null || meters.Length == 0) return;

        float fill = currentValue / maxValue;
        int litCount = Mathf.CeilToInt(fill * meters.Length);

        for (int i = 0; i < meters.Length; i++)
        {
            if (meters[i] == null) continue;

            Image image = meters[i].GetComponent<Image>();
            if (image == null) continue;

            Color color = image.color;

            if (i >= litCount)
            {
                // Empty segments
                color.a = 0f;
            }
            else
            {
                // Fade older segments
                float t = (float)(i + 1) / litCount;
                float alpha = Mathf.Lerp(0.01f, 1f, t);
                color.a = alpha;
            }

            image.color = color;
        }
    }
    public void Activate()
    {
        for (int i = 0; i < meters.Length; i++)
        {
            if (meters[i] != null)
                meters[i].SetActive(true);
        }
        if (needle != null)
            needle.SetActive(true);



    }
    public void Deactivate()
    {
        for (int i = 0; i < meters.Length; i++)
        {
            if (meters[i] != null)
                meters[i].SetActive(false);
        }
        if (needle != null)
            needle.SetActive(false);


    }
    /// <summary>Map the current fill fraction onto the needle's angle range.</summary>
    private void UpdateNeedleTarget()
    {
        float fill = currentValue / maxValue;
        Debug.Log("currentValue: " + currentValue + ", maxValue: " + maxValue);
        Debug.Log("HealthMeterController: fill: " + fill);
        targetAngle = Mathf.Lerp(minAngle, maxAngle, fill);
        needle.transform.localRotation = Quaternion.Euler(0f, 0f, -targetAngle);
        // Debug.Log("HealthMeterController: targetAngle: " + targetAngle);

    }

    private void Update()
    {

    }
}