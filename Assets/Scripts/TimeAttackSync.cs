using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine;

public class TimeAttackSync : MonoBehaviourPunCallbacks
{
    private const string TA_START = "TA_START"; // 타임어택 시작
    public static void StartTimeAttack()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(TA_START))
            return;
        
        var setProps = new Hashtable { { TA_START, PhotonNetwork.Time } };
        var expected = new Hashtable { { TA_START, null } };

        PhotonNetwork.CurrentRoom.SetCustomProperties(setProps, expected);
    }
}
