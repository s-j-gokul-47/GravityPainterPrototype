using System.Collections.Generic;
using UnityEngine;

public static class BallSkinManager
{
    private const string PurchasedPrefix = "SkinPurchased_";
    private const string SelectedSkinKey = "SelectedSkinId";

    public static bool IsSkinPurchased(string skinId)
    {
        bool purchased = PlayerPrefs.GetInt(PurchasedPrefix + skinId, 0) == 1;
        Debug.Log("[BallSkinManager] IsSkinPurchased(" + skinId + ") = " + purchased);
        return purchased;
    }

    public static void PurchaseSkin(string skinId)
    {
        Debug.Log("[BallSkinManager] PurchaseSkin(" + skinId + ")");
        PlayerPrefs.SetInt(PurchasedPrefix + skinId, 1);
        PlayerPrefs.Save();
        Debug.Log("[BallSkinManager] Saved purchase for: " + skinId);
    }

    public static string GetSelectedSkinId()
    {
        string id = PlayerPrefs.GetString(SelectedSkinKey, "default");
        Debug.Log("[BallSkinManager] GetSelectedSkinId() = " + id);
        return id;
    }

    public static void SelectSkin(string skinId)
    {
        Debug.Log("[BallSkinManager] SelectSkin(" + skinId + ")");
        PlayerPrefs.SetString(SelectedSkinKey, skinId);
        PlayerPrefs.Save();
    }

    public static GameObject LoadSkinPrefab(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            Debug.LogWarning("[BallSkinManager] LoadSkinPrefab - empty resourcePath");
            return null;
        }
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        Debug.Log("[BallSkinManager] LoadSkinPrefab(" + resourcePath + ") = " + (prefab != null ? "found" : "null"));
        return prefab;
    }

    public static BallSkinData GetSelectedSkinData(List<BallSkinData> allSkins)
    {
        string selectedId = GetSelectedSkinId();
        BallSkinData skin = allSkins.Find(s => s.skinId == selectedId);
        if (skin == null)
        {
            skin = allSkins.Find(s => s.unlockedByDefault);
            Debug.Log("[BallSkinManager] Selected skin not found, falling back to default");
        }
        return skin;
    }

    public static GameObject LoadSelectedSkin(List<BallSkinData> allSkins)
    {
        Debug.Log("[BallSkinManager] LoadSelectedSkin()");
        BallSkinData skin = GetSelectedSkinData(allSkins);
        if (skin == null)
        {
            Debug.LogError("[BallSkinManager] No skin found at all!");
            return null;
        }
        Debug.Log("[BallSkinManager] Loading skin: " + skin.skinName + " path=" + skin.prefabResourcePath);
        return LoadSkinPrefab(skin.prefabResourcePath);
    }
}
