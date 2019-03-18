using FireSharp.Config;
using FireSharp.Response;
using FireSharp.Interfaces;
using System.Data;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Configurations;

namespace EEHive.Word
{
    public class MeasureModelHumidity
    {
        public DateTime DateTimeHumidity { get; set; }
        public double ValueHumidity { get; set; }
    }

    public partial class HumidityControl : UserControl, INotifyPropertyChanged
    {
        private double _axisMax;
        private double _axisMin;

        /// <summary>
        /// Linking program to FIREBASE project
        /// </summary>
        IFirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = "tkNtmJt1F8agsqnieiILiTh5FCgBkiMeVQoQXX3Q",
            BasePath = "https://bee-hive-database.firebaseio.com/"
        };

        IFirebaseClient client;

        public HumidityControl()
        {
            InitializeComponent();

            //Creating FIRESHARP client
            client = new FireSharp.FirebaseClient(config);

            //To handle live data easily, in this case we built a specialized type
            //the MeasureModel class, it only contains 2 properties
            //DateTime and Value
            //We need to configure LiveCharts to handle MeasureModel class
            //The next code configures MeasureModel  globally, this means
            //that LiveCharts learns to plot MeasureModel and will use this config every time
            //a IChartValues instance uses this type.
            //this code ideally should only run once
            //you can configure series in many ways, learn more at 
            //http://lvcharts.net/App/examples/v1/wpf/Types%20and%20Configuration

            var mapper = Mappers.Xy<MeasureModelHumidity>()
                .X(model => model.DateTimeHumidity.Ticks)   //use DateTime.Ticks as X
                .Y(model => model.ValueHumidity);           //use the value property as Y

            //lets save the mapper globally.
            Charting.For<MeasureModelHumidity>(mapper);

            //the values property will store our values array
            ChartValuesHumidity = new ChartValues<MeasureModelHumidity>();

            //lets set how to display the X Labels
            DateTimeFormatterHumidity = value => new DateTime((long)value).ToString("mm:ss");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(40).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            SetAxisLimits(DateTime.Now);

            //The next code simulates data changes every 300 ms


            Task.Factory.StartNew(export);

            YFormatterHumidity = valueHumidity => valueHumidity + valueHumidity.ToString(" %");

            DataContext = this;
        }

        public ChartValues<MeasureModelHumidity> ChartValuesHumidity { get; set; }
        public Func<double, string> DateTimeFormatterHumidity { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }
        public Func<double, string> YFormatterHumidity { get; set; }

        public double AxisMax
        {
            get { return _axisMax; }
            set
            {
                _axisMax = value;
                OnPropertyChanged("AxisMax");
            }
        }
        public double AxisMin
        {
            get { return _axisMin; }
            set
            {
                _axisMin = value;
                OnPropertyChanged("AxisMin");
            }
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
                        if (resp2 != null)
                        {
                            Data obj2 = resp2.ResultAs<Data>();

                            double Weight = getweight(obj2.DataPackage);
                            ChartValuesHumidity.Add(new MeasureModelHumidity
                            {
                                DateTimeHumidity = System.DateTime.Now.AddSeconds(5 * i - 5 * cnt),
                                ValueHumidity = Weight
                            });
                            //Move axis right limit to current time
                            SetAxisLimits(DateTime.Now);
                            if (ChartValuesHumidity.Count > 1500) ChartValuesHumidity.RemoveAt(0);
                        }

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

        private double getweight(string datastring)
        {
            string WeightString = null;
            double WeightDouble;
            for (int i = 3; i < 6; i++)
            {
                WeightString += datastring[i];
            }
            WeightDouble = Convert.ToDouble(WeightString);
            return WeightDouble / 10;
        }
        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(200).Ticks; // and 20 seconds behind
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}