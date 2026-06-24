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
    private readonly List<ShotGunShellKind> loadedShells = new List<ShotGunShellKind>();
    private int nextShellIndex;

    public bool IsLoaded => isLoaded;
    public int LoadedLiveShellCount => loadedLiveShellCount;
    public int LoadedBlankShellCount => loadedBlankShellCount;
    public int RemainingShellCount => Mathf.Max(0, loadedShells.Count - nextShellIndex);

    private void OnEnable()
    {
        gameFlowStartedSubscription = GameEventBus.Subscribe<GameFlowStartedEvent>(HandleGameFlowStarted);
        PublishLoadedState();
    }

    private void OnDisable()
    {
        gameFlowStartedSubscription?.Dispose();
        gameFlowStartedSubscription = null;
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
    }

    public ShotGunShellKind ConsumeNextShell()
    {
        if (nextShellIndex >= loadedShells.Count)
        {
            SetLoaded(false);
            return ShotGunShellKind.Blank;
        }

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
}
