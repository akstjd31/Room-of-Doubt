using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;

public class PlayerInteractor : MonoBehaviourPun
{
    private PlayerInput playerInput;
    private InputAction interactInput;
    [SerializeField] private Camera cam;
    [SerializeField] private float range = 3f;
    [SerializeField] private LayerMask interactMask;
    private IInteractable current;

    private void Awake()
    {
        playerInput = this.GetComponent<PlayerInput>();

        interactInput = playerInput.actions["Interact"];
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;

        interactInput.performed += OnInteract;
    }

    private void OnDisable()
    {
        if (interactInput != null)
            interactInput.performed -= OnInteract;
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!photonView.IsMine) return;
        if (current == null) return;

        Debug.Log("E키 누름");
        
        // 상호작용 RPC 수행
        photonView.RPC(nameof(TryInteractRPC), 
                    RpcTarget.All,
                    current.ViewId,
                    PhotonNetwork.LocalPlayer.ActorNumber);
    }

    // 상호작용 RPC (info에 Sender 정보가 담겨 있음.)
    [PunRPC]
    private void TryInteractRPC(int targetViewId, int actorNumber, PhotonMessageInfo info)
    {
        PhotonView targetPv = PhotonView.Find(targetViewId);
        if (targetPv == null) return;

        var interactable = targetPv.GetComponent<IInteractable>();
        if (interactable == null) return;

        var actorPv = FindActorPhotonView(info.Sender);
        if (actorPv == null) return;

        float dist = Vector3.Distance(actorPv.transform.position, targetPv.transform.position);
        if (dist > 4.0f) return;

        if (!interactable.CanInteract(actorNumber)) return;

        interactable.Interact(actorNumber);
    }

    // RPC를 전송한 유저의 pv를 반환
    private PhotonView FindActorPhotonView(Player sender)
    {
        foreach (var pv in FindObjectsOfType<PhotonView>())
        {
            if (pv.Owner == sender)
                return pv;
        }

        return null;
    }

    // 카메라 중심 레이 발사
    private void Update()
    {
        if (!photonView.IsMine)
            return;

        // 카메라 중심으로 레이 발사
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out var hit, range, interactMask))
        {
            current = hit.collider.GetComponent<IInteractable>();
        }
        else
        {
            if (current != null)
                current = null;
        }

        UIManager.Instance.UpdateObjectNameText(current?.Prompt);
    }

    // 레이 시각화
    private void OnDrawGizmos()
    {
        if (cam == null) return;

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        // 닿으면 초록, 아님 빨강
        bool hitSomething = Physics.Raycast(origin, dir, out RaycastHit hit, range, interactMask);
        Gizmos.color = hitSomething ? Color.green : Color.red;

        // 물체가 가까이 있으면 물체까지 선 긋기 or 최대 길이까지
        Vector3 end = hitSomething ? hit.point : origin + dir * range;
        Gizmos.DrawLine(origin, end);
    }
}
