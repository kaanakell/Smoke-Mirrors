using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;

    void Start()
    {
        FindPanel();

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inventoryPanel == null)
                FindPanel();

            if (inventoryPanel == null)
            {
                Debug.LogWarning("[InventoryToggle] InventoryPanel not found! Make sure it's tagged 'InventoryPanel'.");
                return;
            }

            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }
    }

    void FindPanel()
    {
        if (inventoryPanel != null) return;

        GameObject found = GameObject.FindGameObjectWithTag("InventoryPanel");
        if (found != null)
        {
            inventoryPanel = found;
            Debug.Log("[InventoryToggle] Found InventoryPanel by tag.");
        }
    }
}