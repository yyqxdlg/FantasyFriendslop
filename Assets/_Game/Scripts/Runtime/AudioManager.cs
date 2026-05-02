using Unity.Netcode;
using UnityEngine;

public class AudioManager : NetworkBehaviour
{

	[SerializeField] private AudioSource soundObject;

    public AudioClip[] audioClips;

	public GameObject player;

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
		PlaySound(possibleClipNames[Random.Range(0, possibleClipNames.Length)], playPos, volume, range);
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

		return null;
	}

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
	public void PlaySoundEveryoneRpc(string clipName, Vector2 playPos, float volume, float range)
	{
        AudioSource source = Instantiate(soundObject, playPos, Quaternion.identity);

        AudioClip clip = GetSoundClip(clipName);

		float distToPlayer = ((Vector2)player.transform.position - playPos).magnitude;

        float playVolume = volume;


        if (range != float.MaxValue)
        {
            float distT = 1 - (distToPlayer / range);

            if (distT < 0)
            {
                distT = 0;
            }

            playVolume = distT * volume;
        }

		source.GetComponent<SoundObject>().PlaySound(clip, playVolume);
    }

}
