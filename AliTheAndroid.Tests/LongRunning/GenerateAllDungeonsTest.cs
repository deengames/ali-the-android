using System;
using System.Diagnostics;
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
        const int DungeonsToGenerate = 86400;

        [OneTimeSetUp]
        public void SetupKeyboard()
        {
            DependencyInjection.kernel = new StandardKernel();
            DependencyInjection.kernel.Bind<IKeyboard>().To<DeadKeyboard>();
        }
        
        [Ignore("This test takes hours! Run overnight every now and then.")]
        [Test]
        public void GenerateSomeDungeons()
        {
            var random = new Random();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            for (var i = 0; i < DungeonsToGenerate; i++)
            {
                var seed = random.Next();
                Console.WriteLine($"{StopWatchTime(stopWatch)} | Generating dungeon {i + 1} with seed {seed}");
                var dungeon = new Dungeon(50, 40, seed);
            }
            stopWatch.Stop();
            Console.WriteLine($"Generated {DungeonsToGenerate} dungeons in {StopWatchTime(stopWatch)}");
        }

        private string StopWatchTime(Stopwatch input)
        {
            var total = input.ElapsedMilliseconds;
            var seconds = total / 1000;
            var minutes = seconds / 60;
            var hours = minutes / 60;

            return $"{hours}h {minutes}m {seconds}s";
        }
    }
}