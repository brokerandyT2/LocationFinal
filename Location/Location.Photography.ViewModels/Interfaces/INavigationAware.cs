using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Photography.ViewModels.Interfaces
{
    public interface INavigationAware
    {
        void OnNavigatedToAsync();
        void OnNavigatedFromAsync();
    }
}
