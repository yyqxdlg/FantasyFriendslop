using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AmbushSkeleton : Spawnable
{
	[SerializeField] private ObjectListCollider colList;

    //[SerializeField] private ConstantCheckOLC clusterCollider;

    //[SerializeField] private List<AmbushSkeleton> cluster;


	[SerializeField] private float timerMax;
	private float timer;

	//public bool triggered;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if (!IsServer) return;

		//Invoke("MakeCluster", 1);

		timer = timerMax;

		//triggered = false;
	}

    /*
	private void MakeCluster()
	{
		Debug.Log("Run make cluster?");

        for (int i = 0; i < clusterCollider.GetNumberOfTargets(); i++)
        {
            GameObject target = clusterCollider.GetTarget(i);

            AmbushSkeleton targetAmbushScript = target.GetComponent<AmbushSkeleton>();

            Debug.Log("TARGET: ", target);

            if (targetAmbushScript != null)
            {
                AddAmbusherToCluster(targetAmbushScript);

                targetAmbushScript.AddAmbusherToCluster(this);
            }
        }

		//Destroy(clusterCollider);
    }

	public bool ContainsCheck(AmbushSkeleton target)
	{
		for (int i = 0; i < cluster.Count; i++)
		{
			if(cluster[i] == target)
			{
				return true;
			}
		}
		return false;
	}

	public void AddAmbusherToCluster(AmbushSkeleton ambusher)
	{
		Debug.Log("Add to cluster?");
		if (!ContainsCheck(ambusher))
		{
            cluster.Add(ambusher);
            Debug.Log("Yes");
        }
	}
	*/

    private int CountPlayers()
	{
		int output = 0;

        for (int i = 0; i < colList.GetNumberOfTargets(); i++)
        {
            GameObject curr = colList.GetTarget(i);

            if(curr.GetComponent<CharacterBasic>() != null)
            {
				output += 1;
            }
        }

		return output;
    }

	// Update is called once per frame
	void Update()
	{
        if (!IsServer) return;

		int playerCount = CountPlayers();

        if (playerCount == 1)
		{
			timer -= Time.deltaTime;
		} else
		{
            timer = timerMax;
        }

		if (timer < 0)
		{
			Arise();
		}
	}

	public void Arise()
	{
		//triggered = true;

		/*
		Debug.Log("Waking up cluster: " + cluster.Count);

		for (int i = 0; i < cluster.Count; i++)
		{
			if (cluster[i] != null)
			{
				if (!cluster[i].triggered)
				{
                    cluster[i].Arise();
                }
			}
		}
		*/

		SpawnerUtil.Instance.NetworkSpawnGameObject("RangedEnemy", gameObject.transform.position);
		NetworkDestroy();
	}
}
