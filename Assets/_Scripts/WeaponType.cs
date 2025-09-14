// _Scripts/WeaponType.cs

public enum WeaponType
{
    AR, MR, SR, SG, SMG, LMG, Pistol
}

public static class WeaponData
{
    public static bool IsAutomatic(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.AR:
            case WeaponType.SMG:
            case WeaponType.LMG:
                return true;
            default:
                return false;
        }
    }
}