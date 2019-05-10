using System;
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
            // Call EventBus constructor, which is private; resets EventBus.Instance
            Activator.CreateInstance(typeof(EventBus), true);
        }
    }
}