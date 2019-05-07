using System;
using DeenGames.AliTheAndroid.Prototype;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Helpers
{
    public class AbstractTest
    {
        [SetUp]
        [TearDown]
        public void ResetEventBus()
        {
            // Call EventBus constructor, which is private; resets EventBus.Instance
            Activator.CreateInstance(typeof(EventBus), true);
        }
    }
}