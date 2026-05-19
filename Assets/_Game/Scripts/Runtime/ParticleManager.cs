using System;
using Unity.Netcode;
using UnityEngine;

public class ParticleManager : NetworkBehaviour
{

	public ParticleSystem[] particles;

	public static ParticleManager Instance { get; private set; }
	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

    public void PlayParticle(string particleName, Vector2 playPos)
    {
        PlayParticleEveryoneRpc(particleName, playPos, -1, ulong.MaxValue);
    }
    public void PlayParticle(string particleName, Vector2 playPos, float duration)
    {
        PlayParticleEveryoneRpc(particleName, playPos, duration, ulong.MaxValue);
    }

    public void PlayParticle(string particleName, Vector2 playPos, float duration, GameObject parent)
    {
        PlayParticleEveryoneRpc(particleName, playPos, duration, parent.GetComponent<NetworkObject>().NetworkObjectId);
    }
    public void PlayParticle(string particleName, Vector2 playPos, float duration, ulong parentNetworkId)
	{
		PlayParticleEveryoneRpc(particleName, playPos, duration, parentNetworkId);
	}

	private ParticleSystem GetParticle(string particleName)
	{
		foreach (var particle in particles)
		{
			if (particle.name == particleName)
			{
				return particle;
			}
		}

		throw new Exception("No such particle");
	}

	[Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
	public void PlayParticleEveryoneRpc(string particleName, Vector2 playPos, float duration, ulong parentNetworkId)
	{
		ParticleSystem particle = GetParticle(particleName);

		ParticleSystem instance = Instantiate(particle, playPos, Quaternion.identity);
		ParticleSystem.MainModule instanceModule = instance.main;

		instance.Stop();

		if(duration != -1)
		{
            instanceModule.duration = duration;
        }

		if (parentNetworkId != ulong.MaxValue)
		{
			bool found = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentNetworkId, out NetworkObject netObj);

			if (found)
			{
				instance.transform.SetParent(netObj.gameObject.transform);
			}
			else
			{
				throw new Exception("Particle parent not found");
			}
		}
		

		instance.Play();
	}

}
