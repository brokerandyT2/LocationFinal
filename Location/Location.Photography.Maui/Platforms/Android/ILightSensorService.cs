using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Photography.Maui.Platforms.Android
{
    public interface ILightSensorService
    {
        void StartListening();
        void StopListening();
        float GetCurrentLux();
    }
}
