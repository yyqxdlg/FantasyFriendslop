using Unity.Netcode;
using UnityEngine;

public class AudioManager : NetworkBehaviour
{

	[SerializeField] private AudioSource soundObject;

    public AudioClip[] audioClips;

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

	public void PlayRandomSound(string[] possibleClipNames, Vector2 playPos, float volume)
	{
		PlaySound(possibleClipNames[Random.Range(0, possibleClipNames.Length)], playPos, volume);
	}
	public void PlaySound(string clipName, Vector2 playPos, float volume)
	{
        PlaySoundEveryoneRpc(clipName, playPos, volume);
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
	public void PlaySoundEveryoneRpc(string clipName, Vector2 playPos, float volume)
	{
        AudioSource source = Instantiate(soundObject, playPos, Quaternion.identity);

        AudioClip clip = GetSoundClip(clipName);

		source.GetComponent<SoundObject>().PlaySound(clip, volume);
    }

}
