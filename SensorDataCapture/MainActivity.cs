using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;
using Lamp;
using System.Threading;
using Android.Hardware;
using Android.Content.PM;

namespace SensorDataCapture{

    [Activity(Label = "SensorDataCapture", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity, ISensorEventListener{
        
        private int progressBarStatus;
        private bool hasAccelerometer, hasMagnetometer, hasGyroscope;

        static readonly object _syncLock = new object();
        SensorManager _sensorManager;
        TextView _xAccelerometer, _yAccelerometer, _zAccelerometer;
        TextView _xGyroscope, _yGyroscope, _zGyroscope;
        TextView _xMagnetometer, _yMagnetometer, _zMagnetometer;
        TextView _status;
        Button _startButton, _stopButton;
        ProgressBar _progressBar;

        System.IO.StreamWriter accelerometerWriter = null;
        System.IO.StreamWriter magnetometerWriter = null;
        System.IO.StreamWriter gyroscopeWriter = null;

        protected override void OnCreate(Bundle bundle){

            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it

            ActionBar.Hide();

            PackageManager pack = PackageManager;
            hasAccelerometer = pack.HasSystemFeature(PackageManager.FeatureSensorAccelerometer);
            hasMagnetometer = pack.HasSystemFeature(PackageManager.FeatureSensorCompass);
            hasGyroscope = pack.HasSystemFeature(PackageManager.FeatureSensorGyroscope);

            _startButton = FindViewById<Button>(Resource.Id.startButton);
            _stopButton = FindViewById<Button>(Resource.Id.stopButton);
            _progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);


            _sensorManager = (SensorManager)GetSystemService(Context.SensorService);

            _xAccelerometer = FindViewById<TextView>(Resource.Id.xAccelerometerValue);
            _yAccelerometer = FindViewById<TextView>(Resource.Id.yAccelerometerValue);
            _zAccelerometer = FindViewById<TextView>(Resource.Id.zAccelerometerValue);

            _xMagnetometer = FindViewById<TextView>(Resource.Id.xMagnetometerValue);
            _yMagnetometer = FindViewById<TextView>(Resource.Id.yMagnetometerValue);
            _zMagnetometer = FindViewById<TextView>(Resource.Id.zMagnetometerValue);

            _xGyroscope = FindViewById<TextView>(Resource.Id.xGyroscopeValue);
            _yGyroscope = FindViewById<TextView>(Resource.Id.yGyroscopeValue);
            _zGyroscope = FindViewById<TextView>(Resource.Id.zGyroscopeValue);

            _status = FindViewById<TextView>(Resource.Id.statusText);

            var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;

            var accelerometerDir = System.IO.Path.Combine(sdCardPath, "accelerometer");
            if (!System.IO.File.Exists(accelerometerDir))
                System.IO.Directory.CreateDirectory(accelerometerDir);

            var magnetometerDir = System.IO.Path.Combine(sdCardPath, "magnetometer");
            if (!System.IO.File.Exists(magnetometerDir))
                System.IO.Directory.CreateDirectory(magnetometerDir);

            var gyroscopeDir = System.IO.Path.Combine(sdCardPath, "gyroscope");
            if (!System.IO.File.Exists(gyroscopeDir))
                System.IO.Directory.CreateDirectory(gyroscopeDir);

            _startButton.Click += startButtonClick;
            _stopButton.Click += stopButtonClick;

        }

        private void startButtonClick(object sender, EventArgs e){

            ProgressBar _progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            Button _stopButton = FindViewById<Button>(Resource.Id.stopButton);
            Button _startButton = FindViewById<Button>(Resource.Id.startButton);
            TextView _status = FindViewById<TextView>(Resource.Id.statusText);

            int secondsToStart = 3;
            int secondsCount = 0;
            RunOnUiThread(() => { _status.Text = string.Format("{0:d}", secondsToStart - secondsCount); });
            progressBarStatus = 0;
            _progressBar.Progress = progressBarStatus;

            var v = Lamp.Plugin.CrossLamp.Current;
            v.TurnOn();

            new Thread(new ThreadStart(delegate {

                RunOnUiThread(() => { _startButton.Enabled = false; });

                while (progressBarStatus < 100){
                    Thread.Sleep(1000);
                    secondsCount++;
                    progressBarStatus = secondsCount * 100/secondsToStart;                    
                    _progressBar.Progress = progressBarStatus;
                    RunOnUiThread(() => { _status.Text = string.Format("{0:d}", secondsToStart - secondsCount); });
                }

                v.TurnOff();
                RunOnUiThread(() => { _stopButton.Enabled = true; });
                RunOnUiThread(() => { _status.Text = "Capturing..."; });

                string startTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;

                if (hasAccelerometer){                    
                    var filePath = System.IO.Path.Combine(sdCardPath, "accelerometer", startTimestamp + "_acc.txt");
                    accelerometerWriter = new System.IO.StreamWriter(filePath, true);
                    _sensorManager.RegisterListener(this, _sensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Game);
                }
                if (hasMagnetometer)
                {
                    var filePath = System.IO.Path.Combine(sdCardPath, "magnetometer", startTimestamp + "_mag.txt");
                    magnetometerWriter = new System.IO.StreamWriter(filePath, true);
                    _sensorManager.RegisterListener(this, _sensorManager.GetDefaultSensor(SensorType.MagneticField), SensorDelay.Game);
                }
                if (hasGyroscope)
                {
                    var filePath = System.IO.Path.Combine(sdCardPath, "gyroscope", startTimestamp + "_gyr.txt");
                    gyroscopeWriter = new System.IO.StreamWriter(filePath, true);
                    _sensorManager.RegisterListener(this, _sensorManager.GetDefaultSensor(SensorType.Gyroscope), SensorDelay.Game);
                }


            })).Start();            

        }


        private void stopButtonClick(object sender, EventArgs e){

            ProgressBar _progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            Button _stopButton = FindViewById<Button>(Resource.Id.stopButton);
            Button _startButton = FindViewById<Button>(Resource.Id.startButton);
            TextView _status = FindViewById<TextView>(Resource.Id.statusText);

            RunOnUiThread(() => { _status.Text = "..."; });
            _progressBar.Progress = 0;

            RunOnUiThread(() => { _stopButton.Enabled = false; });
            RunOnUiThread(() => { _startButton.Enabled = true; });

            _sensorManager.UnregisterListener(this);

            _xAccelerometer.Text = _yAccelerometer.Text  = _zAccelerometer.Text = "--";
            _xMagnetometer.Text = _yMagnetometer.Text = _zMagnetometer.Text = "--";
            _xGyroscope.Text = _yGyroscope.Text = _zGyroscope.Text = "--";

            if (accelerometerWriter != null)
            {
                accelerometerWriter.Close();
            }

        }

        protected override void OnResume()
        {
            base.OnResume();
        }

        protected override void OnPause()
        {
            base.OnPause(); //just stop getting sensor data to save battery            
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            // We don't want to do anything here.
        }

        public void OnSensorChanged(SensorEvent e)
        {

            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffff");

            if (e.Sensor.Type == SensorType.Accelerometer)
            {
                lock (_syncLock)
                {
                    _xAccelerometer.Text = string.Format("{0:f5}", e.Values[0]);
                    _yAccelerometer.Text = string.Format("{0:f5}", e.Values[1]);
                    _zAccelerometer.Text = string.Format("{0:f5}", e.Values[2]);
                    string line = string.Format("{0};{1:f6};{2:f6};{3:f6}", timestamp, e.Values[0], e.Values[1], e.Values[2]);
                    accelerometerWriter.WriteLine(line);
                }
            }

            if (e.Sensor.Type == SensorType.MagneticField)
            {
                lock (_syncLock)
                {
                    _xMagnetometer.Text = string.Format("{0:f5}", e.Values[0]);
                    _yMagnetometer.Text = string.Format("{0:f5}", e.Values[1]);
                    _zMagnetometer.Text = string.Format("{0:f5}", e.Values[2]);
                    string line = string.Format("{0};{1:f6};{2:f6};{3:f6}", timestamp, e.Values[0], e.Values[1], e.Values[2]);
                    magnetometerWriter.WriteLine(line);
                }
            }

            if (e.Sensor.Type == SensorType.Gyroscope)
            {
                lock (_syncLock)
                {
                    _xGyroscope.Text = string.Format("{0:f5}", e.Values[0]);
                    _yGyroscope.Text = string.Format("{0:f5}", e.Values[1]);
                    _zGyroscope.Text = string.Format("{0:f5}", e.Values[2]);
                    string line = string.Format("{0};{1:f6};{2:f6};{3:f6}", timestamp, e.Values[0], e.Values[1], e.Values[2]);
                    gyroscopeWriter.WriteLine(line);
                }
            }

        }

    }
}

