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

    public void OnMouseDown()
    {
        receiver.ReceiveClick(code);
    }

 
}
