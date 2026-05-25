using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PartyGoldDisplay : NetworkBehaviour
{

    public TMP_Text text;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        InitOnChangeEvents();
    }

    private void InitOnChangeEvents()
    {
        if (GameplayManager.Instance.GetComponent<NetworkObject>().IsSpawned)
        {
            GameplayManager.Instance.exitZoneGold.OnValueChanged += OnValuesChange;

            GameplayManager.Instance.partyGoldSafe.OnValueChanged += OnValuesChange;

            UpdateDisplay();
        }
        else
        {
            Invoke("InitOnChangeEvents", 0.1f);
        }
    }

    private void OnValuesChange(int prev, int next)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        int sum = GameplayManager.Instance.partyGoldSafe.Value + GameplayManager.Instance.exitZoneGold.Value;

        text.text = "Party gold: " + sum;
    }
}
