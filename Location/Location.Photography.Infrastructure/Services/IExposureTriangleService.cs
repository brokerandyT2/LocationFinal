// Create this interface in Location/Location.Photography.Infrastructure/Services/IExposureTriangleService.cs
using System;

namespace Location.Photography.Infrastructure.Services
{
    public interface IExposureTriangleService
    {
        string CalculateShutterSpeed(string? baseShutterSpeed, string? baseAperture, string? baseIso,
                                    string? targetAperture, string? targetIso, int increments, double evCompensation);

        string CalculateAperture(string? baseShutterSpeed, string? baseAperture, string? baseIso,
                                string? targetShutterSpeed, string? targetIso, int increments, double evCompensation);

        string CalculateIso(string? baseShutterSpeed, string? baseAperture, string? baseIso,
                           string? targetShutterSpeed, string? targetAperture, int increments, double evCompensation);
    }
}