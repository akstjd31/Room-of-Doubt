using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class KeyPadManager : MonoBehaviourPun
{
    private const int MAX_CODE_LENGTH = 4;

    [Header("Answer")]
    [SerializeField] private string collect = "1234"; // 정답

    [Header("Runtime")]
    [SerializeField] private int[] codes;
    [SerializeField] private int currentNumLength;
    [SerializeField] private TMP_Text screenText;
    [SerializeField] private LayerMask numPadMask;

    public bool IsSolved { get; private set; }
    [SerializeField] private bool isFinal;   // 최종 탈출하기 위한 키패드인가?

    private string input;
    private string result;

    private void Start()
    {
        codes = new int[MAX_CODE_LENGTH];
        input = "";
        currentNumLength = 0;
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
        screenText.fontSize = 1200;
        screenText.text = "UNLOCK!";
    }
}
