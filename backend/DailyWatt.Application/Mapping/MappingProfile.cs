using AutoMapper;
using DailyWatt.Application.DTO.Responses;
using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Models;
using DailyWatt.Domain.Services;

namespace DailyWatt.Application.Mapping;

/// <summary>
/// AutoMapper profile for mapping domain entities and models to DTOs.
/// Domain-agnostic mapping that can be reused across different API versions.
/// </summary>
public class MappingProfile : Profile
{
  public MappingProfile()
  {
    // Auth mappings
    CreateMap<DailyWattUser, UserProfileDto>()
        .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
        .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName ?? string.Empty));

    // Dashboard mappings
    CreateMap<AggregatedConsumptionPoint, ConsumptionPointDto>();

    CreateMap<ConsumptionSummary, SummaryDto>();

    CreateMap<WeatherData, WeatherDayDto>();
    CreateMap<WeatherDay, WeatherDayDto>()
      .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToString("yyyy-MM-dd")));

    // Enedis mappings
    CreateMap<ImportJob, ImportJobDto>()
        .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
  }
}
