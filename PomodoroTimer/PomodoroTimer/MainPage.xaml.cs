using System;
using System.IO;
using Windows.UI.Notifications;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Data.Xml.Dom;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PomodoroTimer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    ///
    public sealed partial class MainPage : Page
    {
        private enum Interval
        {
            Task = 2,
            ShortRest = 1,
            LongRest = 6
        };

        private int _restsTaken { get; set; }
        private int _currentTime { get; set; }
        private DispatcherTimer _timer { get; set; }
        private TimeSpan _tickInterval { get; set; }
        private TimeSpan _intervalRemainingTime { get; set; }
        private DateTime _intervalEnd { get; set; }
        private bool _isRunning = false;

        public MainPage()
        {
            this.InitializeComponent();
            _restsTaken = 0;
            _currentTime = 0;
            _tickInterval = TimeSpan.FromSeconds(1);
            this.initializeTimer(_tickInterval.Seconds);
            this.initializeDisplayTimer(0);
            UpdateTile("Please Start!");
            Application.Current.Resuming += new EventHandler<Object>(App_Resuming);
        }

        private void initializeTimer(int tickInterval)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(tickInterval);
            _timer.Tick += interval_Tick;
        }

        private void initializeDisplayTimer(int intervalTime)
        {
            _intervalRemainingTime = TimeSpan.FromMinutes(intervalTime);
            timerLabel.Text = _intervalRemainingTime.ToString();
        }

        private void interval_Tick(object sender, object e)
        {

            int previousTimeInMinutes = _intervalRemainingTime.Minutes;
            _isRunning = true;
            _intervalRemainingTime = _intervalRemainingTime.Subtract(_tickInterval);
            timerLabel.Text = _intervalRemainingTime.ToString();

            if (previousTimeInMinutes != _intervalRemainingTime.Minutes)
            {
                string timeIndicator = _intervalRemainingTime.Minutes == 0 ? "1 >" : _intervalRemainingTime.Minutes.ToString();
                UpdateTile(timeIndicator + " minute(s) left");
            }
            if (TimeSpan.Equals(_intervalRemainingTime, TimeSpan.Zero))
            {
                _timer.Stop();
                timerLabel.Text = "Time´s up";
                UpdateTile("Time´s up");
                
            }
        }

        private void clearNotificationQueue()
        {
           var scheduledNotifications =  ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications();

            foreach (var item in scheduledNotifications)
            {
                ToastNotificationManager.CreateToastNotifier().RemoveFromSchedule(item);
            }

        }

       

        private void Button_Click_Start(object sender, RoutedEventArgs e)
        {
            if (TimeSpan.Equals(_intervalRemainingTime, TimeSpan.Zero))
            {
                _currentTime = (int)Interval.Task;
                this.initializeDisplayTimer(_currentTime);
            }

            _intervalEnd = DateTime.Now.Add(_intervalRemainingTime);

            if (_currentTime == (int)Interval.LongRest || _currentTime == (int)Interval.ShortRest)
            {
                ScheduleNotification("Rest´s up", "Now, it´s time to go back to the task");
            }
            else
            {
                ScheduleNotification("Time´s up", "Now, it´s time to take a rest");
            }

            _timer.Start();
        }

        private void Button_Click_Stop(object sender, RoutedEventArgs e)
        {
            _isRunning = false;
            _timer.Stop();
            clearNotificationQueue();
        }

        private void Button_Click_Reset(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            clearNotificationQueue();
            this.initializeDisplayTimer(_currentTime);
        }

        private void Button_Click_Rest(object sender, RoutedEventArgs e)
        {
            _restsTaken++;
            int longRestTime = 0;
            if (_restsTaken == 3)
            {
                longRestTime = (int)Interval.LongRest;
                _restsTaken = 0;
            }
            else
            {
                longRestTime = (int)Interval.ShortRest;
            }

            _currentTime = longRestTime;
            this.initializeDisplayTimer(longRestTime);
            _intervalEnd = DateTime.Now.Add(TimeSpan.FromMinutes(_currentTime));
            ScheduleNotification("Rest´s up", "Now, it´s time to go back to the task");
            _timer.Start();
        }

        private void ScheduleNotification(string title, string description)
        {
            var notificationDictionary = new Dictionary<string, string>()
            {
                { "{TITLE}", title },
                { "{DESCRIPTION}", description }
            };

            var notificationXML = LoadAndPopulateXMLFile("/Resources/Notification.xml", notificationDictionary);
            var toastScheduled = new ScheduledToastNotification(notificationXML, _intervalEnd);
            ToastNotificationManager.CreateToastNotifier().AddToSchedule(toastScheduled);
        }

        private void UpdateTile(string tileSign)
        {
            var tileDictionary = new Dictionary<string, string>()
            {
                { "Please Start!", tileSign }
            };

            var tile = new TileNotification(LoadAndPopulateXMLFile("/Resources/Tile.xml", tileDictionary));
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tile);
        }

        private XmlDocument LoadAndPopulateXMLFile(string fileNameWithExtension, Dictionary<string, string> dictionary)
        {
            var xmlDocument = new XmlDocument();
            var xmlFile = File.ReadAllText(Directory.GetCurrentDirectory() + fileNameWithExtension);
            foreach (var item in dictionary)
            {
                xmlFile = xmlFile.Replace(item.Key, item.Value);
            }

            xmlDocument.LoadXml(xmlFile);
            return xmlDocument;
        }

        private void App_Resuming(Object sender, Object e)
        {
            if (_isRunning) {

                if (_intervalEnd <= DateTime.Now)
                {
                    _timer.Stop();
                    timerLabel.Text = "Time`s up";
                }
                else
                {
                    TimeSpan diff = (_intervalEnd - DateTime.Now);
                    _intervalRemainingTime = new TimeSpan(0, diff.Minutes, diff.Seconds);
                    timerLabel.Text = _intervalRemainingTime.ToString();
                }
            }
            
        }

    }
}