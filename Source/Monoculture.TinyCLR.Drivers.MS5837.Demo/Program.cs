/*
 * Copyright 2019 David Smith
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
