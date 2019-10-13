using System;
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
            var dungeon = new Dungeon(80, 28, seed);
        }

        [Test]
        [Ignore("This test should only ever be run by hand.")]
        public void FindDungeonsThatCrashOnGeneration()
        {
            System.IO.File.Delete("DUNGEON_FAILURES.txt");

            LogToFile("Starting ...");
            var start = DateTime.Now;
            var random = new Random();
            var numGenerated = 0;

            while (true)
            {
                var seed = random.Next();
                try {
                    new Dungeon(80, 28, seed);
                    numGenerated++;

                    if (numGenerated % 100 == 0)
                    {
                        LogToFile($"{numGenerated} generated");
                    }
                } catch (Exception e)
                {
                    var elapsed = (DateTime.Now - start).TotalMinutes;
                    LogToFile($"Failed on seed {seed}: {e}");
                    LogToFile(e.StackTrace);
                }
            }
        }

        private void LogToFile(string message)
        {
            System.IO.File.AppendAllText("DUNGEON_FAILURES.txt", $"{DateTime.Now} | {message}\n");
        }
    }
}