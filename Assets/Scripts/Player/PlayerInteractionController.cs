using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(PlayerCameraController))]
public class PlayerInteractionController : MonoBehaviourPun
{
    private PlayerCameraController camController;
    private PlayerInput playerInput;
    private InputAction interactAction;
    [SerializeField] private Camera cam;
    [SerializeField] private float range = 3f;
    [SerializeField] private LayerMask interactMask;
    private InteractableBase current;

    private void Awake()
    {
        playerInput = this.GetComponent<PlayerInput>();
        camController = this.GetComponent<PlayerCameraController>();

        interactAction = playerInput.actions["Interact"];
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;

        interactAction.performed += OnInteract;
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteract;
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (current == null) return;
        if (camController == null) return;

        camController.SetBlendEaseInOut(0.5f);

        // 상호작용 RPC 수행
        photonView.RPC(nameof(TryInteractRPC),
                    RpcTarget.All,
                    current.ViewId);
    }

    // 상호작용 RPC (info에 Sender 정보가 담겨 있음.)
    [PunRPC]
    private void TryInteractRPC(int targetViewId, PhotonMessageInfo info)
    {
        // 해당 오브젝트의 ViewID
        PhotonView targetPv = PhotonView.Find(targetViewId);
        if (targetPv == null) return;

        var interactable = targetPv.GetComponent<InteractableBase>();
        if (interactable == null) return;

        var actorPv = FindActorPhotonView(info.Sender);
        if (actorPv == null) return;

        float dist = Vector3.Distance(actorPv.transform.position, targetPv.transform.position);
        if (dist > 4.0f) return;

        if (info.Sender.IsLocal)
            interactable.RequestInteract(actorPv.Owner.ActorNumber);
    }

    // RPC를 전송한 유저의 pv를 반환
    private PhotonView FindActorPhotonView(Player player)
    {
        foreach (var pv in FindObjectsOfType<PhotonView>())
        {
            if (pv == null) continue;
            if (pv.OwnerActorNr != player.ActorNumber) continue;
            
            if (pv.CompareTag("Player"))
                return pv;
        }
        return null;
    }

    // 카메라 중심 레이 발사
    private void Update()
    {
        if (UIManager.Instance.IsOpen) return;
        if (InspectManager.Instance.IsInspecting) return;
        if (!photonView.IsMine) return;

        if (GameManager.Instance.IsInteractingFocused)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(mouseRay, out var mouseHit, range / 2, interactMask))
                {
                    var target = mouseHit.collider.GetComponent<InteractableBase>();
                    if (target != null)
                        photonView.RPC(nameof(TryInteractRPC), RpcTarget.All, target.ViewId);
                }
            }

            return;
        }

        // 카메라 중심으로 레이 발사
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out var hit, range, interactMask))
        {
            current = hit.collider.GetComponent<InteractableBase>();
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
