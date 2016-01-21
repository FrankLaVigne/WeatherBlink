using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using System.Net.Http;
using Windows.Data.Json;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WeatherBlink
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int FAST_INTERVAL = 500;
        private const int SLOW_INTERVAL = 1000;

        private const int LED_PIN = 5;
        private GpioPin gpioPin;
        private GpioPinValue gpioPinValue;
        private DispatcherTimer blinkingTimer;

        public MainPage()
        {
            this.InitializeComponent();

            AutoRun();

        }

        private void AutoRun()
        {
            LoadWeatherData();
        }

        private void BlinkFast()
        {
            Blink(500);
        }

        private void BlinkSlow()
        {
            Blink(1000);
        }


        private void StopBlinking()
        {
            blinkingTimer.Stop();
        }

        private void TurnLightOff()
        {
            SetLightStatus(GpioPinValue.Low);
        }

        private void TurnLightOn()
        {
            SetLightStatus(GpioPinValue.High);
        }


        private void SetLightStatus(GpioPinValue pinValue)
        {
            gpioPin.Write(pinValue);
        }


        private void Blink(int interval)
        {
            blinkingTimer = new DispatcherTimer();
            blinkingTimer.Interval = TimeSpan.FromMilliseconds(interval);
            blinkingTimer.Tick += BlinkingTimer_Tick;
            InitializeGPIO();
            if (gpioPin != null)
            {
                blinkingTimer.Start();
            }
        }

        private void BlinkingTimer_Tick(object sender, object e)
        {
            if (gpioPinValue == GpioPinValue.High)
            {
                TurnLightOff();
            }
            else
            {
                TurnLightOn();
            }

            gpioPin.Write(gpioPinValue);

        }
        private void InitializeGPIO()
        {
            var gpioController = GpioController.GetDefault();

            if (gpioController == null)
            {
                gpioPin = null;
                txtStatus.Text = "Error: GPIO controller not found.";
                return;
            }

            gpioPin = gpioController.OpenPin(LED_PIN);
            gpioPinValue = GpioPinValue.High;
            gpioPin.Write(gpioPinValue);
            gpioPin.SetDriveMode(GpioPinDriveMode.Output);

            txtStatus.Text = "GPIO pin initialized correctly.";

        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            LoadWeatherData();

        }

        private async void LoadWeatherData()
        {
            var apiKey = "eaeffbce900c25b56ee86ee90e43615c";
            var location = this.txbLocation.Text;
            var apiRequestFormatString = "http://api.openweathermap.org/data/2.5/weather?zip={0},us&appid={1}";

            txtStatus.Text = string.Format("Loading weather data for {0}", location);

            HttpClient webClient = new HttpClient();
            var response = await webClient.GetAsync(string.Format(apiRequestFormatString, location, apiKey));
            var weatherJson = await response.Content.ReadAsStringAsync();

            JsonObject jsonObject = JsonObject.Parse(weatherJson);

            var weatherObject = jsonObject.GetNamedObject("main");
            var minTempValue = weatherObject.GetNamedValue("temp_min");
            var minTempDouble = minTempValue.GetNumber();

            // 38F/3.3C = 276.483 Kelvin 

            if (minTempDouble <= 276.483)
            {
                BlinkFast();
                txtStatus.Text = "Freeze Warning!";

            }
            else
            {
                BlinkSlow();
                txtStatus.Text = "No freezing weather in forecast.";

            }




        }
    }
}
