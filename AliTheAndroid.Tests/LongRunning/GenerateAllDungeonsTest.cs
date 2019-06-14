using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Tests.Helpers;
using Ninject;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.LongRunning
{
    [TestFixture]
    public class GenerateAllDungeonsTests
    {
        // One dungeon takes roughly 1s to generate.
        const float HoursToRun = 0.005f;
        const string LogFilePath = "GenerateAllDungeonsTest.txt";
        const int MaxExpectedGenerationTimeSecondsForDungeon = 2;

        const int RealGameWidth = 80;
        const int RealGameHeight = 30;

        [OneTimeSetUp]
        public void SetupKeyboard()
        {
            DependencyInjection.kernel = new StandardKernel();
            DependencyInjection.kernel.Bind<IKeyboard>().To<DeadKeyboard>();
        }

        // These seeds used to freeze, in Days Gone By. Now, they shouldn't.
        [TestCase(740970391)]
        [TestCase(1036496413)]
        [TestCase(1234)]
        [TestCase(924473797)]
        public void GenerateDungeonDoesntFreezeForKnownFreezingSeeds(int seed)
        {
            var dungeon = new Dungeon(RealGameWidth, RealGameHeight, seed);
        }
    }
}