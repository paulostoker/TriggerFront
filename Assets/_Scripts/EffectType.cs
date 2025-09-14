// _Scripts/Effects/EffectType.cs
public enum ModifierLogic
{
    Additive,
    ResultMap,
    SetDamage,
    ApplyEffect,
    SearchDeck
}
public enum EffectTarget
{
    Self,
    Target,
    AllAllies,
    AllEnemies,
    AllyInArea,
    EnemyInArea,
    NextAlly,
    TargetedAlly
}
public enum ModifierType
{
    AttackDice,
    DefenseDice,
    Movement,
    WeaponRange,
    Damage,
    ForbidAttack,
    DamageScale,
    AllowWallbang,
    WallbangDamage,
    HasOffAngleCardActive,
    CanCollateralDamage,
    CanWidePeek,
    DamageReduction,
    CanTransferEnergy,
    CanCounterAttack,
    RequiresAllyTarget,
    ModifyActionCharges,
    TriggersEnergyEquipMode,
    SprayTransfer,
    TriggersPreFireRoll,
    DrawCards,

}

public enum ConditionType
{
    NoKillsInRound,
    LastManStanding,
    TargetIsOnBombsite,
    ProximityToAllyDeath,
    MinDistanceToAllies,
    AllAlliesAlive,
    HasNotActed,
    FirstTeamAction,
    HasNotMoved,
    HasEnergyEquipped,
    HasCarryTarget,
    HasInGameLeaderTarget,
    IsUsingAutomaticWeapon, 
    IsNotInEcoMode  

}

[System.Flags]
public enum ActionType
{
    None = 0,
    Any = 1,
    Attack = 2,
    Movement = 4,
    GetTargeted = 8,
    TakeDamage = 16,
    OnAnyKill = 32,
    StartDefuse = 64
}