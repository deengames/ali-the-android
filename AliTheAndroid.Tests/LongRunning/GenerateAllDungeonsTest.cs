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

        [Test]
        public void ParseGenerateDungeonsLogAndLookForSlowDungeons()
        {
            DateTime previousDate = DateTime.MinValue;
            var lines = File.ReadAllLines(LogFilePath);
            var seedsThatTookTooLongToGenerate = new List<DungeonGenerationTime>();
            var previousSeed = "";

            foreach (var line in lines)
            {
                var dateTimeString = line.Substring(0, line.IndexOf('|'));
                var currentDate = DateTime.Parse(dateTimeString);
                var generationTime = (currentDate - previousDate).TotalSeconds;

                if (line.Contains("Generating"))
                {
                    var start = line.IndexOf('#') + 1;
                    var stop = line.IndexOf(' ', start);
                    var seed = line.Substring(start, stop - start);
                    
                    if (previousDate != DateTime.MinValue && generationTime > MaxExpectedGenerationTimeSecondsForDungeon)
                    {
                        // Log previous seed. Why? Because logs look like this:
                        // 6/14/2019 7:25:58 AM | Generating dungeon #587374942 ... done
                        // 6/14/2019 7:32:29 AM | Generating dungeon #855970612 ... done
                        // The first seed (previousSeed) started generating at 7:25am, the second at 7:32am. First is implicated.
                        seedsThatTookTooLongToGenerate.Add(new DungeonGenerationTime() { Seed = int.Parse(previousSeed), GenerationTimeSeconds = generationTime });
                    }

                    previousSeed = seed;
                }
                previousDate = currentDate;
            }

            Assert.That(!seedsThatTookTooLongToGenerate.Any(), 
                String.Join('\n', seedsThatTookTooLongToGenerate.OrderByDescending(d => d.GenerationTimeSeconds)) +
                $"\n{seedsThatTookTooLongToGenerate.Count} dungeons took too long.");
        }

        // These seeds used to take *forever* to generate. Now they shouldn't.
        [TestCase(1713409808)]
        public void GenerateDungeonGeneratesInExpectedTimeframe(int seed)
        {
            ParameterizedThreadStart threadStart = (obj) => {
                new Dungeon(RealGameWidth, RealGameHeight, seed);
            };

            var thread = new Thread(threadStart);
            var startTime = DateTime.Now;
            thread.Start();

            while (thread.ThreadState == ThreadState.Running && (DateTime.Now - startTime).TotalSeconds <= 10 * MaxExpectedGenerationTimeSecondsForDungeon)
            {
                // ZZzzz...
            }

            Assert.That(thread.ThreadState, Is.EqualTo(ThreadState.Stopped), $"Dungeon {seed} did not complete generation in {MaxExpectedGenerationTimeSecondsForDungeon} seconds!");
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

        private class DungeonGenerationTime
        {
            public int Seed { get; set; }
            public double GenerationTimeSeconds { get; set; }

            public override string ToString()
            {
                return $"Seed {Seed}: {GenerationTimeSeconds} seconds";
            }
        }
    }
}