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
        ApplyFromRoomProps();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(RoomPropKeys.LAMP_OWNER_ACTOR) ||
            propertiesThatChanged.ContainsKey(RoomPropKeys.LAMP_ON))
        {
            ApplyFromRoomProps();
        }
    }

    private void ApplyFromRoomProps()
    {
        if (lampLight == null || !PhotonNetwork.InRoom) return;

        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        int ownerActor = -1;
        bool lampOn = false;

        if (room.CustomProperties.TryGetValue(RoomPropKeys.LAMP_OWNER_ACTOR, out var ownerObj))
            ownerActor = (int)ownerObj;

        if (room.CustomProperties.TryGetValue(RoomPropKeys.LAMP_ON, out var onObj) && onObj is bool b)
            lampOn = b;

        bool isThisPlayerLampOwner = (photonView.Owner != null && photonView.Owner.ActorNumber == ownerActor);

        lampLight.enabled = isThisPlayerLampOwner && lampOn;
    }
}
