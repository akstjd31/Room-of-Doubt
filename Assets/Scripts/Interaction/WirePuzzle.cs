using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class WirePuzzle : InteractableBase
{
    [SerializeField] private Transform portsTrf;

    [Header("Cinemachine")]
    private PlayerCameraController playerCamCtrl;
    [SerializeField] private CinemachineCamera puzzleCam;
    private CinemachineBrain brain;

    private bool isUsingPuzzle;

    private void Awake()
    {
        if (portsTrf == null) portsTrf = this.transform.GetChild(0);
    }

    private void Start()
    {
        if (PlayerCameraController.Instance != null)
        {
            playerCamCtrl = PlayerCameraController.Instance;
            brain = Camera.main.GetComponent<CinemachineBrain>();
        }
            
    }

    public override void Interact(int actorNumber)
    {
        if (isUsingPuzzle)
            StartCoroutine(ExitPuzzle());
        else
            EnterPuzzleCamera();
    }
    private void EnterPuzzleCamera()
    {
        isUsingPuzzle = true;

        puzzleCam.Priority = 20;
        playerCamCtrl.playerCam.Priority = 0;

        GameManager.Instance.EnterPuzzle();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("퍼즐 카메라 시작");
    }

    private IEnumerator ExitPuzzle()
    {
        if (!isUsingPuzzle) yield break;

        puzzleCam.Priority = 0;
        playerCamCtrl.playerCam.Priority = 20;

        yield return WaitForBlendComplete();
        GameManager.Instance.ExitPuzzle();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isUsingPuzzle = false;

        Debug.Log("퍼즐 카메라 종료");
    }

    private IEnumerator WaitForBlendComplete()
    {
        if (brain == null) yield break;

        yield return null;

        while (brain.ActiveBlend != null)
            yield return null;
    }

    public Transform GetTopSlotParent() => portsTrf.GetChild(0);
    public Transform GetBottomSlotParent() => portsTrf.GetChild(1);
}
