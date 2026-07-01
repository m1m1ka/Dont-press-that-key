using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShotGunState : MonoBehaviour
{
    [SerializeField] private bool isLoaded;
    [SerializeField, Min(0)] private int loadedLiveShellCount;
    [SerializeField, Min(0)] private int loadedBlankShellCount;

    private IDisposable gameFlowStartedSubscription;
    private IDisposable swapCurrentAndNextShellSubscription;
    private IDisposable invertCurrentShellSubscription;
    private readonly List<ShotGunShellKind> loadedShells = new List<ShotGunShellKind>();
    private int nextShellIndex;

    public bool IsLoaded => isLoaded;
    public int LoadedLiveShellCount => loadedLiveShellCount;
    public int LoadedBlankShellCount => loadedBlankShellCount;
    public int LoadedShellCount => loadedShells.Count;
    public int RemainingShellCount => Mathf.Max(0, loadedShells.Count - nextShellIndex);

    private void OnEnable()
    {
        gameFlowStartedSubscription = GameEventBus.Subscribe<GameFlowStartedEvent>(HandleGameFlowStarted);
        swapCurrentAndNextShellSubscription =
            GameEventBus.Subscribe<SwapCurrentAndNextShotGunShellRequestedEvent>(HandleSwapCurrentAndNextShellRequested);
        invertCurrentShellSubscription =
            GameEventBus.Subscribe<InvertCurrentShotGunShellRequestedEvent>(HandleInvertCurrentShellRequested);
        PublishLoadedState();
    }

    private void OnDisable()
    {
        gameFlowStartedSubscription?.Dispose();
        gameFlowStartedSubscription = null;
        swapCurrentAndNextShellSubscription?.Dispose();
        swapCurrentAndNextShellSubscription = null;
        invertCurrentShellSubscription?.Dispose();
        invertCurrentShellSubscription = null;
    }

    public void SetLoaded(bool loaded)
    {
        if (isLoaded == loaded)
        {
            PublishLoadedState();
            return;
        }

        isLoaded = loaded;
        PublishLoadedState();
    }

    public void MarkLoaded()
    {
        loadedShells.Clear();
        loadedShells.Add(ShotGunShellKind.Live);
        loadedLiveShellCount = 1;
        loadedBlankShellCount = 0;
        nextShellIndex = 0;
        SetLoaded(true);
        GameEventBus.Publish(new ShotGunShellsLoadedEvent(this, new List<ShotGunShellKind>(loadedShells)));
    }

    public void ClearLoaded()
    {
        loadedShells.Clear();
        loadedLiveShellCount = 0;
        loadedBlankShellCount = 0;
        nextShellIndex = 0;
        SetLoaded(false);
    }

    public void LoadShells(IReadOnlyList<ShotGunShellKind> shellKinds)
    {
        loadedShells.Clear();
        loadedLiveShellCount = 0;
        loadedBlankShellCount = 0;
        nextShellIndex = 0;

        if (shellKinds != null)
        {
            for (int i = 0; i < shellKinds.Count; i++)
            {
                AddLoadedShell(shellKinds[i]);
            }
        }

        SetLoaded(loadedShells.Count > 0);
        GameEventBus.Publish(new ShotGunShellsLoadedEvent(this, new List<ShotGunShellKind>(loadedShells)));
    }

    public ShotGunShellKind ConsumeNextShell()
    {
        return ConsumeNextShell(out _, out _);
    }

    public bool TryPeekNextShell(out ShotGunShellKind shellKind)
    {
        if (nextShellIndex < 0 || nextShellIndex >= loadedShells.Count)
        {
            shellKind = ShotGunShellKind.Blank;
            return false;
        }

        shellKind = loadedShells[nextShellIndex];
        return true;
    }

    public bool TrySwapCurrentAndNextShell()
    {
        int nextIndex = nextShellIndex + 1;
        if (nextShellIndex < 0 || nextIndex >= loadedShells.Count)
        {
            return false;
        }

        ShotGunShellKind currentShellKind = loadedShells[nextShellIndex];
        loadedShells[nextShellIndex] = loadedShells[nextIndex];
        loadedShells[nextIndex] = currentShellKind;
        PublishShellsLoaded();
        return true;
    }

    public bool TryInvertCurrentShell()
    {
        if (nextShellIndex < 0 || nextShellIndex >= loadedShells.Count)
        {
            return false;
        }

        ShotGunShellKind previousShellKind = loadedShells[nextShellIndex];
        ShotGunShellKind nextShellKind = InvertShellKind(previousShellKind);
        if (previousShellKind == nextShellKind)
        {
            return false;
        }

        loadedShells[nextShellIndex] = nextShellKind;
        DecrementLoadedShellCount(previousShellKind);
        AddLoadedShellCount(nextShellKind);
        PublishLoadedState();
        PublishShellsLoaded();
        return true;
    }

    public ShotGunShellKind ConsumeNextShell(out int shellIndex, out int shellCount)
    {
        if (nextShellIndex >= loadedShells.Count)
        {
            shellIndex = -1;
            shellCount = loadedShells.Count;
            SetLoaded(false);
            return ShotGunShellKind.Blank;
        }

        shellIndex = nextShellIndex;
        shellCount = loadedShells.Count;
        ShotGunShellKind shellKind = loadedShells[nextShellIndex];
        nextShellIndex++;
        DecrementLoadedShellCount(shellKind);
        SetLoaded(nextShellIndex < loadedShells.Count);
        return shellKind;
    }

    public string GetLoadedShellSummary()
    {
        return $"Live={loadedLiveShellCount}, Blank={loadedBlankShellCount}, Remaining={RemainingShellCount}";
    }

    private void AddLoadedShell(ShotGunShellKind shellKind)
    {
        loadedShells.Add(shellKind);
        AddLoadedShellCount(shellKind);
    }

    private void AddLoadedShellCount(ShotGunShellKind shellKind)
    {
        switch (shellKind)
        {
            case ShotGunShellKind.Live:
                loadedLiveShellCount++;
                break;
            case ShotGunShellKind.Blank:
                loadedBlankShellCount++;
                break;
        }
    }

    private void DecrementLoadedShellCount(ShotGunShellKind shellKind)
    {
        switch (shellKind)
        {
            case ShotGunShellKind.Live:
                loadedLiveShellCount = Mathf.Max(0, loadedLiveShellCount - 1);
                break;
            case ShotGunShellKind.Blank:
                loadedBlankShellCount = Mathf.Max(0, loadedBlankShellCount - 1);
                break;
        }
    }

    private void HandleGameFlowStarted(GameFlowStartedEvent evt)
    {
        ClearLoaded();
    }

    private void PublishLoadedState()
    {
        GameEventBus.Publish(new ShotGunLoadedStateChangedEvent(
            this,
            isLoaded,
            loadedLiveShellCount,
            loadedBlankShellCount));
    }

    private void HandleSwapCurrentAndNextShellRequested(SwapCurrentAndNextShotGunShellRequestedEvent evt)
    {
        TrySwapCurrentAndNextShell();
    }

    private void HandleInvertCurrentShellRequested(InvertCurrentShotGunShellRequestedEvent evt)
    {
        TryInvertCurrentShell();
    }

    private void PublishShellsLoaded()
    {
        GameEventBus.Publish(new ShotGunShellsLoadedEvent(this, new List<ShotGunShellKind>(loadedShells)));
    }

    private static ShotGunShellKind InvertShellKind(ShotGunShellKind shellKind)
    {
        switch (shellKind)
        {
            case ShotGunShellKind.Live:
                return ShotGunShellKind.Blank;
            case ShotGunShellKind.Blank:
                return ShotGunShellKind.Live;
            default:
                return shellKind;
        }
    }
}
