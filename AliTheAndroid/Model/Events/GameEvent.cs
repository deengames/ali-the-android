namespace DeenGames.AliTheAndroid.Model.Events
{
    public enum GameEvent
    {
        EntityDeath, // something dies
        PlayerTookTurn, // the player moves/fights/etc.
        EggHatched,
        ShowSubMenu, // Player wants to see the in-game menu
        HideSubMenu, // Player wants to see the in-game menu
        ChangeSubMenu,
        ShowDataCube, // Show a specific data-cube that we just picked up
        AmeerStunned, // You just gave him a jolt of electric juice
    }
}