using UnityEngine;
using System.Linq;
using Photon.Pun;
using Unity.Cinemachine; // 너 프로젝트 기준

public class PlayerController : MonoBehaviourPun
{
    public bool IsEscaped { get; private set; }

    public Transform HeadPivot;
    [SerializeField] private Transform cameraPivot;
    public Transform CameraPivot => cameraPivot;

    [SerializeField] private CinemachineCamera myCam; // "내 플레이 vcam" 용도로만 쓰기
    [SerializeField] private MonoBehaviour[] playerMonos;

    private void Awake()
    {
        // 자동 수집(주의: 너무 많이 꺼질 수 있으니 아래 Exclude를 잘 잡아야 함)
        playerMonos = GetComponentsInChildren<MonoBehaviour>(true)
            .Where(m => m != this)
            .Where(m => !(m is PhotonView))
            // CinemachineBrain은 보통 "메인 출력 카메라"에 있음. 플레이어 하위에 있으면 제외
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

        TimeAttackSync.StartTimeAttack();
    }

    [PunRPC]
    private void ApplyEscapeAll(int actorNumber)
    {
        // 내 오브젝트가 해당 actor의 것인지 확인
        if (photonView.OwnerActorNr != actorNumber) return;

        IsEscaped = true;

        // 1) 조작/상호작용 끄기 (로컬만)
        if (photonView.IsMine)
        {
            foreach (var mono in playerMonos)
                if (mono) mono.enabled = false;

            // 내 전용 시네머신 카메라 끄기 
            if (myCam) myCam.gameObject.SetActive(false);

            // 관전 시작(로컬만)
            SpectatorManager.Instance.EnterSpectate();
        }

        // 2) 모두에게 공통으로 적용: 시각/충돌 제거(유령화)
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
