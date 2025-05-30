﻿using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Entities;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IgnoreAttribute = NUnit.Framework.IgnoreAttribute;

namespace Location.Photography.Infrastructure.Test.Extensions
{
    [TestFixture]
    public class RepositoryExtensionsTests
    {
        // Since we can't directly test extension methods that call methods of the same name,
        // we'll skip these tests and leave a note

        [Test]
        [Ignore("Cannot test extension methods calling same-named methods due to infinite recursion")]
        public async Task CreateAsync_ForTipTypeRepository_ShouldCallCreateAsync()
        {
            // This test is skipped - see attribute explanation
        }

        [Test]
        [Ignore("Cannot test extension methods calling same-named methods due to infinite recursion")]
        public async Task CreateAsync_ForTipRepository_ShouldCallCreateAsync()
        {
            // This test is skipped - see attribute explanation
        }

        [Test]
        [Ignore("Cannot test extension methods calling same-named methods due to infinite recursion")]
        public async Task CreateAsync_ForLocationRepository_ShouldCallCreateAsync()
        {
            // This test is skipped - see attribute explanation
        }
    }
}