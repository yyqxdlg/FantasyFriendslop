using UnityEngine;

// Attach alongside SoundEmitter2D. Requires AudioLowPassFilter on the same GameObject.
// Raycasts to the listener each tick and muffles audio based on how many walls are in the way.
[RequireComponent(typeof(SoundEmitter2D))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class SoundOccluder2D : MonoBehaviour
{
    [Header("Occlusion")]
    [Tooltip("Layer(s) that count as walls. Create a 'Wall' layer and assign your tilemap to it.")]
    public LayerMask occlusionLayerMask;

    [Tooltip("Cutoff frequency (Hz) with no walls between emitter and listener. 22000 = no filtering.")]
    public float cutoffOpen = 22000f;

    [Tooltip("Cutoff frequency (Hz) when 1 wall is blocking the sound.")]
    public float cutoffOneWall = 800f;

    [Tooltip("Cutoff frequency (Hz) when 2 or more walls are blocking the sound.")]
    public float cutoffManyWalls = 300f;

    [Header("Performance")]
    [Tooltip("Should match or be a multiple of SoundEmitter2D's updateInterval.")]
    public float updateInterval = 0.05f;

    private SoundEmitter2D _emitter;
    private AudioLowPassFilter _lowPass;
    private Collider2D _ownCollider; // cached so we can ignore it in raycasts
    private float _timer;

    void Awake()
    {
        _emitter = GetComponent<SoundEmitter2D>();
        _lowPass = GetComponent<AudioLowPassFilter>();
        _ownCollider = GetComponent<Collider2D>(); // may be null if emitter has no collider
    }

    void Update()
    {
        if (_emitter.GetListenerTransform() == null) return;

        _timer += Time.deltaTime;
        if (_timer < updateInterval) return;
        _timer = 0f;

        UpdateOcclusion();
    }

    void UpdateOcclusion()
    {
        Transform listener = _emitter.GetListenerTransform();

        Vector2 origin = transform.position;
        Vector2 target = listener.position;
        Vector2 direction = target - origin;
        float distance = direction.magnitude;

        // Cast through everything on the wall layer between emitter and listener.
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction.normalized, distance, occlusionLayerMask);

        int wallCount = 0;
        foreach (RaycastHit2D hit in hits)
        {
            // Skip the emitter's own collider if it has one.
            if (_ownCollider != null && hit.collider == _ownCollider) continue;
            wallCount++;
        }

        // Map wall count to a low-pass cutoff frequency.
        if (wallCount == 0)
            _lowPass.cutoffFrequency = cutoffOpen;
        else if (wallCount == 1)
            _lowPass.cutoffFrequency = cutoffOneWall;
        else
            _lowPass.cutoffFrequency = cutoffManyWalls;
    }
}