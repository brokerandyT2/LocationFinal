using Location.Core.Application.Common.Models;
using Location.Core.BDD.Tests.Models;

namespace Location.Core.BDD.Tests.Support
{
    /// <summary>
    /// Provides a context for storing and accessing test data
    /// during scenario execution
    /// </summary>
    public class ApiContext
    {
        private readonly TestServiceProvider _serviceProvider;
        private readonly Dictionary<string, object> _scenarioContext = new Dictionary<string, object>();
        private bool _isDisposed = false;

        /// <summary>
        /// Initializes a new API context
        /// </summary>
        public ApiContext()
        {
            try
            {
                Console.WriteLine("Creating new ApiContext with new TestServiceProvider");
                _serviceProvider = new TestServiceProvider();
                InitializeDefaults();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing ApiContext: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Initializes a new API context with the provided service provider
        /// </summary>
        public ApiContext(TestServiceProvider serviceProvider)
        {
            try
            {
                Console.WriteLine("Creating new ApiContext with existing TestServiceProvider");
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                InitializeDefaults();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing ApiContext with existing provider: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private void InitializeDefaults()
        {
            try
            {
                Console.WriteLine("Initializing default models in ApiContext");

                // Initialize default objects to avoid null references
                StoreModel(new LocationTestModel(), "Location");
                StoreModel(new WeatherTestModel(), "Weather");
                StoreModel(new TipTestModel(), "Tip");
                StoreModel(new SettingTestModel(), "Setting");
                StoreModel(new List<LocationTestModel>(), "AllLocations");

                Console.WriteLine("Default models initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing defaults: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets a service of the specified type
        /// </summary>
        public T GetService<T>() where T : class
        {
            if (_isDisposed)
            {
                Console.WriteLine($"WARNING: Attempting to get service {typeof(T).Name} from a disposed ApiContext");
                return null;
            }

            try
            {
                var service = _serviceProvider.GetService<T>();
                if (service == null)
                {
                    Console.WriteLine($"WARNING: Failed to resolve service of type {typeof(T).Name}");
                }
                return service;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting service of type {typeof(T).Name}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Stores a result in the scenario context
        /// </summary>
        public void StoreResult<T>(Result<T> result, string key = "LastResult")
        {
            if (_isDisposed)
            {
                Console.WriteLine($"WARNING: Attempting to store result in a disposed ApiContext. Key: {key}");
                return;
            }

            try
            {
                if (result != null)
                {
                    _scenarioContext[$"Result_{key}"] = result;
                    Console.WriteLine($"Stored result with key: Result_{key}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Attempted to store null result with key: Result_{key}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing result with key {key}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets the last stored result of the specified type
        /// </summary>
        public Result<T> GetLastResult<T>(string key = "LastResult")
        {
            if (_isDisposed)
            {
                Console.WriteLine($"WARNING: Attempting to get result from a disposed ApiContext. Key: {key}");
                return null;
            }

            try
            {
                var fullKey = $"Result_{key}";
                if (_scenarioContext.TryGetValue(fullKey, out var result))
                {
                    if (result is Result<T> typedResult)
                    {
                        return typedResult;
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: Result with key {fullKey} exists but is of type {result?.GetType().Name} instead of Result<{typeof(T).Name}>");
                    }
                }
                else
                {
                    Console.WriteLine($"WARNING: No result found with key {fullKey}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting result with key {key}: {ex.Message}\n{ex.StackTrace}");
            }

            return null;
        }

        /// <summary>
        /// Stores a test model in the scenario context
        /// </summary>
        public void StoreModel<T>(T model, string key) where T : class
        {
            if (_isDisposed)
            {
                Console.WriteLine($"WARNING: Attempting to store model in a disposed ApiContext. Key: {key}");
                return;
            }

            try
            {
                if (model != null && !string.IsNullOrEmpty(key))
                {
                    var fullKey = $"Model_{key}";
                    _scenarioContext[fullKey] = model;
                    Console.WriteLine($"Stored model with key: {fullKey}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Attempted to store null model or with empty key: {key}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing model with key {key}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets a stored test model from the scenario context
        /// </summary>
        public T GetModel<T>(string key) where T : class
        {
            if (_isDisposed)
            {
                Console.WriteLine($"WARNING: Attempting to get model from a disposed ApiContext. Key: {key}");
                return null;
            }

            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    Console.WriteLine("WARNING: Attempted to get model with empty key");
                    return null;
                }

                var fullKey = $"Model_{key}";
                if (_scenarioContext.TryGetValue(fullKey, out var model))
                {
                    if (model is T typedModel)
                    {
                        return typedModel;
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: Model with key {fullKey} exists but is of type {model?.GetType().Name} instead of {typeof(T).Name}");
                    }
                }
                else
                {
                    Console.WriteLine($"WARNING: No model found with key {fullKey}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting model with key {key}: {ex.Message}\n{ex.StackTrace}");
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
        public LocationTestModel GetLocationData()
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
        public WeatherTestModel GetWeatherData()
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
        public TipTestModel GetTipData()
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
        public SettingTestModel GetSettingData()
        {
            return GetModel<SettingTestModel>("Setting");
        }

        /// <summary>
        /// Clears all stored data in the context
        /// </summary>
        public void ClearContext()
        {
            if (_isDisposed)
            {
                Console.WriteLine("WARNING: Attempting to clear an already disposed ApiContext");
                return;
            }

            try
            {
                Console.WriteLine("Clearing ApiContext data");
                _scenarioContext.Clear();
                _isDisposed = true;
                Console.WriteLine("ApiContext data cleared successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing context: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
