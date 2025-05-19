using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Location.Core.Application.Locations.Queries.GetLocationById
{
    /// <summary>
    /// Represents a query to retrieve a location by its unique identifier.
    /// </summary>
    /// <remarks>This query is used to request a location's details by providing its unique identifier. The
    /// result contains a <see cref="LocationDto"/> object if the location is found, or an error if not.</remarks>
    public class GetLocationByIdQuery : IRequest<Result<LocationDto>>
    {
        public int Id { get; set; }
    }
}

