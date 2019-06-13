using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Model.Events;
using DeenGames.AliTheAndroid.Tests.Helpers;
using Ninject;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.LongRunning
{
    [TestFixture]
    public class GenerateAllDungeonsTests
    {
        // One dungeon takes roughly 0.5s to generate. In 12 hours, we can generate ~86400 dungeons.
        const float HoursToRun = 0.005f;
        const int NumThreads = 8;
        
        private static DateTime startTime;
        // Shared across threads
        private static int numGenerated = 0;

        [OneTimeSetUp]
        public void SetupKeyboard()
        {
            DependencyInjection.kernel = new StandardKernel();
            DependencyInjection.kernel.Bind<IKeyboard>().To<DeadKeyboard>();
            EventBus.UseConcurrentModel = true;
        }
        
        //[Ignore("This test takes hours! Run overnight every now and then.")]
        [Test]
        public void GenerateSomeDungeons()
        {
            startTime = DateTime.Now;
            var random = new Random();
            
            // It's possible threads will overlap in seeds. Unlikely, but possible. /shrug
            var threads = new List<Thread>();

            for (int i = 0; i < NumThreads; i++)
            {
                var thread = new Thread(GenerateDungeons);
                thread.Start(random.Next());
                threads.Add(thread);
            }

            Console.WriteLine("Thread started");

            foreach (var thread in threads)
            {
                thread.Join();
                Console.WriteLine("Thread joined");
            }

            Console.WriteLine($"Generated ~{numGenerated} dungeons in {HoursToRun} hours");
        }

        public static void GenerateDungeons(object data)
        {
            var seed = (int)data;
            var random = new Random();

            while ((DateTime.Now - startTime).TotalHours <= HoursToRun)
            {                
                var dungeon = new Dungeon(80, 32, seed); // production size
                seed++;
                Interlocked.Increment(ref numGenerated);
            }
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