using UnityEngine;

[CreateAssetMenu(menuName = "Gravity Painter/Ball Skin Data")]
public class BallSkinData : ScriptableObject
{
    public string skinId;
    public string skinName;
    public int price;
    public bool unlockedByDefault;
    public string prefabResourcePath;
}
