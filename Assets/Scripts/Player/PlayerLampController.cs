using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerLampController : MonoBehaviourPunCallbacks
{
    [SerializeField] private Light lampLight;

    private void Awake()
    {
        if (lampLight != null) lampLight.enabled = false;
    }

    private void Start()
    {
        Apply(photonView.Owner);
    }

    private void Apply(Player p)
    {
        if (lampLight == null || p == null) return;

        bool on = false;
        if (p.CustomProperties != null &&
            p.CustomProperties.TryGetValue(PlayerPropKeys.LAMP_ON, out var obj) &&
            obj is bool b)
        {
            on = b;
        }

        lampLight.enabled = on;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer != photonView.Owner) return;
        if (!changedProps.ContainsKey(PlayerPropKeys.LAMP_ON)) return;

        Apply(targetPlayer);
    }
}

public static class LampNet
{
    public static void SetLampOn(bool on)
    {
        if (!PhotonNetwork.InRoom) return;

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            { PlayerPropKeys.LAMP_ON, on }
        });
    }
}