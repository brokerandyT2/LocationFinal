using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.BDD.Tests.Features
{
    /// <summary>
    /// Base class for all SpecFlow feature test classes
    /// Provides common setup and teardown functionality
    /// </summary>
    public abstract class FeatureBase
    {
        private BoDi.IObjectContainer _objectContainer;

        public FeatureBase(BoDi.IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [TestCleanup]
        public virtual void TestTearDownAsync()
        {
            try
            {
                // Get the ApiContext if it exists
                if (_objectContainer.IsRegistered<BDD.Tests.Support.ApiContext>())
                {
                    var context = _objectContainer.Resolve<BDD.Tests.Support.ApiContext>();
                    context?.ClearContext();
                }
            }
            catch (Exception ex)
            {
                // Log exception but don't rethrow to avoid masking test failures
                System.Console.WriteLine($"Error in TestTearDown: {ex.Message}");
            }
        }
    }
}
