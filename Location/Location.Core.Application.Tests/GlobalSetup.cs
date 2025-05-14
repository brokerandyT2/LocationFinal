using NUnit.Framework;

namespace Location.Core.Application.Tests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            // Global initialization code
            // Configure AutoMapper profiles if needed
            // Initialize any test-wide services
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            // Global cleanup code
        }
    }
}