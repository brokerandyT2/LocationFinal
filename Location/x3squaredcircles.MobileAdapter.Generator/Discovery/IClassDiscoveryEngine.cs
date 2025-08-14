using System.Collections.Generic;
using System.Threading.Tasks;
using x3squaredcircles.MobileAdapter.Generator.Configuration;

namespace x3squaredcircles.MobileAdapter.Generator.Discovery
{
    public interface IClassDiscoveryEngine
    {
        Task<List<DiscoveredClass>> DiscoverClassesAsync(GeneratorConfiguration config);
    }
}