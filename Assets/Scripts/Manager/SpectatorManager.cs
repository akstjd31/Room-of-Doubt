using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using System.Linq;

/// <summary>
/// 탈출 시 관찰자 모드 관련 클래스
/// </summary>
public class SpectatorManager : MonoBehaviour
{
    public static SpectatorManager Instance;

    [Header("Spectate Cam")]
    [SerializeField] private CinemachineCamera spectateVCam;    // 관전으로 쓸 카메라
    [SerializeField] private int activePriority = 100;          // 관전 시 우선도

    private readonly List<PlayerController> players = new();    // 방에 존재하는 모든 플레이어 컨트롤러
    private int currentIndex = -1;
    public bool IsSpectating { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        spectateVCam.Priority = 0;
    }

    // 플레이어 등록 
    public void Register(PlayerController p)
    {
        players.RemoveAll(x => x == null);
        if (p != null && !players.Contains(p)) players.Add(p);
    }

    public void UnRegister(PlayerController p)
    {
        players.Remove(p);
    }

    // 관전 시작
    public void EnterSpectate()
    {
        // 탈출하지 않은(남아있는) 플레이어 필터링
        var alive = players.Where(p => p != null && !p.IsEscaped).ToList();

        if (alive.Count == 0) return;

        IsSpectating = true;
        spectateVCam.Priority = activePriority; // 높은 우선순위로 변경
        currentIndex = 0;
        SetTarget(alive[currentIndex]);
    }

    // 다음 타깃
    public void NextTarget()
    {
        var alive = players.Where(p => p != null && !p.IsEscaped).ToList();
        if (alive.Count <= 1) return; // 혼자 남았다면 변경 불필요

        currentIndex = (currentIndex + 1) % alive.Count;
        SetTarget(alive[currentIndex]);
    }

    // 타깃 설정 (팔로우 설정)
    private void SetTarget(PlayerController target)
    {
        if (target == null || target.CameraPivot == null) return;

        // 1. 타겟 할당
        spectateVCam.Follow = target.CameraPivot;

        // Same As Follow Target 설정 시, 이 Warp 호출이 현재 회전 상태를 즉시 동기화
        spectateVCam.OnTargetObjectWarped(target.CameraPivot, target.CameraPivot.position - spectateVCam.transform.position);

        spectateVCam.transform.rotation = target.CameraPivot.rotation;

        Debug.Log($"[Spectate] {target.name} 시점 동기화 완료");
    }
}