using Unity.Cinemachine;
using UnityEngine;

public class WirePuzzle : InteractableBase
{
    [SerializeField] private Transform portsTrf;

    [Header("Cinemachine")]
    private PlayerCameraController playerCamCtrl;
    [SerializeField] private CinemachineCamera puzzleCam;

    public bool IsUsingPuzzle { get; private set; }

    private void Awake()
    {
        if (portsTrf == null) portsTrf = this.transform.GetChild(0);
    }

    private void Start()
    {
        if (PlayerCameraController.Instance != null)
            playerCamCtrl = PlayerCameraController.Instance;
    }

    public override void Interact(int actorNumber)
    {
        if (IsUsingPuzzle) return;

        EnterPuzzleCamera();
    }
    private void EnterPuzzleCamera()
    {
        IsUsingPuzzle = true;

        puzzleCam.Priority = 20;
        playerCamCtrl.playerCam.Priority = 0;
        

        Debug.Log("퍼즐 카메라 시작");
    }

    private void ExitPuzzle()
    {
        if (!IsUsingPuzzle) return;

        puzzleCam.Priority = 0;
        playerCamCtrl.playerCam.Priority = 20;

        IsUsingPuzzle = false;
        Debug.Log("퍼즐 카메라 종료");
    }
    public Transform GetTopSlotParent() => portsTrf.GetChild(0);
    public Transform GetBottomSlotParent() => portsTrf.GetChild(1);
}
