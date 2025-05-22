using Location.Core.Application.Common.Models;
using Location.Photography.BDD.Tests.Models;
using System;
using System.Collections.Generic;

namespace Location.Photography.BDD.Tests.Support
{
    /// <summary>
    /// Provides a context for storing and accessing test data
    /// during Photography scenario execution
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
                Console.WriteLine("Creating new Photography ApiContext with new TestServiceProvider");
                _serviceProvider = new TestServiceProvider();
                InitializeDefaults();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Photography ApiContext: {ex.Message}\n{ex.StackTrace}");
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
                Console.WriteLine("Creating new Photography ApiContext with existing TestServiceProvider");
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                InitializeDefaults();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Photography ApiContext with existing provider: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private void InitializeDefaults()
        {
            try
            {
                Console.WriteLine("Initializing default Photography models in ApiContext");

                // Initialize default objects to avoid null references
                StoreModel(new ExposureTestModel(), "Exposure");
                StoreModel(new SunCalculationTestModel(), "SunCalculation");
                StoreModel(new SceneEvaluationTestModel(), "SceneEvaluation");
                StoreModel(new List<ExposureTestModel>(), "AllExposures");
                StoreModel(new List<SunCalculationTestModel>(), "AllSunCalculations");
                StoreModel(new List<SceneEvaluationTestModel>(), "AllSceneEvaluations");

                Console.WriteLine("Default Photography models initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Photography defaults: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets a service of the specified type
        /// </summary>
        public T GetService<T>() where T : class
        {
            if (_isDisposed)
            {
                Console.WriteLine($"WARNING: Attempting to get service {typeof(T).Name} from a disposed Photography ApiContext");
                return null;
            }

            try
            {
                var service = _serviceProvider.GetService<T>();
                if (service == null)
                {
                    Console.WriteLine($"WARNING: Failed to resolve Photography service of type {typeof(T).Name}");
                }
                return service;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Photography service of type {typeof(T).Name}: {ex.Message}\n{ex.StackTrace}");
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
                Console.WriteLine($"WARNING: Attempting to store result in a disposed Photography ApiContext. Key: {key}");
                return;
            }

            try
            {
                if (result != null)
                {
                    _scenarioContext[$"Result_{key}"] = result;
                    Console.WriteLine($"Stored Photography result with key: Result_{key}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Attempted to store null Photography result with key: Result_{key}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing Photography result with key {key}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets the last stored result of the specified type
        /// </summary>
        public Result<T> GetLastResult<T>(string key = "LastResult")
        {
            if (_isDisposed)
            {
                Console.WriteLine($"WARNING: Attempting to get result from a disposed Photography ApiContext. Key: {key}");
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
                        Console.WriteLine($"WARNING: Photography result with key {fullKey} exists but is of type {result?.GetType().Name} instead of Result<{typeof(T).Name}>");
                    }
                }
                else
                {
                    Console.WriteLine($"WARNING: No Photography result found with key {fullKey}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Photography result with key {key}: {ex.Message}\n{ex.StackTrace}");
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
                Console.WriteLine($"WARNING: Attempting to store model in a disposed Photography ApiContext. Key: {key}");
                return;
            }

            try
            {
                if (model != null && !string.IsNullOrEmpty(key))
                {
                    var fullKey = $"Model_{key}";
                    _scenarioContext[fullKey] = model;
                    Console.WriteLine($"Stored Photography model with key: {fullKey}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Attempted to store null Photography model or with empty key: {key}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing Photography model with key {key}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets a stored test model from the scenario context
        /// </summary>
        public T GetModel<T>(string key) where T : class
        {
            if (_isDisposed)
            {
                Console.WriteLine($"WARNING: Attempting to get model from a disposed Photography ApiContext. Key: {key}");
                return null;
            }

            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    Console.WriteLine("WARNING: Attempted to get Photography model with empty key");
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
                        Console.WriteLine($"WARNING: Photography model with key {fullKey} exists but is of type {model?.GetType().Name} instead of {typeof(T).Name}");
                    }
                }
                else
                {
                    Console.WriteLine($"WARNING: No Photography model found with key {fullKey}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Photography model with key {key}: {ex.Message}\n{ex.StackTrace}");
            }

            return null;
        }

        /// <summary>
        /// Stores exposure data in the context for use in steps
        /// </summary>
        public void StoreExposureData(ExposureTestModel model)
        {
            StoreModel(model, "Exposure");
        }

        /// <summary>
        /// Gets the stored exposure data
        /// </summary>
        public ExposureTestModel GetExposureData()
        {
            return GetModel<ExposureTestModel>("Exposure");
        }

        /// <summary>
        /// Stores sun calculation data in the context for use in steps
        /// </summary>
        public void StoreSunCalculationData(SunCalculationTestModel model)
        {
            StoreModel(model, "SunCalculation");
        }

        /// <summary>
        /// Gets the stored sun calculation data
        /// </summary>
        public SunCalculationTestModel GetSunCalculationData()
        {
            return GetModel<SunCalculationTestModel>("SunCalculation");
        }

        /// <summary>
        /// Stores scene evaluation data in the context for use in steps
        /// </summary>
        public void StoreSceneEvaluationData(SceneEvaluationTestModel model)
        {
            StoreModel(model, "SceneEvaluation");
        }

        /// <summary>
        /// Gets the stored scene evaluation data
        /// </summary>
        public SceneEvaluationTestModel GetSceneEvaluationData()
        {
            return GetModel<SceneEvaluationTestModel>("SceneEvaluation");
        }

        /// <summary>
        /// Clears all stored data in the context
        /// </summary>
        public void ClearContext()
        {
            if (_isDisposed)
            {
                Console.WriteLine("WARNING: Attempting to clear an already disposed Photography ApiContext");
                return;
            }

            try
            {
                Console.WriteLine("Clearing Photography ApiContext data");
                _scenarioContext.Clear();
                _isDisposed = true;
                Console.WriteLine("Photography ApiContext data cleared successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing Photography context: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}