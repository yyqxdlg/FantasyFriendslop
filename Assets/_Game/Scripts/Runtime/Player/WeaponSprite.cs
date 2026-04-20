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

		float angle = (Mathf.Acos(Vector2.Dot(dir, new Vector2(1,0)) / dir.magnitude)) * (180 / Mathf.PI);

		if (Mathf.Sign(dir.y) == -1)
		{
			angle = 360 - angle;
		}

		gameObject.transform.rotation = Quaternion.Euler(0, 0, angle);
	
	}

}
