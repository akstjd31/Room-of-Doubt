using System;
using System.Linq;
using UnityEngine;


public class KeyPadManager : MonoBehaviour
{
    const int MAX_CODE_LENGTH = 4;
    [SerializeField] int[] codes;
    [SerializeField] private int currentNum;
    private string screenText;
    private string answer;
    private string result;

    private void Start()
    {
        codes = new int[MAX_CODE_LENGTH];
        currentNum = 0;
    }

    private void Update()
    {
        screenText = string.Join("", codes.Select(i => i.ToString()).ToArray());

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 10))
            {
                if (currentNum < Convert.ToInt32(MAX_CODE_LENGTH))
                {
                    
                }
            }
        }
    }
}
