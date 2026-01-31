using System.Collections;
using UnityEngine;

public class SwitchLight : InteractableBase
{
    [SerializeField] private GameObject[] spotLights;
    private bool isOn = false;
    public override void Interact(int actorNumber)
    {
        // 해당 퍼즐 (모든 불을 키는 그런 퍼즐) 이 해결안되었다면 기능 사용 X
        if (!GameManager.Instance.WirePuzzleSolved)
        {
            prompt = "불이 안켜진다.";
            UIManager.Instance.ShowMessage(prompt);
            return;
        }

        isOn = !isOn;
        foreach (GameObject light in spotLights)
        {
            light.SetActive(isOn);
        }
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
