using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Enums
{
    // Lux2K palette: https://lospec.com/palette-list/lux2k
    public static class Palette
    {
       public static Color BlackAlmost= FromHex("#25131a");
        public static Color DarkPurple= FromHex("#3d253b");
        public static Color DarkMutedBrown= FromHex("#523b40");
        public static Color DarkestGreen= FromHex("#1f3736");
        public static Color DarkGreen= FromHex("#2a5a39");

        public static Color Green= FromHex("#427f3b");
        public static Color LightMutedGreen= FromHex("#80a53f");
        public static Color LightGreen= FromHex("#bbc44e");
        public static Color DarkLimeGreen= FromHex("#96c641");
        public static Color LimeGreen= FromHex("#ccf61f");

        public static Color MutedDarkLimeGreen = FromHex("#8a961f");
        public static Color DarkGreenMuted= FromHex("#5c6b53");
        public static Color Brown= FromHex("#895a45");
        public static Color Orange= FromHex("#d1851e");
        public static Color YellowAlmost= FromHex("#ffd569");

        public static Color LighterBrown= FromHex("#bf704d");
        public static Color LightestBrown= FromHex("#e1a171");
        public static Color OffWhite= FromHex("#e6deca");
        public static Color Burgandy= FromHex("#9b4c51");
        public static Color DarkBurgandyPurple= FromHex("#802954");

        public static Color Red= FromHex("#d01946");
        public static Color LightRed= FromHex("#e84444");
        public static Color DarkestBlue= FromHex("#40369f");
        public static Color Purple= FromHex("#7144ff");
        public static Color LilacPinkPurple = FromHex("#af69bf");

        public static Color LightLilacPink = FromHex("#eaa5ff");
        public static Color Blue= FromHex("#5880cc");
        public static Color Aqua= FromHex("#62abd4");
        public static Color Cyan= FromHex("#9bf0fd");
        public static Color CyanMuted= FromHex("#cae6f5");

        public static Color White= FromHex("#ffffff");
        public static Color LightGrey= FromHex("#a7acba");
        public static Color Grey= FromHex("#606060");
        public static Color DarkBlueMuted= FromHex("#56587b");
        public static Color DarkSkinBrown = FromHex("#9a8571");
        public static Color SkinPeach= FromHex("#dfbbb3");
        private static Color FromHex(string hexValue)
        {
            if (hexValue.StartsWith("#"))
            {
                hexValue = hexValue.Substring(1);
            }

            var red = HexToDec(hexValue.Substring(0, 2));
            var green = HexToDec(hexValue.Substring(2, 2));
            var blue = HexToDec(hexValue.Substring(4, 2));

            return Color.FromNonPremultiplied(red, green, blue, 255);
        }

        private static int HexToDec(string hexValue)
        {
            // https://stackoverflow.com/questions/1139957/c-sharp-convert-integer-to-hex-and-back-again
            return int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
        }
    }
}