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
    [SerializeField] private CinemachineCamera spectateVCam; // (또는 CinemachineCamera)
    [SerializeField] private int spectatePriority = 50;

    private readonly List<PlayerController> players = new(); // 네 플레이어 스크립트 타입으로 변경
    private int currentIndex = -1;

    private void Start()
    {
        Instance = this;
        spectatePriority = 0;
    }
    
    public void Register(PlayerController cam)
    {
        if (!players.Contains(cam))
            players.Add(cam); 
    }

    public void UnRegister(PlayerController cam)
    {
        players.Remove(cam);
    }

    // 관전 모드 시작
    public void EnterSpectate()
    {
        var alive = players.Where(p => p != null && !p.IsEscaped).ToList();
        if (alive.Count == 0) return;

        spectateVCam.Priority = spectatePriority;
        currentIndex = 0;
        SetTarget(alive[currentIndex]);
    }

    // 다른 플레이어 관전하기
    public void NextTarget()
    {
        var alive = players.Where(p => p != null && !p.IsEscaped).ToList();
        if (alive.Count == 0) return;

        currentIndex = (currentIndex + 1) % alive.Count;
        SetTarget(alive[currentIndex]);
    }

    private void SetTarget(PlayerController target)
    {
        spectateVCam.Follow = target.CameraPivot;
        spectateVCam.LookAt = target.CameraPivot;
    }
}
