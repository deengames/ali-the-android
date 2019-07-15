using System;
using System.Threading;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model.Events;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Helpers
{
    public class AbstractTest
    {
        [SetUp]
        [TearDown]
        public void ResetEventBus()
        {
            // Set some stuff to values our tests depend on.
            Options.DisplayOldStyleAsciiCharacters = true;

            // Call EventBus constructor, which is private; resets EventBus.Instance
            Activator.CreateInstance(typeof(EventBus), true);
        }

        public void RestrictRuntime(Action testCode, int maxWaitSeconds = 3)
        {
            ThreadStart runTest = () => testCode();
            var thread = new Thread(runTest);

            var startTime = DateTime.Now;
            
            thread.Start();

            while (thread.ThreadState == ThreadState.Running && (
                (DateTime.Now - startTime).TotalSeconds <= maxWaitSeconds) ||
                System.Diagnostics.Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }

            if (thread.ThreadState == ThreadState.Running)
            {
                Assert.Fail($"Test exceeded maximum timeout ({maxWaitSeconds}s); thread state is {thread.ThreadState}; runtime was {(DateTime.Now - startTime).TotalSeconds}");
            }

            // Thread.Abort is not supported on this platform. Leave the thread around, I guess...
            // The test runner terminates, so the thread dies soon enough.
        }
    }
}