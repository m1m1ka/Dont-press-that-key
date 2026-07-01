using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BulletInfoUIController : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite unknownBullet;
    [SerializeField] private Sprite liveBullet;
    [SerializeField] private Sprite blankBullet;

    [Header("UI")]
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private Image[] bulletIcons;
    [SerializeField, FormerlySerializedAs("tag")] private RectTransform currentBulletTag;
    [SerializeField, Range(0f, 1f)] private float firedIconAlpha = 0.45f;
    [SerializeField] private bool showOnlyWhenLoaded = true;

    private readonly List<Image> runtimeBulletIcons = new List<Image>();
    private readonly List<ShotGunShellKind> loadedShellKinds = new List<ShotGunShellKind>();
    private readonly List<bool> revealedShellIcons = new List<bool>();
    private IDisposable shellLoadStartedSubscription;
    private IDisposable shellLoadCompletedSubscription;
    private IDisposable shellsLoadedSubscription;
    private IDisposable shotGunFiredSubscription;
    private IDisposable gameFlowStartedSubscription;
    private IDisposable revealCurrentShellSubscription;
    private int nextShellIndexToReveal;
    private int visibleShellCount;

    private void Awake()
    {
        if (iconContainer == null)
        {
            iconContainer = transform as RectTransform;
        }

        if (currentBulletTag == null)
        {
            Transform foundTag = FindChildByName("Tag");
            currentBulletTag = foundTag != null ? foundTag as RectTransform : null;
        }

        CacheBulletIcons();
        ClearIcons();
    }

    private void OnEnable()
    {
        shellLoadStartedSubscription = GameEventBus.Subscribe<ShotGunShellLoadStartedEvent>(HandleShellLoadStarted);
        shellLoadCompletedSubscription = GameEventBus.Subscribe<ShotGunShellLoadCompletedEvent>(HandleShellLoadCompleted);
        shellsLoadedSubscription = GameEventBus.Subscribe<ShotGunShellsLoadedEvent>(HandleShellsLoaded);
        shotGunFiredSubscription = GameEventBus.Subscribe<ShotGunFiredEvent>(HandleShotGunFired);
        gameFlowStartedSubscription = GameEventBus.Subscribe<GameFlowStartedEvent>(_ => ClearIcons());
        revealCurrentShellSubscription = GameEventBus.Subscribe<RevealCurrentShotGunShellRequestedEvent>(HandleRevealCurrentShellRequested);
    }

    private void OnDisable()
    {
        shellLoadStartedSubscription?.Dispose();
        shellLoadStartedSubscription = null;
        shellLoadCompletedSubscription?.Dispose();
        shellLoadCompletedSubscription = null;
        shellsLoadedSubscription?.Dispose();
        shellsLoadedSubscription = null;
        shotGunFiredSubscription?.Dispose();
        shotGunFiredSubscription = null;
        gameFlowStartedSubscription?.Dispose();
        gameFlowStartedSubscription = null;
        revealCurrentShellSubscription?.Dispose();
        revealCurrentShellSubscription = null;
    }

    private void HandleShellLoadStarted(ShotGunShellLoadStartedEvent evt)
    {
        if (evt.ShellIndex != 0)
        {
            return;
        }

        BeginIncrementalLoad();
    }

    private void HandleShellLoadCompleted(ShotGunShellLoadCompletedEvent evt)
    {
        ShowLoadedUnknownIcon(evt.ShellIndex);
    }

    private void HandleShellsLoaded(ShotGunShellsLoadedEvent evt)
    {
        int shellCount = evt.ShellKinds != null ? evt.ShellKinds.Count : 0;
        if (visibleShellCount > 0)
        {
            StoreLoadedShellKinds(evt.ShellKinds);
            RefreshRevealedShellIcons();
            return;
        }

        BuildUnknownIcons(shellCount);
        StoreLoadedShellKinds(evt.ShellKinds);
        RefreshRevealedShellIcons();
    }

    private void HandleShotGunFired(ShotGunFiredEvent evt)
    {
        if (nextShellIndexToReveal < 0
            || nextShellIndexToReveal >= runtimeBulletIcons.Count
            || nextShellIndexToReveal >= visibleShellCount)
        {
            return;
        }

        Image icon = runtimeBulletIcons[nextShellIndexToReveal];
        if (icon == null)
        {
            return;
        }

        icon.sprite = GetShellSprite(evt.ShellKind);
        SetImageAlpha(icon, firedIconAlpha);
        SetShellIconRevealed(nextShellIndexToReveal, true);
        nextShellIndexToReveal++;
        UpdateCurrentBulletTag();
    }

    private void HandleRevealCurrentShellRequested(RevealCurrentShotGunShellRequestedEvent evt)
    {
        if (nextShellIndexToReveal < 0
            || nextShellIndexToReveal >= runtimeBulletIcons.Count
            || nextShellIndexToReveal >= visibleShellCount
            || nextShellIndexToReveal >= loadedShellKinds.Count)
        {
            return;
        }

        Image icon = runtimeBulletIcons[nextShellIndexToReveal];
        if (icon == null)
        {
            return;
        }

        icon.sprite = GetShellSprite(loadedShellKinds[nextShellIndexToReveal]);
        SetImageAlpha(icon, 1f);
        icon.gameObject.SetActive(true);
        SetShellIconRevealed(nextShellIndexToReveal, true);
        UpdateCurrentBulletTag();
    }

    private void BuildUnknownIcons(int shellCount)
    {
        ClearIcons();
        if (shellCount <= 0)
        {
            return;
        }

        nextShellIndexToReveal = 0;
        SetVisible(true);
        int targetVisibleShellCount = Mathf.Min(shellCount, runtimeBulletIcons.Count);
        for (int i = 0; i < targetVisibleShellCount; i++)
        {
            ShowLoadedUnknownIcon(i);
        }

        if (shellCount > runtimeBulletIcons.Count)
        {
            Debug.LogWarning(
                $"BulletInfoUI has {runtimeBulletIcons.Count} bullet icons, but {shellCount} shells were loaded.",
                this);
        }

        if (targetVisibleShellCount > 0)
        {
            UpdateCurrentBulletTag();
        }
    }

    private void BeginIncrementalLoad()
    {
        ClearIcons();
        SetVisible(true);
        nextShellIndexToReveal = 0;
    }

    private void ShowLoadedUnknownIcon(int shellIndex)
    {
        if (shellIndex < 0 || shellIndex >= runtimeBulletIcons.Count)
        {
            return;
        }

        Image icon = runtimeBulletIcons[shellIndex];
        if (icon == null)
        {
            return;
        }

        icon.sprite = unknownBullet;
        SetImageAlpha(icon, 1f);
        icon.gameObject.SetActive(true);
        SetShellIconRevealed(shellIndex, false);
        visibleShellCount = Mathf.Max(visibleShellCount, shellIndex + 1);

        if (shellIndex == nextShellIndexToReveal)
        {
            UpdateCurrentBulletTag();
        }
    }

    private void ClearIcons()
    {
        CacheBulletIcons();
        for (int i = 0; i < runtimeBulletIcons.Count; i++)
        {
            Image icon = runtimeBulletIcons[i];
            if (icon != null)
            {
                icon.sprite = unknownBullet;
                SetImageAlpha(icon, 1f);
                icon.gameObject.SetActive(false);
            }
        }

        nextShellIndexToReveal = 0;
        visibleShellCount = 0;
        loadedShellKinds.Clear();
        revealedShellIcons.Clear();
        if (currentBulletTag != null)
        {
            currentBulletTag.gameObject.SetActive(false);
        }

        SetVisible(!showOnlyWhenLoaded);
    }

    private Sprite GetShellSprite(ShotGunShellKind shellKind)
    {
        switch (shellKind)
        {
            case ShotGunShellKind.Live:
                return liveBullet != null ? liveBullet : unknownBullet;
            case ShotGunShellKind.Blank:
                return blankBullet != null ? blankBullet : unknownBullet;
            default:
                return unknownBullet;
        }
    }

    private void AlignTagToIcon(RectTransform icon)
    {
        if (currentBulletTag == null || icon == null)
        {
            return;
        }

        currentBulletTag.gameObject.SetActive(true);
        Vector3 tagPosition = currentBulletTag.position;
        tagPosition.x = icon.position.x;
        currentBulletTag.position = tagPosition;
    }

    private void UpdateCurrentBulletTag()
    {
        if (currentBulletTag == null)
        {
            return;
        }

        if (nextShellIndexToReveal < 0
            || nextShellIndexToReveal >= runtimeBulletIcons.Count
            || nextShellIndexToReveal >= visibleShellCount)
        {
            currentBulletTag.gameObject.SetActive(false);
            return;
        }

        Image nextIcon = runtimeBulletIcons[nextShellIndexToReveal];
        if (nextIcon == null || !nextIcon.gameObject.activeInHierarchy)
        {
            currentBulletTag.gameObject.SetActive(false);
            return;
        }

        AlignTagToIcon(nextIcon.rectTransform);
    }

    private void CacheBulletIcons()
    {
        runtimeBulletIcons.Clear();
        if (bulletIcons != null && bulletIcons.Length > 0)
        {
            for (int i = 0; i < bulletIcons.Length; i++)
            {
                if (bulletIcons[i] != null && bulletIcons[i].rectTransform != currentBulletTag)
                {
                    runtimeBulletIcons.Add(bulletIcons[i]);
                }
            }

            return;
        }

        if (iconContainer == null)
        {
            return;
        }

        Image[] childImages = iconContainer.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < childImages.Length; i++)
        {
            if (childImages[i] == null || childImages[i].rectTransform == currentBulletTag)
            {
                continue;
            }

            runtimeBulletIcons.Add(childImages[i]);
        }
    }

    private void StoreLoadedShellKinds(IReadOnlyList<ShotGunShellKind> shellKinds)
    {
        loadedShellKinds.Clear();
        if (shellKinds == null)
        {
            return;
        }

        for (int i = 0; i < shellKinds.Count; i++)
        {
            loadedShellKinds.Add(shellKinds[i]);
        }
    }

    private void RefreshRevealedShellIcons()
    {
        int refreshCount = Mathf.Min(
            Mathf.Min(revealedShellIcons.Count, loadedShellKinds.Count),
            runtimeBulletIcons.Count);
        for (int i = 0; i < refreshCount; i++)
        {
            if (!revealedShellIcons[i])
            {
                continue;
            }

            Image icon = runtimeBulletIcons[i];
            if (icon != null)
            {
                icon.sprite = GetShellSprite(loadedShellKinds[i]);
            }
        }
    }

    private void SetShellIconRevealed(int shellIndex, bool revealed)
    {
        if (shellIndex < 0)
        {
            return;
        }

        while (revealedShellIcons.Count <= shellIndex)
        {
            revealedShellIcons.Add(false);
        }

        revealedShellIcons[shellIndex] = revealed;
    }

    private Transform FindChildByName(string childName)
    {
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

    private void SetVisible(bool visible)
    {
        if (!showOnlyWhenLoaded || iconContainer == null || iconContainer.gameObject == gameObject)
        {
            return;
        }

        iconContainer.gameObject.SetActive(visible);
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
