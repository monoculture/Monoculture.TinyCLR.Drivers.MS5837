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

using System;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace Monoculture.TinyCLR.Drivers.MS5837
{
    public class MS5837Driver
    {
        private readonly I2cDevice _device;
        private readonly MS5837Model _model;
        private readonly ushort[] _coefficients = new ushort[8];

        public MS5837Driver(I2cDevice device, MS5837Model model = MS5837Model.MS583730BA)
        {
            _model = model;

            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public static I2cConnectionSettings GetI2CConnectionSettings() => new I2cConnectionSettings(0x76)
        {
            BusSpeed = I2cBusSpeed.StandardMode,
            AddressFormat = I2cAddressFormat.SevenBit
        };

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            LoadCalibration();

            IsInitialized = true;
        }

        private void LoadCalibration()
        {
            WriteByte(0x1E);

            Thread.Sleep(500);

            for (var i = 0; i < 7; i++)
            {
                var readBuffer = new byte[2];

                var command = new[] { (byte)(0xA0 + i * 2) };

                _device.WriteRead(command, readBuffer);

                if (BitConverter.IsLittleEndian)
                {
                    var tmp = readBuffer[0];
                    readBuffer[0] = readBuffer[1];
                    readBuffer[1] = tmp;
                }

                _coefficients[i] = BitConverter.ToUInt16(readBuffer, 0);
            }

            var storedCrc = _coefficients[0] >> 12;

            var calculatedCrc = Crc4(_coefficients);

            if(storedCrc != calculatedCrc)
                throw new ApplicationException("CRC error reading configuration.");
        }

        private static int Crc4(ushort[] data)
        {
            var nRem = 0;

            data[7] = 0;

            data[0] &= 0x0FFF;

            for (short i = 0; i < 16; i++)
            {
                if (i % 2 == 1)
                {
                    nRem ^= data[i >> 1] & 0x00FF;
                }
                else
                {
                    nRem ^= data[i >> 1] >> 8;
                }

                for (ushort nBit = 8; nBit > 0; nBit--)
                {
                    if ((nRem & 0x8000) != 0)
                    {
                        nRem = (nRem << 1) ^ 0x3000;
                    }
                    else
                    {
                        nRem = nRem << 1;
                    }
                }
            }

            nRem = (nRem >> 12) & 0x000F;

            return nRem ^ 0x00;
        }


        public MS5837Result Read(MS5837SampleRate sampleRate = MS5837SampleRate.Osr256)
        {
            if (IsInitialized == false)
                Initialize();

            var conversionDelay = GetConversionDelay(sampleRate);

            WriteByte((byte)(sampleRate + 64));

            Thread.Sleep(conversionDelay);

            var buffer = ReadBytes(0x00, 3);

            var d1 = (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];

            WriteByte((byte)(sampleRate + 80));

            Thread.Sleep(conversionDelay);

            buffer = ReadBytes(0x00, 3);

            var d2 = (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];

            long sens;
            long off;
            long senSi = 0;
            long ofFi = 0;
            long ti = 0;

            var dT = d2 - _coefficients[5] * 256;

            if (_model == MS5837Model.MS583702BA)
            {
                sens = _coefficients[1] * 65536L + _coefficients[3] * dT / 128L;

                off = _coefficients[2] * 131072L + _coefficients[4] * dT / 64L;
            }
            else
            {
                sens = _coefficients[1] * 32768L + _coefficients[3] * dT / 256L;

                off = _coefficients[2] * 65536L + _coefficients[4] * dT / 128L;
            }

            var temperature = 2000L + dT * _coefficients[6] / 8388608;

            if (_model == MS5837Model.MS583702BA)
            {
                if (temperature / 100 < 20)
                {
                    ti = 11 * dT * dT / 34359738368L;
                    ofFi = 31 * (temperature - 2000) * (temperature - 2000) / 8;
                    senSi = 63 * (temperature - 2000) * (temperature - 2000) / 32;
                }
            }
            else
            {
                if (temperature / 100 < 20) // Low temperature
                {
                    ti = 3 * (dT * dT / 8589934592L);

                    ofFi = 3 * ((temperature - 2000) * (temperature - 2000) / 2);

                    senSi = 5 * ((temperature - 2000) * (temperature - 2000) / 8);

                    if (temperature / 100 < -15) // Very low temperature
                    {
                        ofFi = ofFi + 7 * (temperature + 1500) * (temperature + 1500);

                        senSi = senSi + 4 * (temperature + 1500) * (temperature + 1500);
                    }
                }
                else // High temperature
                {
                    ti = 2 * (dT * dT / 137438953472L);

                    ofFi = 1 * ((temperature - 2000) * (temperature - 2000) / 16);

                    senSi = 0;
                }
            }

            var off2 = off - ofFi;

            var sens2 = sens - senSi;

            var result = new MS5837Result();

            if (_model == MS5837Model.MS583702BA)
            {
                result.Temperature = temperature - ti;
                result.Pressure = (d1 * sens2 / 2097152L - off2) / 32768L / 100;
            }
            else
            {
                result.Temperature = temperature - ti;
                result.Pressure = (d1 * sens2 / 2097152L - off2) / 8192L / 10;
            }

            return result;
        }

        private static int GetConversionDelay(MS5837SampleRate sampleRate)
        {
            var conversionDelay = 0;

            switch (sampleRate)
            {
                case MS5837SampleRate.Osr256:
                    conversionDelay = 1;
                    break;
                case MS5837SampleRate.Osr512:
                    conversionDelay = 2;
                    break;
                case MS5837SampleRate.Osr1024:
                    conversionDelay = 3;
                    break;
                case MS5837SampleRate.Osr2048:
                    conversionDelay = 5;
                    break;
                case MS5837SampleRate.Osr4096:
                    conversionDelay = 10;
                    break;
                case MS5837SampleRate.Osr8192:
                    conversionDelay = 20;
                    break;
            }

            return conversionDelay;
        }

        private void WriteByte(byte command)
        {
            _device.Write(new[] { command });
        }

        private byte[] ReadBytes(byte address, ushort readCount)
        {
            var writeBuffer = new[]{ address };

            var readBuffer = new byte[readCount];

            _device.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }
    }
}
