using FluentAssertions;
using Location.Core.BDD.Tests.Support;
using TechTalk.SpecFlow;
using System.Threading.Tasks;
using System;

namespace Location.Core.BDD.Tests.StepDefinitions
{
    [Binding]
    public class CommonSteps
    {
        private readonly ApiContext _context;

        public CommonSteps(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [Given(@"the application is initialized for testing")]
        public void GivenTheApplicationIsInitializedForTesting()
        {
            // The ApiContext is already initialized by the BeforeScenario hook
            // No need to do anything here
        }

        [Then(@"I should receive a successful result")]
        public void ThenIShouldReceiveASuccessfulResult()
        {
            var lastResult = _context.GetLastResult<object>();
            lastResult.Should().NotBeNull("Result should be available");
            lastResult.IsSuccess.Should().BeTrue("Operation should be successful");
        }

        [Then(@"I should receive a failure result")]
        public void ThenIShouldReceiveAFailureResult()
        {
            var lastResult = _context.GetLastResult<object>();
            lastResult.Should().NotBeNull("Result should be available");
            lastResult.IsSuccess.Should().BeFalse("Operation should have failed");
        }

        [Then(@"the error message should contain ""(.*)""")]
        public void ThenTheErrorMessageShouldContain(string errorText)
        {
            var lastResult = _context.GetLastResult<object>();
            lastResult.Should().NotBeNull("Result should be available");
            lastResult.ErrorMessage.Should().Contain(errorText, $"Error message should contain '{errorText}'");
        }
    }
}