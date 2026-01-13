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

        // 상호작용 RPC 수행
        photonView.RPC(nameof(RPC_RequestInteract), 
                    RpcTarget.MasterClient,
                    current.ViewId,
                    PhotonNetwork.LocalPlayer.ActorNumber);

    }

    [PunRPC]
    private void RPC_RequestInteract(int targetViewId, int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView targetPv = PhotonView.Find(targetViewId);
        if (targetPv == null) return;

        var interactable = targetPv.GetComponent<IInteractable>();
        if (interactable == null) return;

        var actorPv = FindActorPhotonView(info.Sender);
        if (actorPv == null) return;

        float dist = Vector3.Distance(actorPv.transform.position, targetPv.transform.position);
        if (dist > 4.0f) return;

        if (!interactable.CanInteract(actorNumber)) return;

        interactable.ServerInteract(actorNumber);
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

    private void Update()
    {
        if (!photonView.IsMine)
            return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out var hit, range, interactMask))
            current = hit.collider.GetComponentInParent<IInteractable>();
    }

    private void OnDrawGizmos()
    {
        if (cam == null) return;

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        bool hitSomething = Physics.Raycast(origin, dir, out RaycastHit hit, range, interactMask);
        Gizmos.color = hitSomething ? Color.green : Color.red;

        Vector3 end = hitSomething ? hit.point : origin + dir * range;
        Gizmos.DrawLine(origin, end);
    }
}
