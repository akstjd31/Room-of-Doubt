using UnityEngine;
using System.Linq;
using Photon.Pun;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviourPun
{
    public bool IsEscaped { get; private set; }
    public Transform HeadPivot; // 카메라 피벗/머리 트랜스폼
    [SerializeField] private Transform cameraPivot; // 기존 내 카메라 루트
    [SerializeField] private CinemachineCamera myCam;
    public Transform CameraPivot => cameraPivot;
    [SerializeField] private MonoBehaviour[] playerMonos; // 이동/상호작용 스크립트들

    private void Awake()
    {
        playerMonos = GetComponentsInChildren<MonoBehaviour>(true);

        // 스크립트 자신이나, 꺼지면 안 되는 건 제외
        playerMonos = playerMonos
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

    public void Escape()
    {
        IsEscaped = true;

        if (!photonView.IsMine) return;

        // 모노붙은거 제거
        foreach (var mono in playerMonos) if (mono) mono.enabled = false;
        
        // 시각적 요소 제거
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = false;

        // 충돌 제거
        foreach (var c in GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
        }

        // 기존 로컬 카메라 끄고(선택)
        if (myCam) myCam.gameObject.SetActive(false);

        // 관전 시작
        SpectatorManager.Instance.EnterSpectate();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        if (!IsEscaped) return;

        if (Input.GetMouseButtonDown(0))
        {
            SpectatorManager.Instance.NextTarget();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EscapeBox"))
            Escape();
    }
}
