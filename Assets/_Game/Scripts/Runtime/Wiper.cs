using UnityEngine;

public class Wiper : MonoBehaviour
{
    public Transform[] wiperPoints;

    private int currentWipePoint = 0;

    public void Wipe(int i)
    {
        Debug.Log("WIPE AT POINT " + wiperPoints[i].name);

        currentWipePoint = i;

        Invoke("WipeAtCurrentWipePoint", 1);
    }

    private void WipeAtCurrentWipePoint()
    {
        transform.position = wiperPoints[currentWipePoint].position;
    }
    public void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.isTrigger)
        {
            col.gameObject.GetComponent<Spawnable>().NetworkDestroy();
        }
    }
}
