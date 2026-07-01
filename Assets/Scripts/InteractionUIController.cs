using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InteractionUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemDescription;
    [SerializeField] private GameObject loadButton;
    [SerializeField] private GameObject useButton;
    [SerializeField] private GameObject shootSelection;
    [SerializeField] private GameObject shootPlayerButton;
    [SerializeField] private GameObject shootEnemyButton;
    [SerializeField] private ShotGunState shotGunState;

    private UnityEngine.Object hoverOwner;
    private UnityEngine.Object focusedInfoOwner;
    private UsableItem focusedUsableItem;
    private bool shotGunLoaded;
    private IDisposable shotGunLoadedStateChangedSubscription;
    private IDisposable focusStateChangedSubscription;

    private void Awake()
    {
        if (shootSelection == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name != "ShootSelection")
                {
                    continue;
                }

                shootSelection = children[i].gameObject;
                break;
            }
        }

        if (shootPlayerButton == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name != "ShootPlayerBtn")
                {
                    continue;
                }

                shootPlayerButton = children[i].gameObject;
                break;
            }
        }

        if (shootEnemyButton == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name != "ShootEnemyBtn")
                {
                    continue;
                }

                shootEnemyButton = children[i].gameObject;
                break;
            }
        }

        if (useButton == null)
        {
            Transform foundUseButton = FindChildByName("UseBtn");
            useButton = foundUseButton != null ? foundUseButton.gameObject : null;
        }

        if (itemName == null)
        {
            Transform foundItemName = FindChildByName("ItemName");
            itemName = foundItemName != null ? foundItemName.GetComponent<TMP_Text>() : null;
        }

        if (itemDescription == null)
        {
            Transform foundItemDescription = FindChildByName("ItemDescription");
            itemDescription = foundItemDescription != null ? foundItemDescription.GetComponent<TMP_Text>() : null;
        }

        if (useButton == null && loadButton != null)
        {
            useButton = Instantiate(loadButton, loadButton.transform.parent);
            useButton.name = "UseBtn";
            SetButtonLabel(useButton, "Use");
        }

        if (loadButton != null && loadButton.TryGetComponent(out Button button))
        {
            button.onClick.AddListener(HandleLoadButtonClicked);
        }

        if (useButton != null && useButton.TryGetComponent(out Button use))
        {
            use.onClick.AddListener(HandleUseButtonClicked);
        }

        if (shootPlayerButton != null && shootPlayerButton.TryGetComponent(out Button shootPlayer))
        {
            shootPlayer.onClick.AddListener(HandleShootPlayerButtonClicked);
        }

        if (shootEnemyButton != null && shootEnemyButton.TryGetComponent(out Button shootEnemy))
        {
            shootEnemy.onClick.AddListener(HandleShootEnemyButtonClicked);
        }

        ConfigureButtonHoverOutlines();

        if (shotGunState == null)
        {
            shotGunState = FindObjectOfType<ShotGunState>();
        }

        shotGunLoaded = shotGunState != null && shotGunState.IsLoaded;

        HideHoveredItem();
        HideFocusedItemInfo();
        SetFocusActionsVisible(false);
    }

    private void OnEnable()
    {
        shotGunLoadedStateChangedSubscription = GameEventBus.Subscribe<ShotGunLoadedStateChangedEvent>(HandleShotGunLoadedStateChanged);
        focusStateChangedSubscription = GameEventBus.Subscribe<FocusStateChangedEvent>(HandleFocusStateChanged);
    }

    private void OnDisable()
    {
        shotGunLoadedStateChangedSubscription?.Dispose();
        shotGunLoadedStateChangedSubscription = null;
        focusStateChangedSubscription?.Dispose();
        focusStateChangedSubscription = null;
    }

    public void ShowHoveredItem(string displayName)
    {
        ShowHoveredItem(displayName, null, null);
    }

    public void ShowHoveredItem(string displayName, UnityEngine.Object owner)
    {
        ShowHoveredItem(displayName, owner, null);
    }

    public void ShowHoveredItem(string displayName, UnityEngine.Object owner, string description)
    {
        if (focusedInfoOwner != null)
        {
            return;
        }

        if (itemName == null)
        {
            return;
        }

        hoverOwner = owner;
        itemName.text = displayName;
        itemName.gameObject.SetActive(!string.IsNullOrWhiteSpace(displayName));
        if (itemDescription != null)
        {
            itemDescription.text = description ?? string.Empty;
            itemDescription.gameObject.SetActive(!string.IsNullOrWhiteSpace(description));
        }
    }

    public void HideHoveredItem()
    {
        HideHoveredItem(null);
    }

    public void HideHoveredItem(UnityEngine.Object owner)
    {
        if (focusedInfoOwner != null)
        {
            return;
        }

        if (owner != null && hoverOwner != owner)
        {
            return;
        }

        hoverOwner = null;
        if (itemName != null)
        {
            itemName.gameObject.SetActive(false);
        }

        if (itemDescription != null)
        {
            itemDescription.gameObject.SetActive(false);
        }
    }

    public void SetFocusActionsVisible(bool visible)
    {
        bool showUse = visible && focusedUsableItem != null && focusedUsableItem.CanUse;
        if (loadButton != null)
        {
            loadButton.SetActive(visible && !showUse && !shotGunLoaded);
        }

        if (useButton != null)
        {
            useButton.SetActive(showUse);
        }

        if (shootSelection != null)
        {
            shootSelection.SetActive(visible && !showUse && shotGunLoaded);
        }
    }

    private static void HandleLoadButtonClicked()
    {
        GameEventBus.Publish(new ReloadButtonClickedEvent());
    }

    private void HandleUseButtonClicked()
    {
        GameEventBus.Publish(new UseFocusedItemButtonClickedEvent());
        focusedUsableItem = null;
        SetFocusActionsVisible(false);
    }

    private static void HandleShootPlayerButtonClicked()
    {
        GameEventBus.Publish(new ShootPlayerButtonClickedEvent());
    }

    private static void HandleShootEnemyButtonClicked()
    {
        GameEventBus.Publish(new ShootEnemyButtonClickedEvent());
    }

    private void HandleShotGunLoadedStateChanged(ShotGunLoadedStateChangedEvent evt)
    {
        if (shotGunState != null && evt.ShotGunState != shotGunState)
        {
            return;
        }

        shotGunLoaded = evt.IsLoaded;
        SetFocusActionsVisible(false);
    }

    private void HandleFocusStateChanged(FocusStateChangedEvent evt)
    {
        focusedUsableItem = evt.Focused ? FindUsableItem(evt.FocusTarget) : null;
        if (evt.Focused)
        {
            ShowFocusedItemInfo(FindInspectableItem(evt.FocusTarget));
        }
        else
        {
            HideFocusedItemInfo();
        }
    }

    private void ShowFocusedItemInfo(InspectableItem inspectableItem)
    {
        focusedInfoOwner = inspectableItem;
        hoverOwner = null;

        string focusedItemName = inspectableItem != null ? inspectableItem.ItemName : null;
        string focusedItemDescription = inspectableItem != null ? inspectableItem.ItemDescription : null;

        if (itemName != null)
        {
            itemName.text = focusedItemName ?? string.Empty;
            itemName.gameObject.SetActive(!string.IsNullOrWhiteSpace(focusedItemName));
        }

        if (itemDescription != null)
        {
            itemDescription.text = focusedItemDescription ?? string.Empty;
            itemDescription.gameObject.SetActive(!string.IsNullOrWhiteSpace(focusedItemDescription));
        }
    }

    private void HideFocusedItemInfo()
    {
        HideFocusedItemInfo(null);
    }

    private void HideFocusedItemInfo(UnityEngine.Object owner)
    {
        focusedInfoOwner = null;
        if (itemName != null)
        {
            itemName.gameObject.SetActive(false);
        }

        if (itemDescription != null)
        {
            itemDescription.gameObject.SetActive(false);
        }
    }

    private void ConfigureButtonHoverOutlines()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].GetComponent<UIButtonHoverOutline>() != null)
            {
                continue;
            }

            buttons[i].gameObject.AddComponent<UIButtonHoverOutline>();
        }
    }

    private Transform FindChildByName(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    private static UsableItem FindUsableItem(UnityEngine.Object focusTarget)
    {
        if (focusTarget is UsableItem usableItem)
        {
            return usableItem;
        }

        if (focusTarget is Component component)
        {
            return component.GetComponent<UsableItem>() ?? component.GetComponentInParent<UsableItem>();
        }

        if (focusTarget is GameObject gameObject)
        {
            return gameObject.GetComponent<UsableItem>() ?? gameObject.GetComponentInParent<UsableItem>();
        }

        return null;
    }

    private static InspectableItem FindInspectableItem(UnityEngine.Object focusTarget)
    {
        if (focusTarget is InspectableItem inspectableItem)
        {
            return inspectableItem;
        }

        if (focusTarget is Component component)
        {
            return component.GetComponent<InspectableItem>() ?? component.GetComponentInParent<InspectableItem>();
        }

        if (focusTarget is GameObject gameObject)
        {
            return gameObject.GetComponent<InspectableItem>() ?? gameObject.GetComponentInParent<InspectableItem>();
        }

        return null;
    }

    private static void SetButtonLabel(GameObject buttonObject, string label)
    {
        TMP_Text text = buttonObject != null ? buttonObject.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null)
        {
            text.text = label;
        }
    }

    private void OnDestroy()
    {
        if (loadButton != null && loadButton.TryGetComponent(out Button button))
        {
            button.onClick.RemoveListener(HandleLoadButtonClicked);
        }

        if (useButton != null && useButton.TryGetComponent(out Button use))
        {
            use.onClick.RemoveListener(HandleUseButtonClicked);
        }

        if (shootPlayerButton != null && shootPlayerButton.TryGetComponent(out Button shootPlayer))
        {
            shootPlayer.onClick.RemoveListener(HandleShootPlayerButtonClicked);
        }

        if (shootEnemyButton != null && shootEnemyButton.TryGetComponent(out Button shootEnemy))
        {
            shootEnemy.onClick.RemoveListener(HandleShootEnemyButtonClicked);
        }
    }
}
