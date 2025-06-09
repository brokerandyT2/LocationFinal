using BoDi;

namespace Location.Core.BDD.Tests.Features
{
    /// <summary>
    /// Provides helper methods for feature test classes
    /// </summary>
    public static class FeatureTestHelper
    {
        /// <summary>
        /// Safely performs test cleanup without throwing exceptions
        /// </summary>
        /// <param name="objectContainer">The object container used in the feature</param>
        public static void TestTearDown(IObjectContainer objectContainer)
        {
            try
            {
                // Only try to clean up if ApiContext exists
                if (objectContainer != null && objectContainer.IsRegistered<Support.ApiContext>())
                {
                    var context = objectContainer.Resolve<Support.ApiContext>();
                    context?.ClearContext();
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw to avoid masking test failures
                Console.WriteLine($"Error in TestTearDown: {ex.Message}");
            }
        }
    }
}