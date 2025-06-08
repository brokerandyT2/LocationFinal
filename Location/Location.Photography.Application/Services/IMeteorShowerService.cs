using Location.Photography.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public interface IMeteorShowerService
    {
        Task<List<MeteorShower>> GetActiveShowersAsync(DateTime date);
        Task<MeteorShower> GetShowerByCodeAsync(string code);
    }
}
