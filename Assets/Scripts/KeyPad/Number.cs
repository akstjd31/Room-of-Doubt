using UnityEngine;
using TMPro;

public class Number : MonoBehaviour
{
    [SerializeField] private TMP_Text tmp;
    public string NumStr { get; private set; }
    
    private void Awake()
    {
        tmp = this.GetComponentInChildren<TMP_Text>(true);
    }

    private void Start()
    {
        NumStr = tmp.text;
    }
}
