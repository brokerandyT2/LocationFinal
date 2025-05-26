// Location.Photography.Application/Common/Mappings/MappingProfile.cs
using AutoMapper;
using Location.Photography.Application.Commands.Subscription;
using Location.Photography.Application.Queries.Subscription;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;

namespace Location.Photography.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Subscription mappings
            CreateMap<Subscription, SubscriptionStatusDto>()
                .ForMember(dest => dest.HasActiveSubscription, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsExpiringSoon, opt => opt.MapFrom(src => src.IsExpiringSoon()))
                .ForMember(dest => dest.DaysUntilExpiration, opt => opt.MapFrom(src => src.DaysUntilExpiration()));

            CreateMap<ProcessSubscriptionResultDto, Subscription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ConstructUsing((src, context) => new Subscription(
                    src.ProductId,
                    src.TransactionId,
                    src.PurchaseToken,
                    src.PurchaseDate,
                    src.ExpirationDate,
                    src.Status,
                    src.ProductId.Contains("monthly") ? SubscriptionPeriod.Monthly : SubscriptionPeriod.Yearly,
                    string.Empty // UserId will be set separately
                ));

            // Sun calculation mappings
            CreateMap<SunTimesDto, SunPositionDto>()
                .ForMember(dest => dest.Azimuth, opt => opt.Ignore())
                .ForMember(dest => dest.Elevation, opt => opt.Ignore())
                .ForMember(dest => dest.DateTime, opt => opt.MapFrom(src => src.Date));

            // Exposure calculation mappings
            CreateMap<ExposureTriangleDto, ExposureSettingsDto>();

            // Scene evaluation mappings
            CreateMap<SceneEvaluationResultDto, SceneEvaluationStatsDto>()
                .ForMember(dest => dest.MeanRed, opt => opt.MapFrom(src => src.Stats.MeanRed))
                .ForMember(dest => dest.MeanGreen, opt => opt.MapFrom(src => src.Stats.MeanGreen))
                .ForMember(dest => dest.MeanBlue, opt => opt.MapFrom(src => src.Stats.MeanBlue))
                .ForMember(dest => dest.MeanContrast, opt => opt.MapFrom(src => src.Stats.MeanContrast))
                .ForMember(dest => dest.StdDevRed, opt => opt.MapFrom(src => src.Stats.StdDevRed))
                .ForMember(dest => dest.StdDevGreen, opt => opt.MapFrom(src => src.Stats.StdDevGreen))
                .ForMember(dest => dest.StdDevBlue, opt => opt.MapFrom(src => src.Stats.StdDevBlue))
                .ForMember(dest => dest.StdDevContrast, opt => opt.MapFrom(src => src.Stats.StdDevContrast))
                .ForMember(dest => dest.TotalPixels, opt => opt.MapFrom(src => src.Stats.TotalPixels));
        }
    }
}