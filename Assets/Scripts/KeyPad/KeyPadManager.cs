using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;

public class KeyPadManager : MonoBehaviourPunCallbacks
{
    private const int MAX_CODE_LENGTH = 4;
    private const string ROOM_PROP_KEY = "KP_ANSWER";

    [Header("Answer")]
    [SerializeField] private string collect; // 정답

    [Header("Runtime")]
    [SerializeField] private int[] codes;
    [SerializeField] private int currentNumLength;
    [SerializeField] private TMP_Text screenText;
    [SerializeField] private LayerMask numPadMask;

    public bool IsSolved { get; private set; }
    [SerializeField] private bool isFinal;   // 최종 탈출하기 위한 키패드인가?

    private string input;
    private string result;

    public void Init()
    {
        codes = new int[MAX_CODE_LENGTH];
        input = "";
        currentNumLength = 0;

        EnsureSharedAnswer();
    }

    private void Update()
    {
        if (IsSolved) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 10f, numPadMask))
            {
                // 아직 입력이 덜 되었을 때
                if (currentNumLength < MAX_CODE_LENGTH)
                {
                    var numComp = hit.transform.GetComponent<Number>();
                    if (numComp == null) return;

                    string nStr = numComp.NumStr;
                    if (string.IsNullOrEmpty(nStr)) return;

                    // 인트 변환 시도 (#, * 제외)
                    if (int.TryParse(nStr, out int n))
                    {
                        codes[currentNumLength] = n;
                        input += nStr;
                        currentNumLength++;
                    }
                }

                if (screenText != null)
                    screenText.text = input;

                if (currentNumLength >= MAX_CODE_LENGTH)
                {
                    result = string.Join("", new List<int>(codes).ConvertAll(i => i.ToString()).ToArray());
                    Debug.Log($"[KeyPad] Input Result = {result}");

                    if (collect == result)
                    {
                        if (!IsSolved)
                        {
                            if (isFinal)
                            {
                                SuccessLocal();
                            }
                            else
                            {
                                photonView.RPC(nameof(SuccessRPC), RpcTarget.AllBuffered);
                            }
                        }
                    }
                    else
                    {
                        ResetLocalInput();
                    }

                    input = "";
                    if (screenText != null && !IsSolved)
                        screenText.text = "";
                }
            }
        }
    }

    private void EnsureSharedAnswer()
    {
        if (!PhotonNetwork.InRoom) return;
        if (!PhotonNetwork.IsMasterClient) return;

        string newAnswer = GenerateRandomDigits(MAX_CODE_LENGTH);
        var props = new Hashtable { { ROOM_PROP_KEY, newAnswer } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // 현재 플레이어들에게 즉시 적용 + 늦게 들어오는 유저도 적용되게 Buffered
        photonView.RPC(nameof(SetAnswerRPC), RpcTarget.AllBuffered, newAnswer);

        Debug.Log($"비밀번호 설정 완료! 현재: {collect}");
    }

    // 0 ~ 9 까지 랜덤 수 생성
    private string GenerateRandomDigits(int length)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(UnityEngine.Random.Range(0, 10));
        return sb.ToString();
    }

    [PunRPC]
    private void SetAnswerRPC(string answer) => collect = answer;

    private void ResetLocalInput()
    {
        currentNumLength = 0;
        for (int i = 0; i < codes.Length; i++)
            codes[i] = 0;
        input = "";
    }

    // 로컬 전용 처리
    private void SuccessLocal()
    {
        IsSolved = true;

        Debug.Log("해결! (로컬)");
        if (screenText != null)
        {
            screenText.fontSize = 1200;
            screenText.text = "UNLOCK!";
        }
    }

    // 성공 결과만 공유
    [PunRPC]
    private void SuccessRPC()
    {
        if (IsSolved) return;

        IsSolved = true;

        Debug.Log("해결! (RPC)");

        if (screenText != null)
        {
            screenText.fontSize = 1200;
            screenText.text = "UNLOCK!";
        }
    }
}
