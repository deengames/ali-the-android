using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Model.Entities;

namespace DeenGames.AliTheAndroid.Helpers
{
    public static class GravityWaveFinder
    {
        public static IEnumerable<GravityWave> FloodFillFind(GravityWave start, IList<GravityWave> gravityWaves)
        {
            var toProcess = new List<GravityWave>();
            toProcess.Add(start);
            
            var discovered = new List<GravityWave>();

            while (toProcess.Any())
            {
                var next = toProcess[0];
                toProcess.RemoveAt(0);
                
                discovered.Add(next);
                var toDiscover = gravityWaves.Where(g => 
                    !discovered.Contains(g) && !toProcess.Contains(g) &&
                    GoRogue.Distance.EUCLIDEAN.Calculate(g.X, g.Y, next.X, next.Y) <= 1);

                toProcess.AddRange(toDiscover);
            }

            return discovered;
        }
    }
}