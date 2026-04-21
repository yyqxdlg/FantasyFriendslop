using Unity.Netcode;
using UnityEngine;

public class WeaponSprite : NetworkBehaviour
{

	public override void OnNetworkSpawn()
	{
		
	}

	public void updatePosAndRot(Vector2 pos, Vector2 dir)
	{
		gameObject.transform.position = new Vector3(pos.x, pos.y, 0);

		float angle = FFUtilities.CounterClockwiseAngle(dir, new Vector2(1, 0));
		Debug.Log(angle);

		gameObject.transform.rotation = Quaternion.Euler(0, 0, angle);
	
	}

}
