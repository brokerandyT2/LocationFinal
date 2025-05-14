using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Location.Core.Application.Commands.Locations; // This should import both DeleteLocationCommand AND DeleteLocationCommandHandler
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using FluentAssertions;

namespace Location.Core.Application.Tests.Locations.Commands.DeleteLocation
{
    [TestFixture]
    public class DeleteLocationCommandHandlerTests
    {
       
    }
}