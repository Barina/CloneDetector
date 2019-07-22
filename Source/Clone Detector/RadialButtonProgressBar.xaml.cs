using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CloneDetector
{
    /// <summary>
    /// Interaction logic for RadialButtonProgressBar.xaml
    /// </summary>
    public partial class RadialButtonProgressBar : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="Maximum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum",
                typeof(double),
                typeof(RadialButtonProgressBar),
                new PropertyMetadata(100.0, new PropertyChangedCallback(ValuePropertyChanged)));

        /// <summary>
        /// Identifies the <see cref="Minimum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum",
                typeof(double),
                typeof(RadialButtonProgressBar),
                new PropertyMetadata(0.0, new PropertyChangedCallback(ValuePropertyChanged)));

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value",
                typeof(double),
                typeof(RadialButtonProgressBar),
                new PropertyMetadata(0.0, new PropertyChangedCallback(ValuePropertyChanged)));

        /// <summary>
        /// Identifies the <see cref="IsWorking"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsWorkingProperty =
            DependencyProperty.Register("IsWorking",
                typeof(bool),
                typeof(RadialButtonProgressBar),
                new PropertyMetadata(false, WorkingPropertyChanged));

        private static void ValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            (d as RadialButtonProgressBar)?.UpdateProgressBarValue();

        private static void WorkingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            (d as RadialButtonProgressBar)?.UpdateProgressBar();

        /// <summary>
        /// Gets or sets the maximum value for the progress arc.
        /// </summary>
        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, Math.Max(value, Minimum));
        }

        /// <summary>
        /// Gets or sets the minimum value for the progress arc.
        /// </summary>
        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, Math.Min(value, MaxHeight));
        }

        /// <summary>
        /// Gets or sets the current progress arc value.
        /// </summary>
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Math.Max(Math.Min(value, Maximum), Minimum));
        }

        /// <summary>
        /// Gets or sets whether this button is in working state or not.
        /// </summary>
        public bool IsWorking
        {
            get => (bool)GetValue(IsWorkingProperty);
            set => SetValue(IsWorkingProperty, value);
        }

        private event RoutedEventHandler radialButtonClick = new RoutedEventHandler((s, e) => { });
        /// <summary>
        /// Occurs when this button is clicked.
        /// </summary>
        public event RoutedEventHandler RadialButtonClick
        {
            add => radialButtonClick += value;
            remove => radialButtonClick -= value;
        }

        public RadialButtonProgressBar() => InitializeComponent();

        /// <summary>
        /// Refresh the value of the progress arc.
        /// </summary>
        private void UpdateProgressBarValue()
        {
            var v = Value - Minimum;
            var max = Maximum - Minimum;
            var per = v / max;
            // calculate the appropriate angle from current values
            progressArc.EndAngle = 360 * per;
        }

        /// <summary>
        /// Refresh the progress arc.
        /// </summary>
        private void UpdateProgressBar()
        {
            // set the appropriate text to the button
            textBlock.Text = IsWorking ? "Stop" : "Start";
            // start the corresponding animation
            var anim = FindResource(IsWorking ? "WorkingAnim" : "ReadyAnim") as Storyboard;
            anim?.Begin();
        }

        private void Button_Click(object sender, RoutedEventArgs e) =>
            radialButtonClick(sender, e);
    }
}