using Location.Core.Application.Common.Models;
using Location.Core.BDD.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Location.Core.BDD.Tests.Support
{
    /// <summary>
    /// Provides a context for storing and accessing test data
    /// during scenario execution
    /// </summary>
    public class ApiContext
    {
        private readonly TestServiceProvider _serviceProvider;
        private readonly Dictionary<string, object> _scenarioContext = new();

        /// <summary>
        /// Initializes a new API context with the provided service provider
        /// </summary>
        /// <param name="useMocks">Whether to use mocked services or real implementations</param>
        public ApiContext()
        {
            _serviceProvider = new TestServiceProvider();
            InitializeDefaults();
        }

        // Alternative constructor that can be called programmatically
        internal ApiContext(TestServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // Initialize default objects to avoid null references
            StoreModel(new LocationTestModel(), "Location");
            StoreModel(new WeatherTestModel(), "Weather");
            StoreModel(new TipTestModel(), "Tip");
            StoreModel(new SettingTestModel(), "Setting");
        }

        /// <summary>
        /// Gets a service of the specified type
        /// </summary>
        public T GetService<T>() where T : class
        {
            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// Stores a result in the scenario context
        /// </summary>
        public void StoreResult<T>(Result<T> result, string key = "LastResult")
        {
            _scenarioContext[$"Result_{key}"] = result;
        }

        /// <summary>
        /// Gets the last stored result of the specified type
        /// </summary>
        public Result<T>? GetLastResult<T>(string key = "LastResult")
        {
            if (_scenarioContext.TryGetValue($"Result_{key}", out var result) && result is Result<T> typedResult)
            {
                return typedResult;
            }

            return null;
        }

        /// <summary>
        /// Stores a test model in the scenario context
        /// </summary>
        public void StoreModel<T>(T model, string key) where T : class
        {
            _scenarioContext[$"Model_{key}"] = model;
        }

        /// <summary>
        /// Gets a stored test model from the scenario context
        /// </summary>
        public T? GetModel<T>(string key) where T : class
        {
            if (_scenarioContext.TryGetValue($"Model_{key}", out var model) && model is T typedModel)
            {
                return typedModel;
            }

            return null;
        }

        /// <summary>
        /// Stores location data in the context for use in steps
        /// </summary>
        public void StoreLocationData(LocationTestModel model)
        {
            StoreModel(model, "Location");
        }

        /// <summary>
        /// Gets the stored location data
        /// </summary>
        public LocationTestModel? GetLocationData()
        {
            return GetModel<LocationTestModel>("Location");
        }

        /// <summary>
        /// Stores weather data in the context for use in steps
        /// </summary>
        public void StoreWeatherData(WeatherTestModel model)
        {
            StoreModel(model, "Weather");
        }

        /// <summary>
        /// Gets the stored weather data
        /// </summary>
        public WeatherTestModel? GetWeatherData()
        {
            return GetModel<WeatherTestModel>("Weather");
        }

        /// <summary>
        /// Stores tip data in the context for use in steps
        /// </summary>
        public void StoreTipData(TipTestModel model)
        {
            StoreModel(model, "Tip");
        }

        /// <summary>
        /// Gets the stored tip data
        /// </summary>
        public TipTestModel? GetTipData()
        {
            return GetModel<TipTestModel>("Tip");
        }

        /// <summary>
        /// Stores setting data in the context for use in steps
        /// </summary>
        public void StoreSettingData(SettingTestModel model)
        {
            StoreModel(model, "Setting");
        }

        /// <summary>
        /// Gets the stored setting data
        /// </summary>
        public SettingTestModel? GetSettingData()
        {
            return GetModel<SettingTestModel>("Setting");
        }

        /// <summary>
        /// Clears all stored data in the context
        /// </summary>
        public void ClearContext()
        {
            _scenarioContext.Clear();
        }
    }
}