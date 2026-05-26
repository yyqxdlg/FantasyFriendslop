using System.Collections;
using UnityEngine;

public class Wiper : MonoBehaviour
{
    public Transform[] wiperPoints;

    private int currentWipePoint = 0;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Wipe(int i)
    {
        Debug.Log("WIPE AT POINT " + wiperPoints[i].name);
        currentWipePoint = i;
        Invoke("WipeAtCurrentWipePoint", 1);
    }

    private void WipeAtCurrentWipePoint()
    {
        if (rb != null)
            rb.MovePosition(wiperPoints[currentWipePoint].position);
        else
            transform.position = wiperPoints[currentWipePoint].position;
    }

    public void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.isTrigger)
        {
            Spawnable spawnable = GetComponent<Spawnable>();

            if(spawnable != null)
            {
                spawnable.NetworkDestroy();
            }
        }
    }
}