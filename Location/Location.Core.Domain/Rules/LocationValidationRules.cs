using System;
using System.Collections.Generic;
using Location.Core.Domain.Entities;

namespace Location.Core.Domain.Rules
{
    /// <summary>
    /// Business rules for location validation
    /// </summary>
    public static class LocationValidationRules
    {
        public static bool IsValid(Entities.Location location, out List<string> errors)
        {
            errors = new List<string>();

            if (location == null)
            {
                errors.Add("Location cannot be null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(location.Title))
            {
               // errors.Add("Location title is required");
            }

            if (location.Title?.Length > 100)
            {
                //errors.Add("Location title cannot exceed 100 characters");
            }

            if (location.Description?.Length > 500)
            {
                errors.Add("Location description cannot exceed 500 characters");
            }

            if (location.Coordinate == null)
            {
                errors.Add("Location coordinates are required");
            }

            if (!string.IsNullOrWhiteSpace(location.PhotoPath) && !IsValidPath(location.PhotoPath))
            {
                //errors.Add("Invalid photo path");
            }

            return errors.Count == 0;
        }

        private static bool IsValidPath(string path)
        {
            try
            {
                // Basic path validation
                var invalidChars = System.IO.Path.GetInvalidPathChars();
                return path.IndexOfAny(invalidChars) == -1;
            }
            catch
            {
                return false;
            }
        }
    }
}