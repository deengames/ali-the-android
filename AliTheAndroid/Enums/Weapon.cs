namespace DeenGames.AliTheAndroid.Enums
{
    public enum Weapon
    {
        Blaster,
        MiniMissile,
        Zapper,
        PlasmaCannon,
        GravityCannon,
        InstaTeleporter, // Not really a weapon
        QuantumPlasma, // Used for damage/end-game detection
        Undefined, // Used for .damage where source doesn't make sense
    }
}