using UnityEngine;

public class ParticleScript : MonoBehaviour
{
    private ParticleSystem _system;
    public void Awake()
    {
        _system = GetComponent<ParticleSystem>();
        _system.Stop();
    }

    public void Play()
    {
        Debug.Log("PLAY");
        _system.Play();

        var emission = _system.emission;
        emission.enabled = true;
    }

    public void Stop()
    {
        Debug.Log("STOP");
        _system.Stop();

        var emission = _system.emission;
        emission.enabled = false;
    }
}
