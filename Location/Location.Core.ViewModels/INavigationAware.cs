using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.ViewModels
{
    public interface INavigationAware
    {
        void OnNavigatedToAsync();
        void OnNavigatedFromAsync();
    }
}
