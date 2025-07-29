using Location.Core.Application.Services;
using Location.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Location.Core.Application.Services;
namespace Location.Photography.Application
{
    public static class DependencyBootstrapper
    {
        private static Microsoft.Extensions.DependencyInjection.IServiceCollection _serviceProvider;

        public static void Initialize()
        {
            _serviceProvider = new ServiceCollection();
;


        }
    }
}
