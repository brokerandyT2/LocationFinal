using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.BDD.Tests.Models;
using Location.Photography.BDD.Tests.Support;
using Moq;

namespace Location.Photography.BDD.Tests.Drivers
{
    /// <summary>
    /// Driver for exposure calculator operations in BDD tests
    /// </summary>
    public class ExposureCalculatorDriver
    {
        private readonly ApiContext _context;
        private readonly Mock<IExposureCalculatorService> _exposureCalculatorServiceMock;

        public ExposureCalculatorDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _exposureCalculatorServiceMock = _context.GetService<Mock<IExposureCalculatorService>>();
        }

        /// <summary>
        /// Sets up multiple exposure models in the mock service
        /// </summary>
        public void SetupExposures(List<ExposureTestModel> exposures)
        {
            if (exposures == null || !exposures.Any()) return;

            foreach (var exposure in exposures)
            {
                if (!exposure.Id.HasValue || exposure.Id.Value <= 0)
                {
                    exposure.Id = exposures.IndexOf(exposure) + 1;
                }
            }

            // Store for later retrieval
            _context.StoreModel(exposures, "SetupExposures");
        }

        /// <summary>
        /// Calculates shutter speed for given exposure settings
        /// </summary>
        public async Task<Result<ExposureSettingsDto>> CalculateShutterSpeedAsync(ExposureTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            var baseExposure = model.ToBaseExposureTriangle();

            // Create expected result
            var expectedResult = new ExposureSettingsDto
            {
                ShutterSpeed = model.ResultShutterSpeed ?? model.TargetShutterSpeed,
                Aperture = model.TargetAperture,
                Iso = model.TargetIso,
                ErrorMessage = model.ErrorMessage
            };

            // Setup mock to return expected result
            _exposureCalculatorServiceMock
                .Setup(s => s.CalculateShutterSpeedAsync(
                    It.Is<ExposureTriangleDto>(e =>
                        e.ShutterSpeed == baseExposure.ShutterSpeed &&
                        e.Aperture == baseExposure.Aperture &&
                        e.Iso == baseExposure.Iso),
                    It.Is<string>(a => a == model.TargetAperture),
                    It.Is<string>(i => i == model.TargetIso),
                    It.Is<ExposureIncrements>(inc => inc == model.Increments),
                    It.IsAny<CancellationToken>(),
                    It.Is<double>(ev => Math.Abs(ev - model.EvCompensation) < 0.01)))
                .ReturnsAsync(string.IsNullOrEmpty(model.ErrorMessage)
                    ? Result<ExposureSettingsDto>.Success(expectedResult)
                    : Result<ExposureSettingsDto>.Failure(model.ErrorMessage));

            // NO MediatR - Direct response
            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<ExposureSettingsDto>.Success(expectedResult)
                : Result<ExposureSettingsDto>.Failure(model.ErrorMessage);

            // Store result and update context
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                model.UpdateFromResult(result.Data);
                _context.StoreExposureData(model);
            }

