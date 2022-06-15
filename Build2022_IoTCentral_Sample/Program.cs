using System;
using System.Diagnostics;
using System.Threading;

using nanoFramework.Hardware.Esp32;
using nanoFramework.Azure.Devices.Client;
using nanoFramework.Azure.Devices.Provisioning.Client ;
using nanoFramework.Networking;
using nanoFramework.AtomLite;
using nanoFramework.M2Mqtt.Messages;
using System.Device.I2c;
using Iot.Device.Sht3x;

namespace Build2022_IoTCentral_Sample
{
    public class Program
    {

        const string ssid = "your SSID";
        const string password = "your Password";
        
        const string dspAddress = "global.azure-devices-provisioning.net";
        
        const string idscope = "xxxxxxx";
        const string registrationid = "xxxxxxxxx";
        const string saskey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=";

        

        public static void Main()
        {
            //Wifi connection
            if (!ConnectWifi())
            {
                Debug.WriteLine("Wifi Connection failed...");
                return;
            }
            else
            {
                Debug.WriteLine("Wifi Connected...");
            }

            Thread.Sleep(5000);

            //DPS setting
            var provisioning = ProvisioningDeviceClient.Create(dspAddress, idscope, registrationid, saskey);

            var myDevice = provisioning.Register(null, new CancellationTokenSource(30000).Token);
            
            if (myDevice.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                Debug.WriteLine($"Registration is not assigned: {myDevice.Status}, error message: {myDevice.ErrorMessage}");
                return;
            }

            Debug.WriteLine($"Device successfully assigned:");

            //IoTCentoral connect
            var device = new DeviceClient(myDevice.AssignedHub, registrationid, saskey, MqttQoSLevel.AtMostOnce);

            var res = device.Open();

            if (!res)
            {
                Debug.WriteLine("can't open the device");
                return;
            }

            //event
            device.AddMethodCallback(ledon);
            device.AddMethodCallback(ledoff);

            //LED
            var rgb = AtomLite.NeoPixel;
            rgb.Image.SetPixel(0, 0, System.Drawing.Color.FromArgb(0, 0, 128, 128));
            rgb.Update();

            //I2C setting
            Configuration.SetPinFunction(26, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(32, DeviceFunction.I2C1_CLOCK);

            I2cConnectionSettings settings = new I2cConnectionSettings(1, 0x44);

            using I2cDevice i2cdevice=I2cDevice.Create(settings);
            using Sht3x sensor = new(i2cdevice);

            while (true)
            {
                device.SendMessage($"{{\"temperature\":{sensor.Temperature.DegreesCelsius.ToString("F2")},\"humidity\":{sensor.Humidity.Percent.ToString("F2")}}}", new CancellationTokenSource(2000).Token);

                Debug.WriteLine($"{{\"temperature\":{sensor.Temperature.DegreesCelsius.ToString("F2")},\"humidity\":{sensor.Humidity.Percent.ToString("F2")}}}");
                Thread.Sleep(60000);
            }
        }   

        private static bool ConnectWifi()
        {
            Debug.WriteLine("Connecting Wifi....");
            var success=WifiNetworkHelper.ConnectDhcp(ssid,password,reconnectionKind:System.Device.Wifi.WifiReconnectionKind.Automatic,requiresDateTime:true,token:new CancellationTokenSource(60000).Token);

            if (!success)
            {
                Debug.WriteLine($"Can't connect to the network, error: {WifiNetworkHelper.Status}");

                if (WifiNetworkHelper.HelperException != null)
                {
                    Debug.WriteLine($"ex: {WifiNetworkHelper.HelperException}");
                }
            }

            Debug.WriteLine($"Date and time is now {DateTime.UtcNow}");

            return success;
        }

        private static string ledon(int rid,string payload)
        {
            var rgb = AtomLite.NeoPixel;
            rgb.Image.SetPixel(0, 0, System.Drawing.Color.FromArgb(0, 128, 128, 128));
            rgb.Update();

            Debug.WriteLine($"Call back called :-) rid={rid}, payload={payload}");
            return "{\"Yes\":\"baby\",\"itisworking\":42}";
        }

        private static string ledoff(int rid, string payload)
        {
            var rgb = AtomLite.NeoPixel;
            rgb.Image.SetPixel(0, 0, System.Drawing.Color.FromArgb(0, 0, 0, 0));
            rgb.Update();

            Debug.WriteLine($"Call back called :-) rid={rid}, payload={payload}");
            return "{\"Yes\":\"baby\",\"itisworking\":42}";
        }
    }
}
