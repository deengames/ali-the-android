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
        // One dungeon takes roughly 0.5s to generate. In 12 hours, we can generate ~86400 dungeons.
        const float HoursToRun = 0.001f;
        const string LogFilePath = "GenerateAllDungeonsTest.txt";

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
            Log($"Starting on {startTime}");

            var random = new Random();
            var numGenerated = 0;
            
            while ((DateTime.Now - startTime).TotalHours <= HoursToRun)
            {
                var seed = random.Next();
                Log($"Generating dungeon #{seed} ...", false);
                var dungeon = new Dungeon(80, 32, seed); // production size
                Log($"Done\n", false);
                numGenerated++;
            }

            Console.WriteLine($"Generated ~{numGenerated} dungeons in {HoursToRun} hours. Log file is {LogFilePath}\n");
            Log($"Generated ~{numGenerated} dungeons in {HoursToRun} hours.");
        }

        private void Log(string message, bool writeNewLine = true)
        {
            var finalMessage = $"{DateTime.Now} | ";
            if (!writeNewLine)
            {
                finalMessage = "";
            }
            finalMessage += message;
            if (writeNewLine) {
                finalMessage += '\n';
            }

            File.AppendAllText(LogFilePath, finalMessage);
        }
    }
}