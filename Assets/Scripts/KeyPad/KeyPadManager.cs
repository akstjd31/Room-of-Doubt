using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using TMPro;

public class KeyPadManager : MonoBehaviour
{
    const int MAX_CODE_LENGTH = 4;
    [SerializeField] int[] codes;
    [SerializeField] private int currentNumLength;
    [SerializeField] private string collect;
    [SerializeField] private TMP_Text screenText;
    [SerializeField] private LayerMask numPadMask;
    public bool IsSolved { get; private set; }
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
        if (!IsSolved)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 10, numPadMask))
                {
                    if (currentNumLength < Convert.ToInt32(MAX_CODE_LENGTH))
                    {
                        string nStr = hit.transform.gameObject.GetComponent<Number>().NumStr;

                        if (nStr.IsNullOrEmpty()) return;

                        int n;
                        if (int.TryParse(nStr, out n))
                        {
                            codes[currentNumLength] = n;
                            input += nStr;
                            currentNumLength++;
                        }
                    }

                    screenText.text = input;
                    if (currentNumLength >= Convert.ToInt32(MAX_CODE_LENGTH))
                    {
                        result = String.Join("", new List<int>(codes).ConvertAll(i => i.ToString()).ToArray());
                        Debug.Log(result);

                        if (collect.Equals(result))
                        {
                            IsSolved = true;
                            Debug.Log("정답!");

                            // 맞췄을 때 해야할 부분
                        }
                        else
                        {
                            currentNumLength = 0;

                            for (int i = 0; i < codes.Length; i++)
                                codes[i] = 0;
                        }

                        input = "";
                    }
                }
            }
        }
    }
}
