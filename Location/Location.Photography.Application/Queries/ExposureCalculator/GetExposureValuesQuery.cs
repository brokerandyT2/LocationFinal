// Location.Photography.Application/Queries/ExposureCalculator/GetExposureValuesQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Queries.ExposureCalculator
{
    public class GetExposureValuesQuery : IRequest<Result<ExposureValuesDto>>
    {
        public ExposureIncrements Increments { get; set; }
    }

    public class ExposureValuesDto
    {
        public string[] ShutterSpeeds { get; set; }
        public string[] Apertures { get; set; }
        public string[] ISOs { get; set; }
    }

    public class GetExposureValuesQueryHandler : IRequestHandler<GetExposureValuesQuery, Result<ExposureValuesDto>>
    {
        private readonly IExposureCalculatorService _exposureCalculatorService;

        public GetExposureValuesQueryHandler(IExposureCalculatorService exposureCalculatorService)
        {
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
        }

        public async Task<Result<ExposureValuesDto>> Handle(GetExposureValuesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var shutterSpeedsResult = await _exposureCalculatorService.GetShutterSpeedsAsync(request.Increments, cancellationToken);
                var aperturesResult = await _exposureCalculatorService.GetAperturesAsync(request.Increments, cancellationToken);
                var isosResult = await _exposureCalculatorService.GetIsosAsync(request.Increments, cancellationToken);

                if (!shutterSpeedsResult.IsSuccess || !aperturesResult.IsSuccess || !isosResult.IsSuccess)
                {
                    string errorMessage = shutterSpeedsResult.ErrorMessage ?? aperturesResult.ErrorMessage ?? isosResult.ErrorMessage ?? "Failed to retrieve exposure values";
                    return Result<ExposureValuesDto>.Failure(errorMessage);
                }

                var result = new ExposureValuesDto
                {
                    ShutterSpeeds = shutterSpeedsResult.Data,
                    Apertures = aperturesResult.Data,
                    ISOs = isosResult.Data
                };

                return Result<ExposureValuesDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<ExposureValuesDto>.Failure($"Error retrieving exposure values: {ex.Message}");
            }
        }
    }
}