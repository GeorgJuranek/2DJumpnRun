using UnityEngine;
using TMPro;

public class GUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI jumpsRemainingText;
    [SerializeField] private TextMeshProUGUI congratulationText;

    private void OnEnable()
    {
        PigeonController.OnJumpsChanged += UpdateText;
        PigeonController.OnCongratulationChanged += UpdateCongratulation;
    }

    private void OnDisable()
    {
        PigeonController.OnJumpsChanged -= UpdateText;
        PigeonController.OnCongratulationChanged -= UpdateCongratulation;
    }

    public void UpdateText(int jumps, bool canJump, bool isWithinCoyoteTime = false, bool isDoomedToDeath = false)
    {
        string jumpIndicator = "";
        int currentJumps = jumps + (canJump ? 1 : 0);
        string currentSign = "";


        if (isWithinCoyoteTime && currentJumps > 0 && !isDoomedToDeath)
        {
            jumpsRemainingText.color = Color.white;
            currentSign = "O ";
        }
        else
        {
            jumpsRemainingText.color = Color.red;
            currentSign = "X ";
        }

        for (int i = currentJumps; i > 0; i--)
        {
            jumpIndicator += currentSign;
        }

        jumpsRemainingText.text = $"Jump: {jumpIndicator}";
    }

    public void UpdateCongratulation(bool isInDoor)
    {
        congratulationText.text = isInDoor ? "Congratulations!\n\nYou have reached a nest." : "";
    }
}
