using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class TestStoreWiring : MonoBehaviour
{
    public TestStoreUI testStoreUI;
    public GameObject mainMenuRoot;
    [SerializeField] private Button storeButton;

    private void Start()
    {
        if (storeButton != null)
        {
            storeButton.onClick.RemoveAllListeners();
            storeButton.onClick.AddListener(() =>
            {
                if (mainMenuRoot != null)
                    mainMenuRoot.SetActive(false);

                if (testStoreUI != null)
                    testStoreUI.Open();
            });
        }
    }
}
