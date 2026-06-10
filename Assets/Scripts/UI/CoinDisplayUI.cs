using UnityEngine;
using TMPro;

/// <summary>
/// Place this on a UI Text element (TextMeshPro) to display the total coins.
/// </summary>
public class CoinDisplayUI : MonoBehaviour
{
    [Tooltip("The TextMeshPro element to update. If left empty, it will try to find one on this object.")]
    [SerializeField] private TextMeshProUGUI coinText;

    private void Awake()
    {
        // Auto-assign the text component if you attach this script directly to a Text element
        if (coinText == null)
        {
            coinText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        UpdateCoinDisplay();
    }

    public void UpdateCoinDisplay()
    {
        if (coinText != null)
        {
            // Update the text to show the saved total coins
            coinText.text = "Total Coins: " + CoinManager.GetTotalCoins().ToString();
        }
    }
}
