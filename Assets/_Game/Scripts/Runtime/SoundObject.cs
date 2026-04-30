using UnityEngine;

public class SoundObject : MonoBehaviour
{
    [SerializeField] private AudioSource source;

    public void PlaySound(AudioClip clip, float volume)
    {
        source.volume = volume;

        source.clip = clip;

        source.Play();

        float clipLength = source.clip.length;

        Invoke("DestroySelf", clipLength);
    }

    public void DestroySelf()
    {
        Debug.Log("Trying to destroy");
        Destroy(gameObject);
    }
}
