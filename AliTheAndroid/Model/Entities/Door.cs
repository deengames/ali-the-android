using DeenGames.AliTheAndroid.Enums;
using Newtonsoft.Json;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    [System.Diagnostics.DebuggerDisplay("Door at ({X}, {Y}) L={IsLocked}")]
    public class Door : AbstractEntity
    {
        public bool IsBacktrackingDoor {get; private set;}

        [JsonProperty]
        internal bool isLocked = false;

        [JsonProperty]
        internal bool isOpened = false;

        public bool IsOpened { get { return this.isOpened; }
        set {
            this.isOpened = value;
            this.Character = this.IsOpened ? '-' : '+';
        }}

        
        public bool IsLocked { get { return this.isLocked; }
        set {
            this.isLocked = value;
            this.Color = this.isLocked ? Palette.Orange : Palette.YellowAlmost;
        }}

        public Door(int x, int y, bool isLocked = false, bool isBacktrackingDoor = false) : base(x, y, '+', Palette.YellowAlmost)
        {
            this.IsLocked = isLocked;
            this.Color = isLocked ? Palette.Orange : Palette.YellowAlmost;
            this.IsBacktrackingDoor = isBacktrackingDoor;
        }
    }
}