using UnityEngine;

public class BananaEndingTarget : MonoBehaviour
{
    [TextArea(2, 4)]
    [SerializeField] private string endingText = "Ending text goes here.";

    public string EndingText => endingText;
}