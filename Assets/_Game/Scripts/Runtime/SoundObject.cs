using UnityEngine;

public class SoundObject : MonoBehaviour
{
    [SerializeField] private AudioSource source;

    public void PlaySound(AudioClip clip, float volume, bool repeat)
    {
        source.volume = volume;

        source.clip = clip;

        source.Play();

        source.loop = repeat;

        float clipLength = source.clip.length;

        if (!repeat)
        {
            Invoke("DestroySelf", clipLength);
        }
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
