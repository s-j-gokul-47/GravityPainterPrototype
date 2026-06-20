using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestStoreUI : MonoBehaviour
{
    public GameObject storeRoot;
    public GameObject skinCardPrefab;
    public GameObject gridParent;
    public TextMeshProUGUI coinDisplay;
    public TextMeshProUGUI statusMessage;
    public BallPreviewRenderer previewRenderer;

    [Header("Confirmation Popup")]
    public GameObject confirmPopup;
    public TextMeshProUGUI confirmText;
    public Button confirmYesBtn;
    public Button confirmNoBtn;

    private List<BallSkinData> allSkins;
    private readonly List<SkinCard> cards = new List<SkinCard>();
    private BallSkinData _pendingSkin;

    private class SkinCard
    {
        public GameObject root;
        public BallSkinData skin;
        public Button button;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI priceLabel;
        public Image lockIcon;
        public Image checkIcon;
    }

    private void Awake()
    {
        WireBackButton();
        WireConfirmButtons();
        LoadSkins();
    }

    private void WireConfirmButtons()
    {
        if (confirmYesBtn != null)
        {
            confirmYesBtn.onClick.RemoveAllListeners();
            confirmYesBtn.onClick.AddListener(ConfirmPurchase);
        }
        if (confirmNoBtn != null)
        {
            confirmNoBtn.onClick.RemoveAllListeners();
            confirmNoBtn.onClick.AddListener(CancelPurchase);
        }
    }

    private void LoadSkins()
    {
        allSkins = new List<BallSkinData>(Resources.LoadAll<BallSkinData>("Settings/BallSkins"));
    }

    private void WireBackButton()
    {
        Transform backBtnT = transform.Find("BackButton");
        if (backBtnT == null) return;
        Button btn = backBtnT.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (storeRoot != null)
            storeRoot.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        if (storeRoot != null)
            storeRoot.SetActive(false);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform mainMenu = canvas.transform.Find("MainMenu");
            if (mainMenu != null)
                mainMenu.gameObject.SetActive(true);
        }
    }

    private void OnDisable()
    {
        foreach (var card in cards)
        {
            if (card.root != null)
                Destroy(card.root);
        }
        cards.Clear();
    }

    public void Refresh()
    {
        if (allSkins == null || allSkins.Count == 0)
            LoadSkins();

        UpdateCoinDisplay();
        RebuildCards();
        SelectDefaultSkin();
    }

    private void UpdateCoinDisplay()
    {
        if (coinDisplay != null)
            coinDisplay.text = "Coins: " + CoinManager.GetTotalCoins();
    }

    private void RebuildCards()
    {
        foreach (var card in cards)
        {
            if (card.root != null)
                Destroy(card.root);
        }
        cards.Clear();

        if (skinCardPrefab == null || gridParent == null || allSkins == null)
            return;

        foreach (BallSkinData skin in allSkins)
        {
            if (skin == null) continue;

            GameObject cardObj = Instantiate(skinCardPrefab, gridParent.transform);

            SkinCard card = new SkinCard();
            card.root = cardObj;
            card.skin = skin;

            TextMeshProUGUI[] labels = cardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var label in labels)
            {
                if (label.name.Contains("Name"))
                    card.nameLabel = label;
                else if (label.name.Contains("Price"))
                    card.priceLabel = label;
            }

            Image[] images = cardObj.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.name.Contains("Lock"))
                    card.lockIcon = img;
                else if (img.name.Contains("Check"))
                    card.checkIcon = img;
            }

            card.button = cardObj.GetComponentInChildren<Button>(true);
            if (card.button != null)
            {
                BallSkinData captured = skin;
                card.button.onClick.AddListener(() => OnSkinCardClicked(captured));
            }

            ApplyCardState(card);
            cards.Add(card);
        }
    }

    private void ApplyCardState(SkinCard card)
    {
        if (card == null || card.skin == null) return;

        bool purchased = BallSkinManager.IsSkinPurchased(card.skin.skinId);
        bool selected = BallSkinManager.GetSelectedSkinId() == card.skin.skinId;
        bool unlocked = purchased || card.skin.unlockedByDefault;

        if (card.nameLabel != null)
            card.nameLabel.text = card.skin.skinName;

        if (card.priceLabel != null)
        {
            if (purchased)
                card.priceLabel.text = selected ? "Selected" : "Owned";
            else
                card.priceLabel.text = card.skin.price + " coins";
        }

        if (card.lockIcon != null)
            card.lockIcon.gameObject.SetActive(!unlocked);

        if (card.checkIcon != null)
            card.checkIcon.gameObject.SetActive(selected);
    }

    private void OnSkinCardClicked(BallSkinData skin)
    {
        if (skin == null) return;

        if (previewRenderer != null)
            previewRenderer.ShowSkin(skin);

        bool purchased = BallSkinManager.IsSkinPurchased(skin.skinId) || skin.unlockedByDefault;
        bool selected = BallSkinManager.GetSelectedSkinId() == skin.skinId;

        if (selected) return;

        if (purchased)
        {
            BallSkinManager.SelectSkin(skin.skinId);
            ShowStatus("Selected " + skin.skinName);
            Refresh();
        }
        else
        {
            if (CoinManager.GetTotalCoins() < skin.price)
            {
                ShowStatus("Not enough coins!");
                return;
            }

            _pendingSkin = skin;
            if (confirmPopup != null && confirmText != null)
            {
                confirmText.text = "Buy " + skin.skinName + " for " + skin.price + " coins?";
                confirmPopup.SetActive(true);
            }
        }
    }

    private void ConfirmPurchase()
    {
        if (_pendingSkin == null) return;

        if (confirmPopup != null)
            confirmPopup.SetActive(false);

        if (CoinManager.SpendCoins(_pendingSkin.price))
        {
            BallSkinManager.PurchaseSkin(_pendingSkin.skinId);
            BallSkinManager.SelectSkin(_pendingSkin.skinId);
            ShowStatus("New skin unlocked! (" + _pendingSkin.skinName + ")");
            Refresh();
        }

        _pendingSkin = null;
    }

    private void CancelPurchase()
    {
        if (confirmPopup != null)
            confirmPopup.SetActive(false);
        _pendingSkin = null;
    }

    private void SelectDefaultSkin()
    {
        if (allSkins == null || allSkins.Count == 0) return;

        string selectedId = BallSkinManager.GetSelectedSkinId();
        BallSkinData found = allSkins.Find(s => s.skinId == selectedId)
                          ?? allSkins.Find(s => s.unlockedByDefault);

        if (found != null && previewRenderer != null)
            previewRenderer.ShowSkin(found);
    }

    private void ShowStatus(string message)
    {
        if (statusMessage != null)
        {
            StopAllCoroutines();
            statusMessage.text = message;
            statusMessage.gameObject.SetActive(true);
            StartCoroutine(HideStatusAfterDelay());
        }
    }

    private System.Collections.IEnumerator HideStatusAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (statusMessage != null)
            statusMessage.gameObject.SetActive(false);
    }
}
