using UnityEngine;

// Attach to any GameObject that should emit spatialized sound.
// Requires an AudioSource on the same GameObject.
[RequireComponent(typeof(AudioSource))]
public class SoundEmitter2D : MonoBehaviour
{
    [Header("Listener")]
    [Tooltip("Assign the local player's Transform. In a Netcode game, do this at runtime from the owning client.")]
    public Transform listenerTransform;

    [Header("Distance Falloff")]
    [Tooltip("Within this distance, volume is always 1.")]
    public float minDistance = 1f;

    [Tooltip("Beyond this distance, volume is 0.")]
    public float maxDistance = 15f;

    [Tooltip("Shape of the volume falloff. X = normalized distance (0..1), Y = volume (0..1).")]
    public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Performance")]
    [Tooltip("How often (in seconds) volume is recalculated. Lower = more responsive, higher = cheaper.")]
    // NOTE: If many emitters are active simultaneously this scales linearly. Consider skipping
    // emitters beyond a broad-range threshold before running the full distance + raycast checks.
    public float updateInterval = 0.05f;

    private AudioSource _audioSource;
    private float _timer;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        // Disable Unity's built-in 3D rolloff entirely — we handle volume in script.
        _audioSource.spatialBlend = 0f;
    }

    void Start()
    {
        // Fallback: if no listener was assigned (e.g. sing ayer or forgot to set it),
        // try to find the player by tag. In a Netcode game ign listenerTransform from
        // the local NetworkObject instead of relying on th 
        if (listenerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                listenerTransform = player.transform;
            else
                Debug.LogWarning($"[SoundEmitter2D] No list ransform set on {gameObject.name} and no 'Player' tag found.");
        }
    }

    void Update()
    {
        if (listenerTransform == null) return;

        _timer += Time.deltaTime;
        if (_timer < updateInterval) return;
        _timer = 0f;

        UpdateVolume();
    }

    void UpdateVolume()
    {
        float distance = Vector2.Distance(transform.position, listenerTransform.position);

        if (distance >= maxDistance)
        {
            _audioSource.volume = 0f;
            return;
        }

        if (distance <= minDistance)
        {
            _audioSource.volume = 1f;
            return;
        }

        // Normalize distance to 0..1 range between min and max, then sample the curve.
        float t = (distance - minDistance) / (maxDistance - minDistance);
        _audioSource.volume = falloffCurve.Evaluate(t);
    }

    // Called by SoundOccluder2D — keeps concerns separated without needing a manager.
    public AudioSource GetAudioSource() => _audioSource;
    public Transform GetListenerTransform() => listenerTransform;
}