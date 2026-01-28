using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;


public class RoomRewardManager : MonoBehaviourPun
{
    public static RoomRewardManager Instance;
    [SerializeField] private RoomRewardData rewardData;
    public RoomRewardData RewardData => rewardData;

    // 탈출자 목록
    private readonly HashSet<int> escapedActors = new HashSet<int>();
    private bool finalized = false;

    private void Awake()
    {
        Instance = this;
    }

    public void NotifyEscapedToMaster(int actorNumber)
    {
        if (!PhotonNetwork.InRoom) return;
        photonView.RPC(nameof(NotifyEscapedRPC), RpcTarget.MasterClient, actorNumber);
    }

    [PunRPC]
    private void NotifyEscapedRPC(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 위조 방지
        if (info.Sender.ActorNumber != actorNumber) return;

        // 이미 등록된 탈출자면 중복 처리 방지
        if (!escapedActors.Add(actorNumber)) return;

        photonView.RPC(nameof(GrantExpRPC), RpcTarget.All, actorNumber, rewardData.exp);
    }

    [PunRPC]
    private void GrantExpRPC(int targetActor, int exp)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != targetActor) return;

        int n = escapedActors.Count;
        if (n <= 0)
        {
            Debug.Log("탈출한 사람이 없음! 경험치 못줌");
            return;
        }

        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager가 없어서 경험치를 못줌");
            return;
        }

        UserDataManager.Instance.AddExp(exp);
        Debug.Log($"보상 경험치 : {exp}");
    }

    public void FinalizeGoldRewards()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (finalized) return;
        finalized = true;

        int n = escapedActors.Count;
        if (n <= 0)
        {
            Debug.Log("탈출한 사람이 없음! 골드 못줌");
            return;
        }

        int totalGold = rewardData.gold;

        // 골드 n빵
        int share = totalGold / n;
        int remainder = totalGold % n;

        List<int> sorted = new List<int>(escapedActors);
        sorted.Sort();

        for (int i = 0; i < sorted.Count; i++)
        {
            int actor = sorted[i];
            int goldForThis = share + (i < remainder ? 1 : 0);

            photonView.RPC(nameof(GrantGoldRPC), RpcTarget.All, actor, goldForThis);
        }

        Debug.Log($"(마스터의 알림) 전체 골드량: {totalGold}, n빵한 최대 골드량: {share}, 남는 골드: {remainder}");
    }

    [PunRPC]
    private void GrantGoldRPC(int targetActor, int gold)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != targetActor) return;

        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager가 없어 골드를 못줌!");
        }

        UserDataManager.Instance.AddGold(gold);
        Debug.Log("골드 지급 완료! 지급된 골드량: " + gold);
    }
}
