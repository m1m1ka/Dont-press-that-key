using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InteractionUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private GameObject loadButton;
    [SerializeField] private GameObject shootSelection;
    [SerializeField] private GameObject shootPlayerButton;
    [SerializeField] private GameObject shootEnemyButton;
    [SerializeField] private ShotGunState shotGunState;

    private UnityEngine.Object hoverOwner;
    private bool shotGunLoaded;
    private IDisposable shotGunLoadedStateChangedSubscription;

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

        if (loadButton != null && loadButton.TryGetComponent(out Button button))
        {
            button.onClick.AddListener(HandleLoadButtonClicked);
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
        SetFocusActionsVisible(false);
    }

    private void OnEnable()
    {
        shotGunLoadedStateChangedSubscription = GameEventBus.Subscribe<ShotGunLoadedStateChangedEvent>(HandleShotGunLoadedStateChanged);
    }

    private void OnDisable()
    {
        shotGunLoadedStateChangedSubscription?.Dispose();
        shotGunLoadedStateChangedSubscription = null;
    }

    public void ShowHoveredItem(string displayName)
    {
        ShowHoveredItem(displayName, null);
    }

    public void ShowHoveredItem(string displayName, UnityEngine.Object owner)
    {
        if (itemName == null)
        {
            return;
        }

        hoverOwner = owner;
        itemName.text = displayName;
        itemName.gameObject.SetActive(!string.IsNullOrWhiteSpace(displayName));
    }

    public void HideHoveredItem()
    {
        HideHoveredItem(null);
    }

    public void HideHoveredItem(UnityEngine.Object owner)
    {
        if (owner != null && hoverOwner != owner)
        {
            return;
        }

        hoverOwner = null;
        if (itemName != null)
        {
            itemName.gameObject.SetActive(false);
        }
    }

    public void SetFocusActionsVisible(bool visible)
    {
        if (loadButton != null)
        {
            loadButton.SetActive(visible && !shotGunLoaded);
        }

        if (shootSelection != null)
        {
            shootSelection.SetActive(visible && shotGunLoaded);
        }
    }

    private static void HandleLoadButtonClicked()
    {
        GameEventBus.Publish(new ReloadButtonClickedEvent());
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

    private void OnDestroy()
    {
        if (loadButton != null && loadButton.TryGetComponent(out Button button))
        {
            button.onClick.RemoveListener(HandleLoadButtonClicked);
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
