using UnityEngine;
using System.Linq;
using Photon.Pun;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviourPun
{
    public bool IsEscaped { get; private set; }

    public Transform HeadPivot;
    [SerializeField] private Transform cameraPivot;
    public Transform CameraPivot => cameraPivot;

    [SerializeField] private CinemachineCamera myCam;   // 자식으로 있는 시네머신 카메라
    [SerializeField] private MonoBehaviour[] playerMonos;

    private void Awake()
    {
        
        playerMonos = GetComponentsInChildren<MonoBehaviour>(true)
            .Where(m => m != this)
            .Where(m => !(m is PhotonView))
            .Where(m => !(m is CinemachineBrain))
            .ToArray();
    }

    private void Start()
    {
        SpectatorManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (SpectatorManager.Instance != null)
            SpectatorManager.Instance.UnRegister(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;
        if (IsEscaped) return;

        if (other.CompareTag("EscapeBox"))
        {
            photonView.RPC(nameof(RequestEscapeToMaster), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    [PunRPC]
    private void RequestEscapeToMaster(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (info.Sender.ActorNumber != actorNumber) return;

        photonView.RPC(nameof(ApplyEscapeAll), RpcTarget.All, actorNumber);

        if (RoomRewardManager.Instance != null)
            RoomRewardManager.Instance.NotifyEscapedToMaster(actorNumber);

        TimeAttackSync.StartTimeAttack();
    }

    [PunRPC]
    private void ApplyEscapeAll(int actorNumber)
    {
        if (photonView.OwnerActorNr != actorNumber) return;

        IsEscaped = true;

        if (photonView.IsMine)
        {
            foreach (var mono in playerMonos)
                if (mono) mono.enabled = false;

            if (myCam) myCam.gameObject.SetActive(false);

            // 관전 시작(로컬만)
            SpectatorManager.Instance.EnterSpectate();
        }

        // 관전 모드 돌입을 위한 렌더러, 콜라이더 끄기
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = false;

        foreach (var c in GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        if (!IsEscaped) return;

        if (Input.GetMouseButtonDown(0))
            SpectatorManager.Instance.NextTarget();
    }
}
