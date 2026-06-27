using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class NotificationScreenController : MonoBehaviour
{
    [Serializable]
    private sealed class HealthIconUi
    {
        [SerializeField] private Transform hpIconsRoot;
        [SerializeField] private GameObject iconPrefab;
        [SerializeField] private string infoRootName;
        [SerializeField] private string alternateInfoRootName;
        [SerializeField] private string iconsRootName = "HpIcons";
        [SerializeField, Min(0.01f)] private float blinkFadeDuration = 0.12f;
        [SerializeField] private bool removeLostIconsFromStart;

        private readonly List<GameObject> icons = new List<GameObject>();
        private GameObject templateIcon;

        public HealthIconUi()
        {
        }

        public HealthIconUi(string infoRootName, string alternateInfoRootName = null, bool removeLostIconsFromStart = false)
        {
            this.infoRootName = infoRootName;
            this.alternateInfoRootName = alternateInfoRootName;
            this.removeLostIconsFromStart = removeLostIconsFromStart;
        }

        public void Resolve(Transform searchRoot)
        {
            if (hpIconsRoot == null && !string.IsNullOrWhiteSpace(infoRootName))
            {
                Transform infoRoot = FindChildByName(searchRoot, infoRootName);
                if (infoRoot == null && !string.IsNullOrWhiteSpace(alternateInfoRootName))
                {
                    infoRoot = FindChildByName(searchRoot, alternateInfoRootName);
                }

                if (infoRoot == null)
                {
                    GameObject foundObject = GameObject.Find(infoRootName);
                    if (foundObject == null && !string.IsNullOrWhiteSpace(alternateInfoRootName))
                    {
                        foundObject = GameObject.Find(alternateInfoRootName);
                    }

                    infoRoot = foundObject != null ? foundObject.transform : null;
                }

                hpIconsRoot = infoRoot != null ? FindChildByName(infoRoot, iconsRootName) : null;
            }

            if (templateIcon == null && hpIconsRoot != null && hpIconsRoot.childCount > 0)
            {
                templateIcon = hpIconsRoot.GetChild(0).gameObject;
                if (iconPrefab == null)
                {
                    iconPrefab = templateIcon;
                }
            }
        }

        public void UpdateHealth(MonoBehaviour coroutineOwner, Transform searchRoot, int currentHealth, int maxHealth, bool animateLoss)
        {
            Resolve(searchRoot);

            if (hpIconsRoot == null || iconPrefab == null || maxHealth <= 0)
            {
                return;
            }

            if (templateIcon != null)
            {
                templateIcon.SetActive(false);
            }

            int visibleHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            RemoveMissingIcons();

            while (icons.Count < visibleHealth)
            {
                GameObject icon = Instantiate(iconPrefab, hpIconsRoot);
                icon.name = $"{iconPrefab.name}_{icons.Count + 1}";
                icon.SetActive(true);
                SetIconAlpha(icon, 1f);
                icons.Add(icon);
            }

            if (icons.Count <= visibleHealth)
            {
                return;
            }

            while (icons.Count > visibleHealth)
            {
                int removeIndex = removeLostIconsFromStart ? 0 : icons.Count - 1;
                GameObject icon = icons[removeIndex];
                icons.RemoveAt(removeIndex);
                if (icon == null)
                {
                    continue;
                }

                if (animateLoss && coroutineOwner != null && coroutineOwner.isActiveAndEnabled)
                {
                    coroutineOwner.StartCoroutine(PlayLostIconAnimation(icon));
                }
                else
                {
                    Destroy(icon);
                }
            }
        }

        private void ClearIcons()
        {
            for (int i = icons.Count - 1; i >= 0; i--)
            {
                GameObject icon = icons[i];
                if (icon != null)
                {
                    Destroy(icon);
                }
            }

            icons.Clear();

            if (hpIconsRoot == null)
            {
                return;
            }

            for (int i = hpIconsRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = hpIconsRoot.GetChild(i).gameObject;
                if (child == templateIcon)
                {
                    continue;
                }

                Destroy(child);
            }
        }

        private void RemoveMissingIcons()
        {
            for (int i = icons.Count - 1; i >= 0; i--)
            {
                if (icons[i] == null)
                {
                    icons.RemoveAt(i);
                }
            }
        }

        private IEnumerator PlayLostIconAnimation(GameObject icon)
        {
            for (int blinkIndex = 0; blinkIndex < 3; blinkIndex++)
            {
                yield return FadeIconAlpha(icon, 1f, 0f, blinkFadeDuration);
                if (icon == null)
                {
                    yield break;
                }

                if (blinkIndex < 2)
                {
                    SetIconAlpha(icon, 1f);
                }
            }

            if (icon != null)
            {
                Destroy(icon);
            }
        }

        private IEnumerator FadeIconAlpha(GameObject icon, float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            SetIconAlpha(icon, fromAlpha);

            while (elapsed < duration)
            {
                if (icon == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetIconAlpha(icon, Mathf.Lerp(fromAlpha, toAlpha, t));
                yield return null;
            }

            SetIconAlpha(icon, toAlpha);
        }

        private static void SetIconAlpha(GameObject icon, float alpha)
        {
            if (icon == null)
            {
                return;
            }

            Image[] images = icon.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Color color = images[i].color;
                color.a = alpha;
                images[i].color = color;
            }
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                {
                    return children[i];
                }
            }

            return null;
        }
    }

    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject screenRoot;
    [SerializeField] private string regularMessage = "0 1";
    [SerializeField] private GameObject infoRoot;
    [SerializeField] private GameObject playerHpInfoRoot;
    [SerializeField] private GameObject enemyHpInfoRoot;

    [Header("Health UI")]
    [SerializeField] private HealthIconUi playerHealthUi = new HealthIconUi("PlayerHpInfo");
    [SerializeField] private HealthIconUi enemyHealthUi = new HealthIconUi("EnemyHpInfo", "EnemeyHpInfo", true);

    private IDisposable characterHealthChangedSubscription;

    private void Awake()
    {
        ResolveDisplayRoots();

        if (messageText == null)
        {
            messageText = infoRoot != null
                ? infoRoot.GetComponentInChildren<TMP_Text>(true)
                : GetComponentInChildren<TMP_Text>(true);
        }

        if (screenRoot == null && infoRoot != null)
        {
            screenRoot = infoRoot;
        }
        else if (screenRoot == null && messageText != null)
        {
            screenRoot = messageText.gameObject;
        }

        ShowRegular();
    }

    private void OnEnable()
    {
        characterHealthChangedSubscription = GameEventBus.Subscribe<CharacterHealthChangedEvent>(HandleCharacterHealthChanged);
    }

    private void OnDisable()
    {
        characterHealthChangedSubscription?.Dispose();
        characterHealthChangedSubscription = null;
    }

    public void Show(string message)
    {
        SetInfoModeActive(true);

        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    public void Clear()
    {
        if (messageText != null)
        {
            messageText.text = string.Empty;
        }
    }

    public void ShowRegular()
    {
        SetInfoModeActive(false);
        if (messageText != null)
        {
            messageText.text = regularMessage;
        }
    }

    public void SetRegularMessage(string message)
    {
        regularMessage = message;
        ShowRegular();
    }

    private void HandleCharacterHealthChanged(CharacterHealthChangedEvent evt)
    {
        switch (evt.Character)
        {
            case GameCharacter.Player:
                playerHealthUi?.UpdateHealth(this, transform, evt.CurrentHealth, evt.MaxHealth, evt.Delta < 0);
                break;
            case GameCharacter.Enemy:
                enemyHealthUi?.UpdateHealth(this, transform, evt.CurrentHealth, evt.MaxHealth, evt.Delta < 0);
                break;
        }
    }

    private void ResolveDisplayRoots()
    {
        if (infoRoot == null)
        {
            Transform foundInfoRoot = FindChildByName(transform, "Info");
            infoRoot = foundInfoRoot != null ? foundInfoRoot.gameObject : null;
        }

        if (playerHpInfoRoot == null)
        {
            Transform foundPlayerHpRoot = FindChildByName(transform, "PlayerHpInfo");
            playerHpInfoRoot = foundPlayerHpRoot != null ? foundPlayerHpRoot.gameObject : null;
        }

        if (enemyHpInfoRoot == null)
        {
            Transform foundEnemyHpRoot = FindChildByName(transform, "EnemyHpInfo");
            if (foundEnemyHpRoot == null)
            {
                foundEnemyHpRoot = FindChildByName(transform, "EnemeyHpInfo");
            }

            enemyHpInfoRoot = foundEnemyHpRoot != null ? foundEnemyHpRoot.gameObject : null;
        }
    }

    private void SetInfoModeActive(bool active)
    {
        GameObject targetInfoRoot = infoRoot != null ? infoRoot : screenRoot;
        if (targetInfoRoot != null)
        {
            targetInfoRoot.SetActive(active);
        }

        if (playerHpInfoRoot != null)
        {
            playerHpInfoRoot.SetActive(!active);
        }

        if (enemyHpInfoRoot != null)
        {
            enemyHpInfoRoot.SetActive(!active);
        }
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }
}
