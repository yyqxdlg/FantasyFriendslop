using Unity.Netcode;
using UnityEngine;

public class ClickButton : NetworkBehaviour
{

	public ClickReceiver receiver;

	public int code;

	public NetworkVariable<bool> visible = new NetworkVariable<bool>(
			true,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server
	);

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		visible.OnValueChanged += OnChangeVisibility;

		OnChangeVisibility(false, visible.Value);
	}

	public void Init(ClickReceiver receiver, int code)
	{
		this.receiver = receiver;
		this.code = code;
	}

	public void OnMouseDown()
	{
		receiver.ReceiveClick(code);
	}

	public void ChangeVisibility(bool newVal)
	{
		if (!IsServer) throw new System.Exception("Should be called from server");

        visible.Value = newVal;
	}

	public void OnChangeVisibility(bool prev, bool next)
	{

        SpriteRenderer square = gameObject.GetComponent<SpriteRenderer>();

        CanvasGroup canvasGroup = gameObject.GetComponentInChildren<CanvasGroup>();

		if (next)
		{
            square.enabled = true;

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
        }

        if (!next)
        {
            square.enabled = false;

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
