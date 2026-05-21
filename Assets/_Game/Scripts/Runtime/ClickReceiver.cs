using Unity.Netcode;
using UnityEngine;

public class ClickReceiver : NetworkBehaviour
{
    public ClickButton[] buttons;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Init(this, i);
        }
    }

    public virtual void ReceiveClick(int code)
    {
        Debug.Log("Received click from: " + code);
    }
}
