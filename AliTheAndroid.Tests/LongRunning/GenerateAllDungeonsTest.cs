using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        // One dungeon takes roughly 1s to generate. Generates ~4200 dungeons per hour.
        const float HoursToRun = 0.005f;
        const string LogFilePath = "GenerateAllDungeonsTest.txt";
        const int RealGameWidth = 80;
        const int RealGameHeight = 32;

        [OneTimeSetUp]
        public void SetupKeyboard()
        {
            DependencyInjection.kernel = new StandardKernel();
            DependencyInjection.kernel.Bind<IKeyboard>().To<DeadKeyboard>();
        }
        
        [Test]
        public void GenerateSomeDungeons()
        {
            DateTime startTime = DateTime.Now;
            File.Delete(LogFilePath);
            Log($"Starting on {startTime} - generating for {HoursToRun} hours.");

            var random = new Random();
            var numGenerated = 0;
            
            while ((DateTime.Now - startTime).TotalHours <= HoursToRun)
            {
                var seed = random.Next();
                Log($"{DateTime.Now} | Generating dungeon #{seed} ... ", false);
                var dungeon = new Dungeon(RealGameWidth, RealGameHeight, seed); // production size
                Log($"done\n", false);
                numGenerated++;
            }

            Console.WriteLine($"Generated ~{numGenerated} dungeons in {HoursToRun} hours. Log file is {LogFilePath}\n");
            Log($"Generated ~{numGenerated} dungeons in {HoursToRun} hours.");
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

        private void Log(string message, bool writeNewLine = true)
        {
            var finalMessage = "";
            if (writeNewLine)
            {
                finalMessage += $"{DateTime.Now} | ";
            }
            finalMessage += message;
            if (writeNewLine) {
                finalMessage += '\n';
            }

            File.AppendAllText(LogFilePath, finalMessage);
        }
    }
}