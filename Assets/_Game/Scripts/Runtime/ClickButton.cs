using UnityEngine;

public class ClickButton : MonoBehaviour
{

    public ClickReceiver receiver;

    public int code;
    public void Init(ClickReceiver receiver, int code)
    {
        this.receiver = receiver;
        this.code = code;
    }

    // Update is called once per frame
    void Update()
	{
        if (receiver == null)
        {
            Debug.Log("Clickbutton without receiver!");
        }
	}

    public void OnMouseDown()
    {
        receiver.ReceiveClick(code);
    }

 
}
