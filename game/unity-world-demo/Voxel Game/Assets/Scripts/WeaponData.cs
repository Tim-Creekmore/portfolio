using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Voxel Kingdom/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public enum AttackDirection { Overhead, Left, Right, Thrust }

    [Header("Identity")]
    public string weaponName = "Unnamed Weapon";

    [Header("Damage")]
    public float baseDamage = 20f;

    [Header("Timing (seconds)")]
    public float windUpTime = 0.25f;
    public float swingTime = 0.15f;
    public float recoveryTime = 0.35f;

    [Header("Range")]
    public float range = 1.8f;
    public float hitRadius = 0.3f;

    [Header("Stamina")]
    public float staminaCostAttack = 15f;
    public float staminaCostBlock = 10f;

    [Header("Directions")]
    public AttackDirection[] supportedDirections = {
        AttackDirection.Overhead,
        AttackDirection.Left,
        AttackDirection.Right,
        AttackDirection.Thrust
    };

    [Header("Damage Multipliers per Direction")]
    public float overheadMultiplier = 1.2f;
    public float sideMultiplier = 1.0f;
    public float thrustMultiplier = 0.9f;

    public float GetDirectionMultiplier(AttackDirection dir)
    {
        switch (dir)
        {
            case AttackDirection.Overhead: return overheadMultiplier;
            case AttackDirection.Left:
            case AttackDirection.Right:   return sideMultiplier;
            case AttackDirection.Thrust:  return thrustMultiplier;
            default: return 1f;
        }
    }
}
