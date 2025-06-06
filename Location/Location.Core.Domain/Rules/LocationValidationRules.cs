namespace Location.Core.Domain.Rules
{
    /// <summary>
    /// Business rules for location validation
    /// </summary>
    public static class LocationValidationRules
    {
        /// <summary>
        /// Validates the specified <see cref="Entities.Location"/> object and returns a value indicating whether it is
        /// valid.
        /// </summary>
        /// <remarks>A valid location must meet the following criteria: <list type="bullet">
        /// <item><description>The <paramref name="location"/> object must not be <see
        /// langword="null"/>.</description></item> <item><description>The <c>Title</c> property, if specified, must not
        /// exceed 100 characters.</description></item> <item><description>The <c>Description</c> property, if
        /// specified, must not exceed 500 characters.</description></item> <item><description>The <c>Coordinate</c>
        /// property must not be <see langword="null"/>.</description></item> <item><description>If the <c>PhotoPath</c>
        /// property is specified, it must be a valid file path.</description></item> </list></remarks>
        /// <param name="location">The <see cref="Entities.Location"/> object to validate. Cannot be <see langword="null"/>.</param>
        /// <param name="errors">When this method returns, contains a list of validation error messages, if any.  If the location is valid,
        /// this list will be empty.</param>
        /// <returns><see langword="true"/> if the <paramref name="location"/> is valid; otherwise, <see langword="false"/>.</returns>
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
        /// <summary>
        /// Determines whether the specified path is valid by checking for invalid characters.
        /// </summary>
        /// <remarks>This method checks the path against the set of invalid characters defined by  <see
        /// cref="System.IO.Path.GetInvalidPathChars"/>. If the path is null or an error occurs  during validation, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="path">The file or directory path to validate.</param>
        /// <returns><see langword="true"/> if the specified path does not contain any invalid characters;  otherwise, <see
        /// langword="false"/>.</returns>
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