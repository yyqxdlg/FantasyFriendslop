using UnityEngine;

public class Wiper : MonoBehaviour
{
    public Transform[] wiperPoints;

    public void Wipe(int i)
    {
        Debug.Log("WIPE AT POINT " + wiperPoints[i].name); 

        transform.position = wiperPoints[i].position;
    }
    public void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.isTrigger)
        {
            col.gameObject.GetComponent<Spawnable>().NetworkDestroy();
        }
    }
}