            return result;
        }

        /// <summary>
        /// Calculates aperture for given exposure settings
        /// </summary>
        public async Task<Result<ExposureSettingsDto>> CalculateApertureAsync(ExposureTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            var baseExposure = model.ToBaseExposureTriangle();

            // Create expected result
            var expectedResult = new ExposureSettingsDto
            {
                ShutterSpeed = model.TargetShutterSpeed,
                Aperture = model.ResultAperture ?? model.TargetAperture,
                Iso = model.TargetIso,
                ErrorMessage = model.ErrorMessage
            };

            // Setup mock to return expected result
            _exposureCalculatorServiceMock
                .Setup(s => s.CalculateApertureAsync(
                    It.Is<ExposureTriangleDto>(e =>
                        e.ShutterSpeed == baseExposure.ShutterSpeed &&
                        e.Aperture == baseExposure.Aperture &&
                        e.Iso == baseExposure.Iso),
                    It.Is<string>(ss => ss == model.TargetShutterSpeed),
                    It.Is<string>(i => i == model.TargetIso),
                    It.Is<ExposureIncrements>(inc => inc == model.Increments),
                    It.IsAny<CancellationToken>(),
                    It.Is<double>(ev => Math.Abs(ev - model.EvCompensation) < 0.01)))
                .ReturnsAsync(string.IsNullOrEmpty(model.ErrorMessage)
                    ? Result<ExposureSettingsDto>.Success(expectedResult)
                    : Result<ExposureSettingsDto>.Failure(model.ErrorMessage));

            // NO MediatR - Direct response
            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<ExposureSettingsDto>.Success(expectedResult)
                : Result<ExposureSettingsDto>.Failure(model.ErrorMessage);

            // Store result and update context
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                model.UpdateFromResult(result.Data);
                _context.StoreExposureData(model);
            }

            return result;
        }

        /// <summary>
        /// Calculates ISO for given exposure settings
        /// </summary>
        public async Task<Result<ExposureSettingsDto>> CalculateIsoAsync(ExposureTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            var baseExposure = model.ToBaseExposureTriangle();

            // Create expected result
            var expectedResult = new ExposureSettingsDto
            {
                ShutterSpeed = model.TargetShutterSpeed,
                Aperture = model.TargetAperture,
                Iso = model.ResultIso ?? model.TargetIso,
                ErrorMessage = model.ErrorMessage
            };

            // Setup mock to return expected result
            _exposureCalculatorServiceMock
                .Setup(s => s.CalculateIsoAsync(
                    It.Is<ExposureTriangleDto>(e =>
                        e.ShutterSpeed == baseExposure.ShutterSpeed &&
                        e.Aperture == baseExposure.Aperture &&
                        e.Iso == baseExposure.Iso),
                    It.Is<string>(ss => ss == model.TargetShutterSpeed),
                    It.Is<string>(a => a == model.TargetAperture),
                    It.Is<ExposureIncrements>(inc => inc == model.Increments),
                    It.IsAny<CancellationToken>(),
                    It.Is<double>(ev => Math.Abs(ev - model.EvCompensation) < 0.01)))
                .ReturnsAsync(string.IsNullOrEmpty(model.ErrorMessage)
                    ? Result<ExposureSettingsDto>.Success(expectedResult)
                    : Result<ExposureSettingsDto>.Failure(model.ErrorMessage));

            // NO MediatR - Direct response
            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<ExposureSettingsDto>.Success(expectedResult)
                : Result<ExposureSettingsDto>.Failure(model.ErrorMessage);

            // Store result and update context
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                model.UpdateFromResult(result.Data);
                _context.StoreExposureData(model);
            }

            return result;
        }

        /// <summary>
        /// Gets available shutter speeds for given increment
        /// </summary>
        public async Task<Result<string[]>> GetShutterSpeedsAsync(ExposureIncrements increments)
        {
            var shutterSpeeds = increments switch
            {
                ExposureIncrements.Full => new[] { "1/8000", "1/4000", "1/2000", "1/1000", "1/500", "1/250", "1/125", "1/60", "1/30", "1/15", "1/8", "1/4", "1/2", "1", "2", "4", "8", "15", "30" },
                ExposureIncrements.Half => new[] { "1/8000", "1/6000", "1/4000", "1/3000", "1/2000", "1/1500", "1/1000", "1/750", "1/500", "1/350", "1/250", "1/180", "1/125", "1/90", "1/60", "1/45", "1/30", "1/20", "1/15", "1/10", "1/8", "1/6", "1/4", "0.3", "0.5", "0.7", "1", "1.5", "2", "3", "4", "6", "8", "10", "15", "20", "30" },
                ExposureIncrements.Third => new[] { "1/8000", "1/6400", "1/5000", "1/4000", "1/3200", "1/2500", "1/2000", "1/1600", "1/1250", "1/1000", "1/800", "1/640", "1/500", "1/400", "1/320", "1/250", "1/200", "1/160", "1/125", "1/100", "1/80", "1/60", "1/50", "1/40", "1/30", "1/25", "1/20", "1/15", "1/13", "1/10", "1/8", "1/6", "1/5", "1/4", "0.3", "0.4", "0.5", "0.6", "0.8", "1", "1.3", "1.6", "2", "2.5", "3.2", "4", "5", "6", "8", "10", "13", "15", "20", "25", "30" },
                _ => new[] { "1/125" }
            };

            // Setup mock
            _exposureCalculatorServiceMock
                .Setup(s => s.GetShutterSpeedsAsync(
                    It.Is<ExposureIncrements>(inc => inc == increments),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(shutterSpeeds));

            // NO MediatR - Direct response
            var result = Result<string[]>.Success(shutterSpeeds);
            _context.StoreResult(result);

            return result;
        }

        /// <summary>
        /// Gets available apertures for given increment
        /// </summary>
        public async Task<Result<string[]>> GetAperturesAsync(ExposureIncrements increments)
        {
            var apertures = increments switch
            {
                ExposureIncrements.Full => new[] { "f/1", "f/1.4", "f/2", "f/2.8", "f/4", "f/5.6", "f/8", "f/11", "f/16", "f/22", "f/32", "f/45", "f/64" },
                ExposureIncrements.Half => new[] { "f/1", "f/1.2", "f/1.4", "f/2", "f/2.4", "f/2.8", "f/3.4", "f/4", "f/4.8", "f/5.6", "f/6.7", "f/8", "f/9.5", "f/11", "f/13.5", "f/16", "f/19", "f/22", "f/26.9", "f/32", "f/38.1", "f/45", "f/53.8", "f/64" },
                ExposureIncrements.Third => new[] { "f/1", "f/1.1", "f/1.3", "f/1.4", "f/1.6", "f/1.8", "f/2", "f/2.2", "f/2.5", "f/2.8", "f/3.2", "f/3.6", "f/4", "f/4.5", "f/5", "f/5.6", "f/6.3", "f/7.1", "f/8", "f/9", "f/10.1", "f/11", "f/12.7", "f/14.3", "f/16", "f/18", "f/20.2", "f/22", "f/25.4", "f/28.5", "f/32", "f/36", "f/40.3", "f/45", "f/50.8", "f/57", "f/64" },
                _ => new[] { "f/5.6" }
            };

            // Setup mock
            _exposureCalculatorServiceMock
                .Setup(s => s.GetAperturesAsync(
                    It.Is<ExposureIncrements>(inc => inc == increments),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(apertures));

            // NO MediatR - Direct response
            var result = Result<string[]>.Success(apertures);
            _context.StoreResult(result);

            return result;
        }

        /// <summary>
        /// Gets available ISOs for given increment
        /// </summary>
        public async Task<Result<string[]>> GetIsosAsync(ExposureIncrements increments)
        {
            var isos = increments switch
            {
                ExposureIncrements.Full => new[] { "25600", "12800", "6400", "3200", "1600", "800", "400", "200", "100", "50" },
                ExposureIncrements.Half => new[] { "25600", "17600", "12800", "8800", "6400", "4400", "3600", "3200", "2200", "1600", "1100", "800", "560", "400", "280", "200", "140", "100", "70", "50" },
                ExposureIncrements.Third => new[] { "25600", "20000", "16000", "12800", "10000", "8000", "6400", "5000", "4000", "3200", "2500", "2000", "1600", "1250", "1000", "800", "640", "500", "400", "320", "250", "200", "160", "125", "100", "70", "50" },
                _ => new[] { "100" }
            };

            // Setup mock
            _exposureCalculatorServiceMock
                .Setup(s => s.GetIsosAsync(
                    It.Is<ExposureIncrements>(inc => inc == increments),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(isos));

            // NO MediatR - Direct response
            var result = Result<string[]>.Success(isos);
            _context.StoreResult(result);

            return result;
        }

        /// <summary>
        /// Gets exposure by settings - context management pattern
        /// </summary>
        public async Task<Result<ExposureTestModel>> GetExposureBySettingsAsync(string shutterSpeed, string aperture, string iso)
        {
            // Check individual context first
            var individual = _context.GetExposureData();
            if (individual != null &&
                individual.BaseShutterSpeed == shutterSpeed &&
                individual.BaseAperture == aperture &&
                individual.BaseIso == iso)
            {
                var response = new ExposureTestModel
                {
                    Id = individual.Id,
                    BaseShutterSpeed = individual.BaseShutterSpeed,
                    BaseAperture = individual.BaseAperture,
                    BaseIso = individual.BaseIso,
                    TargetShutterSpeed = individual.TargetShutterSpeed,
                    TargetAperture = individual.TargetAperture,
                    TargetIso = individual.TargetIso,
                    ResultShutterSpeed = individual.ResultShutterSpeed,
                    ResultAperture = individual.ResultAperture,
                    ResultIso = individual.ResultIso,
                    Increments = individual.Increments,
                    FixedValue = individual.FixedValue,
                    EvCompensation = individual.EvCompensation,
                    ErrorMessage = individual.ErrorMessage
                };
                var result = Result<ExposureTestModel>.Success(response);
                _context.StoreResult(result);
                return result;
            }

            // Check collection contexts
            var collectionKeys = new[] { "AllExposures", "SetupExposures" };
            foreach (var collectionKey in collectionKeys)
            {
                var collection = _context.GetModel<List<ExposureTestModel>>(collectionKey);
                var found = collection?.FirstOrDefault(e =>
                    e.BaseShutterSpeed == shutterSpeed &&
                    e.BaseAperture == aperture &&
                    e.BaseIso == iso);

                if (found != null)
                {
                    var response = new ExposureTestModel
                    {
                        Id = found.Id,
                        BaseShutterSpeed = found.BaseShutterSpeed,
                        BaseAperture = found.BaseAperture,
                        BaseIso = found.BaseIso,
                        TargetShutterSpeed = found.TargetShutterSpeed,
                        TargetAperture = found.TargetAperture,
                        TargetIso = found.TargetIso,
                        ResultShutterSpeed = found.ResultShutterSpeed,
                        ResultAperture = found.ResultAperture,
                        ResultIso = found.ResultIso,
                        Increments = found.Increments,
                        FixedValue = found.FixedValue,
                        EvCompensation = found.EvCompensation,
                        ErrorMessage = found.ErrorMessage
                    };
                    var result = Result<ExposureTestModel>.Success(response);
                    _context.StoreResult(result);
                    return result;
                }
            }

            // Not found
            var failureResult = Result<ExposureTestModel>.Failure($"Exposure with settings '{shutterSpeed}, {aperture}, {iso}' not found");
            _context.StoreResult(failureResult);
            return failureResult;
        }
    }
}