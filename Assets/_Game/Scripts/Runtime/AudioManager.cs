using System;
using Unity.Netcode;
using UnityEngine;

public class AudioManager : NetworkBehaviour
{

	[SerializeField] private AudioSource soundObject;

    public AudioClip[] audioClips;

	public GameObject player;

	public float masterVolume = 1f;

	private AudioSource backgroundSongObject;

    public static AudioManager Instance { get; private set; }
	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	// note: if you want to play a sound with uniform sound across the map, make range float.MaxValue
	public void PlayRandomSound(string[] possibleClipNames, Vector2 playPos, float volume, float range)
	{
		PlaySound(possibleClipNames[UnityEngine.Random.Range(0, possibleClipNames.Length)], playPos, volume, range);
	}

    // note: if you want to play a sound with uniform sound across the map, make range float.MaxValue
    public void PlaySound(string clipName, Vector2 playPos, float volume, float range)
	{
        PlaySoundEveryoneRpc(clipName, playPos, volume, range);
	}

	private AudioClip GetSoundClip(string clipName)
	{
		foreach (var clip in audioClips)
		{
			if(clip.name == clipName)
			{
				return clip;
			}
		}

		throw new Exception("No such clip");
	}

	//there is one background song going on at a time (max). Use this to change what song is being played
	public void PlayBackgroundSong(string clipName, float volume)
	{
		PlayBackgroundSongRpc(clipName, volume);
	}

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayBackgroundSongRpc(string clipName, float volume)
	{
        if (backgroundSongObject != null)
        {
            if (backgroundSongObject.clip.name == clipName) return;

            Destroy(backgroundSongObject);
        }

        AudioSource source = Instantiate(soundObject, new Vector3(0, 0, 0), Quaternion.identity);

        AudioClip clip = GetSoundClip(clipName);

        float playVolume = volume * masterVolume;

        source.GetComponent<SoundObject>().PlaySound(clip, playVolume, true);

		backgroundSongObject = source;
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
	public void PlaySoundEveryoneRpc(string clipName, Vector2 playPos, float volume, float range)
	{
        AudioSource source = Instantiate(soundObject, playPos, Quaternion.identity);

        AudioClip clip = GetSoundClip(clipName);

		float distToPlayer = ((Vector2)player.transform.position - playPos).magnitude;

        float playVolume = volume * masterVolume;


        if (range != float.MaxValue)
        {
            float distT = 1 - (distToPlayer / range);

            if (distT < 0)
            {
                distT = 0;
            }

            playVolume = distT * playVolume;
        }

		source.GetComponent<SoundObject>().PlaySound(clip, playVolume, false);
    }

}
