using Photon.Pun;
using UnityEngine;

public class CubeObj : MonoBehaviourPun, IInteractable
{
    [SerializeField] private string prompt;
    public int ViewId => photonView.ViewID;

    public string Prompt => prompt;

    public bool CanInteract(int actorId)
    {  
        // 잠금 상태라면 false
        return true;
    }
    
    public void ServerInteract(int actorId)
    {
        // 나 이거 수행했어! 를 모든 클라이언트에게 알려주기
    }

    public void ClientApplyState()
    {
        // 클라이언트 상태 전달용
    }
}
