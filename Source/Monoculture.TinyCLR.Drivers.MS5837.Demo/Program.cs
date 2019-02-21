using System.Diagnostics;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Pins;

namespace Monoculture.TinyCLR.Drivers.MS5837.Demo
{
    class Program
    {
        static void Main()
        {
            Read();
        }

        private static void Read()
        {
            var driver = GetMs5837Driver();

            driver.Initialize();

            for (var i = 0; i < 10; i++)
            {
                var result = driver.Read();

                Debug.WriteLine("Pressure " + result.Pressure);
                Debug.WriteLine("Temperature: " + result.Temperature);
            }
        }

        private static MS5837Driver GetMs5837Driver()
        {
            var settings = MS5837Driver.GetI2CConnectionSettings();

            var controller = I2cController.FromName(G120E.I2cBus.I2c0);

            var device = controller.GetDevice(settings);

            return new MS5837Driver(device);
        }
    }
}
