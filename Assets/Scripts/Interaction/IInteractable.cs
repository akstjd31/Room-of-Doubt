
public interface IInteractable
{
    int ViewId { get; }                 // 해당 ViewID
    string Prompt { get; }              // 화면에 띄울 문구
    bool CanInteract(int actorId);      // 현 상호작용이 가능한지? (왜냐면 다른 플레이어가 사용중일수도)
    void ServerInteract(int actorId);   // 상태 변경
    void ClientApplyState();            // 변경된 상태를 클라이언트에게 전달
}
