using BoDi;
using TechTalk.SpecFlow;
using Microsoft.Extensions.Logging;
using System;

namespace Location.Core.BDD.Tests.Hooks
{
    [Binding]
    public class TestInitialization
    {
        private readonly IObjectContainer _objectContainer;
        private static bool _isInitialized;

        public TestInitialization(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            // Initialize any global resources here
            _isInitialized = true;
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            // Clean up any global resources here
            _isInitialized = false;
        }

        [BeforeFeature]
        public static void BeforeFeature(FeatureContext featureContext)
        {
            // Initialize feature-specific resources
            var featureName = featureContext.FeatureInfo.Title;
            Console.WriteLine($"Starting feature: {featureName}");
        }

        [AfterFeature]
        public static void AfterFeature()
        {
            // Clean up feature-specific resources
            Console.WriteLine("Feature completed");
        }
    }
}