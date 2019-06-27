namespace DeenGames.AliTheAndroid.Infrastructure.Common
{
    public interface IKeyboard
    {
        bool IsKeyPressed(Key key);
        void Clear();
    }
}