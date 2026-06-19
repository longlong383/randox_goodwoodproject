using System.Runtime.Serialization;
using UnityEngine;

/// <summary>
/// Drives the HUD HealthMeter: lights up meter segments based on a value and
/// rotates the needle to point at the corresponding spot on the arc.
///
/// Hierarchy expected under this GameObject:
///   meter1 .. meter10   (meter1 = lowest / red, meter10 = highest / green)
///   needle
///   heart               (decorative, untouched)
/// </summary>
public class HealthMeter : MonoBehaviour
{
    [Header("Meter Segments")]
    [Tooltip("Ordered lowest -> highest (meter1 = red, meter10 = green). " +
             "Leave empty to auto-find meter1..meter10 by name at Awake.")]
    [SerializeField] private GameObject[] meters = new GameObject[0];

    [Header("Needle")]
    [SerializeField] private Transform needle;
    [Tooltip("Local Z angle of the needle at value 0 (points to the red / meter1 side).")]
    [SerializeField] private float minAngle = 9f;
    [Tooltip("Local Z angle of the needle at max value (points to the green / meter10 side).")]
    [SerializeField] private float maxAngle = 32f;

    [Header("Range")]
    [SerializeField] private float maxValue = 50f;

    [Header("Smoothing (optional)")]
    [Tooltip("If on, the needle eases toward its target instead of snapping.")]
    [SerializeField] private bool smoothNeedle = true;
    [Tooltip("Needle rotation speed in degrees per second when smoothing is on.")]
    [SerializeField] private float needleSpeed = 360f;

    private float currentValue;
    private float targetAngle;

    private void Awake()
    {
        // Auto-wire the segments by name if they weren't assigned in the Inspector.
        if (meters == null || meters.Length == 0)
        {
            meters = new GameObject[10];
            for (int i = 0; i < meters.Length; i++)
            {
                Transform seg = transform.Find("meter" + (i + 1));
                if (seg != null) meters[i] = seg.gameObject;
            }
        }

        if (needle == null)
        {
            Transform n = transform.Find("needle");
            if (n != null) needle = n;
        }

        // Initialise the display to the starting value.
        SetValue(currentValue);
    }

    /// <summary>
    /// Public entry point. Pass the current health/score and the meter updates.
    /// Value is clamped to [0, maxValue].
    /// </summary>
    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, 0f, maxValue);

        UpdateSegments();
        UpdateNeedleTarget();

        // Snap immediately if smoothing is disabled.
        if (!smoothNeedle && needle != null)
            needle.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    /// <summary>Show every segment whose threshold has been reached; hide the rest.</summary>
    private void UpdateSegments()
    {
        if (meters == null || meters.Length == 0) return;

        // fill is 0..1; with 10 segments and maxValue 50 each segment is worth 5 points.
        float fill = currentValue / maxValue;
        int litCount = Mathf.CeilToInt(fill * meters.Length);

        for (int i = 0; i < meters.Length; i++)
        {
            if (meters[i] != null)
                meters[i].SetActive(i < litCount);
        }
    }

    /// <summary>Map the current fill fraction onto the needle's angle range.</summary>
    private void UpdateNeedleTarget()
    {
        float fill = currentValue / maxValue;
        targetAngle = Mathf.Lerp(minAngle, maxAngle, fill);
    }

    private void Update()
    {
        if (!smoothNeedle || needle == null) return;

        float z = needle.localEulerAngles.z;
        float newZ = Mathf.MoveTowardsAngle(z, targetAngle, needleSpeed * Time.deltaTime);
        needle.localRotation = Quaternion.Euler(0f, 0f, newZ);
    }
}