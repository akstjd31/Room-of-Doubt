
using System.Collections;

public interface IInteractable
{
    int ViewId { get; }                 // 해당 ViewID
    string Prompt { get; }              // 화면에 띄울 문구
    Item RewardItem { get; }            // 해당 상호작용을 통해 얻을 수 있는 보상 아이템 (없다면 null)
    Item RequiredItem { get; }          // 상호작용을 하기 위해 필요한 아이템 (없다면 null)

    // 현 상호작용이 가능한지? (안되는 경우 ex. 다른 플레이어가 이용중일떄), 상호작용 할때 필요한 아이템도 인자로 넘겨받기
    bool CanInteract(int actorNumber);      
    IEnumerator EnterCamera();
    IEnumerator ExitCamera();

    // 상호작용 수행, 만약 특정 조건으로 아이템이 필요하여 사용된다면 추가로 프로퍼티로 받음
    void Interact(int actorNumber);              
}