using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Configurations;

namespace EEHive.Word
{
    public class MeasureModelExteriorTemperature
    {
        public DateTime DateTimeExteriorTemperature { get; set; }
        public double ValueExteriorTemperature { get; set; }
    }

    public partial class ExteriorTemperatureControl : UserControl, INotifyPropertyChanged
    {
        private double _axisMax;
        private double _axisMin;
        private double _trend;

        public ExteriorTemperatureControl()
        {
            InitializeComponent();

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

            var mapper = Mappers.Xy<MeasureModelExteriorTemperature>()
                .X(model => model.DateTimeExteriorTemperature.Ticks)   //use DateTime.Ticks as X
                .Y(model => model.ValueExteriorTemperature);           //use the value property as Y

            //lets save the mapper globally.
            Charting.For<MeasureModelExteriorTemperature>(mapper);

            //the values property will store our values array
            ChartValuesExteriorTemperature = new ChartValues<MeasureModelExteriorTemperature>();

            //lets set how to display the X Labels
            DateTimeFormatterExteriorTemperature = value => new DateTime((long)value).ToString("mm:ss");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(4).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            SetAxisLimits(DateTime.Now);

            //The next code simulates data changes every 300 ms

            IsReading = true;

            Task.Factory.StartNew(Read);

            YFormatterExteriorTemperature = valueExteriorTemperature => valueExteriorTemperature + valueExteriorTemperature.ToString(" %");

            DataContext = this;
        }

        public ChartValues<MeasureModelExteriorTemperature> ChartValuesExteriorTemperature { get; set; }
        public Func<double, string> DateTimeFormatterExteriorTemperature { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }
        public Func<double, string> YFormatterExteriorTemperature { get; set; }

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

        public bool IsReading { get; set; }

        private void Read()
        {
            var r = new Random();

            while (IsReading)
            {
                Thread.Sleep(2000);
                var now = DateTime.Now;

                _trend = r.Next(40, 60);

                ChartValuesExteriorTemperature.Add(new MeasureModelExteriorTemperature
                {
                    DateTimeExteriorTemperature = now,
                    ValueExteriorTemperature = _trend
                });

                SetAxisLimits(now);

                //lets only use the last 150 values
                if (ChartValuesExteriorTemperature.Count > 150) ChartValuesExteriorTemperature.RemoveAt(0);
            }
        }

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(20).Ticks; // and 20 seconds behind
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