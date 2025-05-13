using NUnit.Framework;

namespace Location.Core.Domain.Tests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            // Global initialization code can go here
            // For example, setting up test database, logging, etc.
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            // Global cleanup code can go here
        }
    }
}