using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Helpers;
using Helpers.WPF;

namespace Helpers.Controls
{
    /// <summary>
    /// Interaction logic for WaitProgress.xaml
    /// </summary>
    public partial class WaitProgress : UserControl, INotifyPropertyChanged
    {
        #region Propertyes

        internal const bool PercentageFractionalPartVisibleDefaultValue = true;
        internal const bool PercentageVisibleDefaultValue = true;
        internal const double PercentageDefaultValue = 0.0;
        internal static readonly System.Windows.Media.Color ColorDefaultValue = System.Windows.Media.Colors.Black;

        /// <summary>
        /// Show percentage
        /// </summary>
        public static readonly DependencyProperty PercentageVisibleProperty =
            DependencyProperty.Register("PercentageVisible", PercentageVisibleDefaultValue.GetType(), typeof(WaitProgress), new FrameworkPropertyMetadata(PercentageVisibleDefaultValue, new PropertyChangedCallback(MyCustom_PropertyChanged)));

        /// <summary>
        /// Show percentagge fractional part
        /// </summary>
        public static readonly DependencyProperty PercentageFractionalPartVisibleProperty =
            DependencyProperty.Register("PercentageFractionalPartVisible", PercentageFractionalPartVisibleDefaultValue.GetType(), typeof(WaitProgress), new FrameworkPropertyMetadata(PercentageFractionalPartVisibleDefaultValue, new PropertyChangedCallback(MyCustom_PropertyChanged)));

        /// <summary>
        /// Percentage value to show
        /// </summary>
        public static readonly DependencyProperty PercentageProperty =
            DependencyProperty.Register("Percentage", PercentageDefaultValue.GetType(), typeof(WaitProgress), new FrameworkPropertyMetadata(PercentageDefaultValue, new PropertyChangedCallback(MyCustom_PropertyChanged)));

        /// <summary>
        /// Color
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", ColorDefaultValue.GetType(), typeof(WaitProgress), new FrameworkPropertyMetadata(ColorDefaultValue, new PropertyChangedCallback(MyCustom_PropertyChanged)));

        private static void MyCustom_PropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            WaitProgress p = dobj as WaitProgress;
            if (p != null)
            {
                p.RaisePropertyChanged(e.Property.Name);
            }
        }

        /// <summary>
        /// Show percentage
        /// </summary>
        public bool PercentageVisible
        {
            get { return (bool)this.GetValue(PercentageVisibleProperty); }
            set { this.SetValue(PercentageVisibleProperty, value); }
        }

        /// <summary>
        /// Show percentagge fractional part
        /// </summary>
        public bool PercentageFractionalPartVisible
        {
            get { return (bool)this.GetValue(PercentageFractionalPartVisibleProperty); }
            set { this.SetValue(PercentageFractionalPartVisibleProperty, value); }
        }

        /// <summary>
        /// Percentage value to show
        /// </summary>
        public double Percentage
        {
            get { return (double)this.GetValue(PercentageProperty); }
            set { this.SetValue(PercentageProperty, value); }
        }

        /// <summary>
        /// Color
        /// </summary>
        public System.Windows.Media.Color Color
        {
            get { return (System.Windows.Media.Color)this.GetValue(ColorProperty); }
            set { this.SetValue(ColorProperty, value); }
        }

        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        public WaitProgress()
        {
            InitializeComponent();
            if (this.IsDesignMode())
                Color = ColorDefaultValue;
        }

        #region INotifyPropertyChanged

        /// <summary>
        /// Raise that property is changed for INotifyPropertyChanged interface
        /// </summary>
        /// <param name="propertyName">Property name</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Represents the method that will handle the System.ComponentModel.INotifyPropertyChanged.PropertyChanged event raised when a property is changed on a component.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion;
    }
}
