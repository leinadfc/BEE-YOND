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
    /// <summary>
    /// Input data to graphs class. ValueActivity (y-axis value), DateTimeActivity (x-axis value)
    /// </summary>
    public class MeasureModel
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
    }

    public partial class WeightControl : UserControl, INotifyPropertyChanged
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

        public WeightControl()
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
            //this code ideally SHOULD ONLY RUN ONCE
            //you can configure series in many ways, learn more at 
            //http://lvcharts.net/App/examples/v1/wpf/Types%20and%20Configuration

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)   // x-axis values use DateTime.Ticks as X  1Tick = 100nseconds number of ticks is the number of 100ns intervals that have happened since 1 January at 00:00
                .Y(model => model.Value);           //y-axis values use the value property as Y

            //lets save the mapper globally, we can use it elswhere.
            Charting.For<MeasureModel>(mapper);

            //the values property will store our values array, here we also input the previous values in time
            ChartValues = new ChartValues<MeasureModel>();
            

            //lets set how to display the X LABELS, datetimeformatter Func of <double, string> Encapsulates a method that has one parameter and returns a value of the type specified by the TResult parameter.
            DateTimeFormatter = value => new DateTime((long)value).ToString("mm:ss");

            //AxisStep forces the distance between each separator in the X axis. X AXIS GAP
            AxisStep = TimeSpan.FromSeconds(40).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            //Limit of the graph is current value, so we see current + past values
            SetAxisLimits(DateTime.Now);

            //The next code simulates data changes every 300 ms
            //See isreading bool below

            //IsReading = true;

            //See Read

            Task.Factory.StartNew(export);

            YFormatter = value => value + value.ToString(" kg");

            DataContext = this;
        }

        //Saying now charvalues is not int our double, it follows our own structure

        public ChartValues<MeasureModel> ChartValues { get; set; }
        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }
        public Func<double, string> YFormatter { get; set; }

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

        //  public bool IsReading { get; set; }

        /*private void Read()
        {
            var r = new Random();

            while (IsReading)
            {
                //Suspends the current thread for the specified amount of time
                Thread.Sleep(2000);

                var now = DateTime.Now;

                //Random integer in the range of 40 and 60
                _trend = r.Next(40,60);

                ChartValues.Add(new MeasureModel
                {
                    DateTime = now,
                    Value = _trend
                });

                //Move axis right limit to current time
                SetAxisLimits(now);

                //lets only use the last 150 values, deletes the value 151 all the time
                if (ChartValues.Count > 1500) ChartValues.RemoveAt(0);
            }
        }*/

        

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            
            //This allows us to change timescale number of seconds we can have behind 
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

        /// <summary>
        /// Data retrieveing section
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        
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

                        double Weight = getweight(obj2.DataPackage);
                        ChartValues.Add(new MeasureModel
                        {
                            DateTime = System.DateTime.Now.AddSeconds(5*i-5*cnt),
                            Value = Weight
                        });
                        //Move axis right limit to current time
                        SetAxisLimits(DateTime.Now);
                        if (ChartValues.Count > 1500) ChartValues.RemoveAt(0);

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
            for (int i = 0; i < 3; i++)
            {
                WeightString += datastring[i];
            }
            WeightDouble = Convert.ToDouble(WeightString);
            return WeightDouble / 10;
        }

    }
}