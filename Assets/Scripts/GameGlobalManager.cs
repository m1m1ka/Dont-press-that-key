using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameGlobalManager : MonoBehaviour
{
    private const string AutoCreatedObjectName = "GameGlobalManager";

    [Header("Health")]
    [SerializeField, Min(1)] private int playerMaxHealth = 3;
    [SerializeField, Min(1)] private int enemyMaxHealth = 3;

    [Header("Level Progress")]
    [SerializeField, Min(1)] private int currentLevel = 1;
    [SerializeField, Min(0)] private int completedLevelCount;

    private IDisposable shotGunHitResolvedSubscription;
    private int playerCurrentHealth;
    private int enemyCurrentHealth;

    public int PlayerCurrentHealth => playerCurrentHealth;
    public int PlayerMaxHealth => playerMaxHealth;
    public int EnemyCurrentHealth => enemyCurrentHealth;
    public int EnemyMaxHealth => enemyMaxHealth;
    public int CurrentLevel => currentLevel;
    public int CompletedLevelCount => completedLevelCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (FindFirstObjectByType<GameGlobalManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(AutoCreatedObjectName);
        managerObject.AddComponent<GameGlobalManager>();
    }

    private void Awake()
    {
        playerCurrentHealth = Mathf.Clamp(playerCurrentHealth > 0 ? playerCurrentHealth : playerMaxHealth, 0, playerMaxHealth);
        enemyCurrentHealth = Mathf.Clamp(enemyCurrentHealth > 0 ? enemyCurrentHealth : enemyMaxHealth, 0, enemyMaxHealth);
    }

    private void OnEnable()
    {
        shotGunHitResolvedSubscription = GameEventBus.Subscribe<ShotGunHitResolvedEvent>(HandleShotGunHitResolved);
    }

    private void Start()
    {
        PublishInitialState();
    }

    private void OnDisable()
    {
        shotGunHitResolvedSubscription?.Dispose();
        shotGunHitResolvedSubscription = null;
    }

    public void DamageCharacter(GameCharacter character, int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        SetCharacterHealth(character, GetCurrentHealth(character) - damage);
    }

    public void HealCharacter(GameCharacter character, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetCharacterHealth(character, GetCurrentHealth(character) + amount);
    }

    public void RestoreCharacterHealth(GameCharacter character)
    {
        SetCharacterHealth(character, GetMaxHealth(character));
    }

    public void RestoreAllHealth()
    {
        SetCharacterHealth(GameCharacter.Player, playerMaxHealth);
        SetCharacterHealth(GameCharacter.Enemy, enemyMaxHealth);
    }

    public void SetCurrentLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        PublishLevelProgressChanged();
    }

    public void CompleteCurrentLevel()
    {
        completedLevelCount = Mathf.Max(completedLevelCount, currentLevel);
        currentLevel++;
        PublishLevelProgressChanged();
    }

    private void HandleShotGunHitResolved(ShotGunHitResolvedEvent evt)
    {
        DamageCharacter(evt.Target, evt.Damage);
    }

    private void SetCharacterHealth(GameCharacter character, int health)
    {
        int maxHealth = GetMaxHealth(character);
        int previousHealth = GetCurrentHealth(character);
        int nextHealth = Mathf.Clamp(health, 0, maxHealth);
        if (previousHealth == nextHealth)
        {
            return;
        }

        switch (character)
        {
            case GameCharacter.Player:
                playerCurrentHealth = nextHealth;
                break;
            case GameCharacter.Enemy:
                enemyCurrentHealth = nextHealth;
                break;
        }

        GameEventBus.Publish(new CharacterHealthChangedEvent(character, nextHealth, maxHealth, nextHealth - previousHealth));

        if (nextHealth <= 0)
        {
            GameEventBus.Publish(new CharacterDiedEvent(character));
        }
    }

    private int GetCurrentHealth(GameCharacter character)
    {
        switch (character)
        {
            case GameCharacter.Player:
                return playerCurrentHealth;
            case GameCharacter.Enemy:
                return enemyCurrentHealth;
            default:
                return 0;
        }
    }

    private int GetMaxHealth(GameCharacter character)
    {
        switch (character)
        {
            case GameCharacter.Player:
                return playerMaxHealth;
            case GameCharacter.Enemy:
                return enemyMaxHealth;
            default:
                return 0;
        }
    }

    private void PublishInitialState()
    {
        GameEventBus.Publish(new CharacterHealthChangedEvent(GameCharacter.Player, playerCurrentHealth, playerMaxHealth, 0));
        GameEventBus.Publish(new CharacterHealthChangedEvent(GameCharacter.Enemy, enemyCurrentHealth, enemyMaxHealth, 0));
        PublishLevelProgressChanged();
    }

    private void PublishLevelProgressChanged()
    {
        GameEventBus.Publish(new LevelProgressChangedEvent(currentLevel, completedLevelCount));
    }
}
