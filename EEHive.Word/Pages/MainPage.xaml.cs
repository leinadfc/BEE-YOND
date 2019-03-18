using EEHive.Word.Core;
using System.Text;
using FireSharp.Config;
using FireSharp.Response;
using System.Threading.Tasks;
using System;
using System.Threading;
using FireSharp.Interfaces;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EEHive.Word
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : BasePage<LoginViewModel>
    {
        /// <summary>
        /// Linking program to FIREBASE project
        /// </summary>
        IFirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = "tkNtmJt1F8agsqnieiILiTh5FCgBkiMeVQoQXX3Q",
            BasePath = "https://bee-hive-database.firebaseio.com/"
        };

        IFirebaseClient client;

        public MainPage()
        {
            InitializeComponent();
            client = new FireSharp.FirebaseClient(config);
            Task.Factory.StartNew(export);
        }
        public async void export()
        {
            int i = 0;
            while (true)
            {
                Thread.Sleep(1000);

               
                FirebaseResponse resp1 = await client.GetTaskAsync("Counter/Node");
                CounterClass obj1 = resp1.ResultAs<CounterClass>();
                int cnt = Convert.ToInt32(obj1.cnt);
                

                while (true)
                {
                    if (i == cnt)
                    {
                        break;
                    }
                    i++;
                    try
                    {
                        FirebaseResponse resp2 = await client.GetTaskAsync("Data/" + i);
                        Data obj2 = resp2.ResultAs<Data>();
                        string DayWeek = getdayWeek(obj2.DataPackage);
                        string Day = Getday(obj2.DataPackage);
                        string Month = GetMonth(obj2.DataPackage);
                        string Year = GetYear(obj2.DataPackage);
                        string Hour = GetHour(obj2.DataPackage);
                        string Minute = GetMinute(obj2.DataPackage);
                        string Second = GetSecond(obj2.DataPackage);
                        UpdateText(DayWeek, Day, Month, Year, Hour, Minute, Second);
                    }
                    catch
                    {

                    }
                }
            }
        }

        /// <summary>
        /// Converts the portion of strin grelated to Weight to a double
        /// </summary>
        /// <param name="datastring"></param>

        private string getdayWeek(string datastring)
        {
            string DayWeek = null;
            string Day = null;
            for (int i = 0; i < 1; i++)
            {
                DayWeek += datastring[i];
            }
            if (DayWeek == "1")
            {
                Day = "Sunday";
            }
            if (DayWeek == "2")
            {
                Day = "Monday";
            }
            if (DayWeek == "3")
            {
                Day = "Tuesday";
            }
            if (DayWeek == "4")
            {
                Day = "Wednesday";
            }
            if (DayWeek == "5")
            {
                Day = "Thursday";
            }
            if (DayWeek == "6")
            {
                Day = "Friday";
            }
            if (DayWeek == "7")
            {
                Day = "Saturday";
            }

            return Day;
        }

        private string Getday (string datastring)
        {
            string Day = null;
            for (int i = 7; i < 9; i++)
            {
                Day += datastring[i];
            }
            return Day;
        }

        private string GetMonth(string datastring)
        {
            string Month = null;
            for (int i = 5; i < 7; i++)
            {
                Month += datastring[i];
            }
            return Month;
        }

        private string GetYear(string datastring)
        {
            string Year = null;
            for (int i = 1; i < 5; i++)
            {
                Year += datastring[i];
            }
            return Year;
        }

        private string GetHour(string datastring)
        {
            string Hour = null;
            for (int i = 9; i < 11; i++)
            {
                Hour += datastring[i];
            }
            return Hour;
        }

        private string GetMinute(string datastring)
        {
            string Minute = null;
            for (int i = 11; i < 13; i++)
            {
                Minute += datastring[i];
            }
            return Minute;
        }

        private string GetSecond(string datastring)
        {
            string Second = null;
            for (int i = 13; i < 15; i++)
            {
                Second += datastring[i];
            }
            return Second;
        }


        private void UpdateText (string DayWeek, string Day, string Month, string Year, string Hour, string Minute, string Second) {

            LastUpdated.Dispatcher.BeginInvoke(
                 new Action(() => LastUpdated.Text = $"Last measurement: {DayWeek} - {Day}/{Month}/{Year} - {Hour}:{Minute}:{Second}"));
        }

    }
}



