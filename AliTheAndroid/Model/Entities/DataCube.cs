using System.Collections.Generic;
using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public  class DataCube : AbstractEntity
    {
        public static readonly Color[] DisplayColors = new Color[] { Palette.White, Palette.Cyan, Palette.Blue };

        // Buzz-words: Experiment Chamber, AllCure, prototype #37, Ameer
        private static List<string> cubeTexts = new List<string>()
        {
            // B2
            @"Marbellu Corporation hired me to work on some sort of cure. It's exciting to start working here, because the Qur'an teaches us that saving one life is like save humanity!",
            @"There was an explosion in the Experiment Chamber. AllCure prototype #37 leaked and affected a couple of the crew. The Ameer put them in quarantine. Why? Was it an accident? Or ... sabotage?",
            @"We're not building a cure. We're building a weapon. We told the Ameer, but he didn't even blink.  He knew.  How can he support this? Doesn't he know that he will be held accountable on the Day of Reckoning?",
            @"After hours, I gained access to the Experiment Chamber through the vents.  I found and analyzed a blood sample with AllCure.  It's an air-borne virus. It's in the vents. It's everywhere.",
            @"I cut myself on a metal fragment coming back from the Experiment Chamber. Felt strange. I'm infected. I can feel my senses heightening, muscles growing. What am I becoming?",
            @"The scientists finished AllCure - the Ameer turned them into Zugs with the prototype. They smashed our Warp Drive ... quantum plasma spread everywhere.",
            @"I am the last. The last scientist, the last Zug. The Ameer holed himself up on the last floor, he may still be there. I hope my Plasma Cannon works ...",
            // B9
            @"If you're reading this, I'm dead. The Ameer finished the AllCure and drank it, then killed everyone. His wounds heal instantly. I don't know how to stop him."
        };

        private const int FirstDataCubeFloor = 1; // 1 = B2

        private const char DisplayCharacter = (char)240; // ≡

        public int FloorNumber { get; private set; } // 5 => B5
        public string Text { get; private set; }
        public bool IsRead { get; set; } = false;

        public DataCube(int x, int y, int floorNumber, string text) : base(x, y, DisplayCharacter, Palette.White)
        {
            this.FloorNumber = floorNumber;
            this.Text = text;
        }

        public List<DataCube> GenerateCubes()
        {
            var toReturn = new List<DataCube>();

            for (var i = 0; i < cubeTexts.Count; i++)
            {
                var floorNumber = i + FirstDataCubeFloor;
                var cube = new DataCube(0, 0, floorNumber, cubeTexts[i]);
            }

            return toReturn;
        }
    }
}