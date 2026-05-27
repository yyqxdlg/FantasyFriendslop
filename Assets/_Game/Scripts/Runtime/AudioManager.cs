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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        backgroundSongObject = Instantiate(soundObject, new Vector3(0, 0, 0), Quaternion.identity);
    }

	// note: if you want to play a sound with uniform sound across the map, make range float.MaxValue
	public void PlayRandomSound(string[] possibleClipNames, Vector2 playPos, float volume, float range)
	{
		PlaySound(possibleClipNames[UnityEngine.Random.Range(0, possibleClipNames.Length)], playPos, volume, range);
	}

    // note: if you want to play a sound with uniform sound across the map, make range float.MaxValue
    public void PlaySound(string clipName, Vector2 playPos, float volume, float range)
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("AudioManager is not spawned yet. Skipping sound: " + clipName);
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("NetworkManager is not listening. Skipping sound: " + clipName);
            return;
        }

        PlaySoundEveryoneRpc(clipName, playPos, volume, range);
    }

    public void PlaySound(string clipName, Vector2 playPos, float volume)
    {
        PlaySound(clipName, playPos, volume, 10f);
    }

    public void PlaySound(string clipName, Vector2 playPos)
    {
        PlaySound(clipName, playPos, 1f, 10f);
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

		throw new Exception("No such clip: " + clipName);
	}

	//there is one background song going on at a time (max). Use this to change what song is being played
	public void PlayBackgroundSong(string clipName, float volume)
	{
		PlayBackgroundSongRpc(clipName, volume);
	}

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayBackgroundSongRpc(string clipName, float volume)
    {
        Debug.Log("TEST 1");
        Debug.Log(backgroundSongObject);

        Debug.Log("TEST");

        AudioClip clip = GetSoundClip(clipName);

        float playVolume = volume * masterVolume;

        backgroundSongObject.GetComponent<SoundObject>().PlaySound(clip, playVolume, true);
    }

    /*
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayBackgroundSongRpc(string clipName, float volume)
	{
		Debug.Log("TEST");
		Debug.Log(backgroundSongObject);

        if (backgroundSongObject != null)
        {
            if (backgroundSongObject.clip.name == clipName) return;

            Destroy(backgroundSongObject);
			backgroundSongObject = null;
        }

        AudioSource source = Instantiate(soundObject, new Vector3(0, 0, 0), Quaternion.identity);

        AudioClip clip = GetSoundClip(clipName);

        float playVolume = volume * masterVolume;

        source.GetComponent<SoundObject>().PlaySound(clip, playVolume, true);

		backgroundSongObject = source;
    }
	*/

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
public void PlaySoundEveryoneRpc(string clipName, Vector2 playPos, float volume, float range)
{
    if (soundObject == null)
    {
        Debug.LogWarning("AudioManager soundObject is missing.");
        return;
    }

    AudioClip clip = null;

    foreach (var audioClip in audioClips)
    {
        if (audioClip != null && audioClip.name == clipName)
        {
            clip = audioClip;
            break;
        }
    }

    if (clip == null)
    {
        Debug.LogWarning("No such clip: " + clipName);
        return;
    }

    AudioSource source = Instantiate(soundObject, playPos, Quaternion.identity);

    float playVolume = volume * masterVolume;

    if (range != float.MaxValue && player != null)
    {
        float distToPlayer = ((Vector2)player.transform.position - playPos).magnitude;

        if (distToPlayer > 0.5f)
        {
            float distT = 1 - (distToPlayer / range);
            if (distT < 0) distT = 0;
            playVolume = distT * playVolume;
        }
        else
        {
            playVolume *= 0.5f;
        }
    }

    source.GetComponent<SoundObject>().PlaySound(clip, playVolume, false);
}

}
