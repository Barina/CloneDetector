using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace CloneDetector
{
    /// <summary>
    /// Interaction logic for GrowableButton.xaml
    /// </summary>
    public partial class GrowableButton : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="BracketsThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BracketsThicknessProperty =
            DependencyProperty.Register("BracketsThickness",
                typeof(double),
                typeof(GrowableButton),
                new PropertyMetadata(0.0, OnBracketsThicknessPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="BracketsWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BracketsWidthProperty =
            DependencyProperty.Register("BracketsWidth",
                typeof(double),
                typeof(GrowableButton),
                new PropertyMetadata(1.0));

        /// <summary>
        /// Identifies the <see cref="Spacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register("Spacing",
                typeof(double),
                typeof(GrowableButton),
                new PropertyMetadata(2.0, OnSpacingPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="DesiredWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DesiredWidthProperty =
            DependencyProperty.Register("DesiredWidth",
                typeof(double),
                typeof(GrowableButton),
                new PropertyMetadata(35.0));

        /// <summary>
        /// Identifies the <see cref="Prefix"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.Register("Prefix",
                typeof(string),
                typeof(GrowableButton),
                new PropertyMetadata("-"));

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text",
                typeof(string),
                typeof(GrowableButton),
                new PropertyMetadata());

        private static void OnBracketsThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            (d as GrowableButton)?.UpdateBracketsThickness();

        private static void OnSpacingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            (d as GrowableButton)?.UpdateSpacing();

        /// <summary>
        /// Gets or sets the thickness of the brackets.
        /// </summary>
        public double BracketsThickness
        {
            get => (double)GetValue(BracketsThicknessProperty);
            set => SetValue(BracketsThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the brackets.
        /// </summary>
        public double BracketsWidth
        {
            get => (double)GetValue(BracketsWidthProperty);
            set => SetValue(BracketsWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the space between the elements inside this control.
        /// </summary>
        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the control when fully grown.
        /// </summary>
        public double DesiredWidth
        {
            get => (double)GetValue(DesiredWidthProperty);
            set => SetValue(DesiredWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the prefix \ icon of this control. this text will always appear.
        /// </summary>
        public string Prefix
        {
            get => (string)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        /// <summary>
        /// Gets or sets the text to show when this control is fully grown.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private event RoutedEventHandler click;
        /// <summary>
        /// Occurs when the control is clicked.
        /// </summary>
        public event RoutedEventHandler Click
        {
            add => click += value;
            remove => click -= value;
        }

        public GrowableButton()
        {
            InitializeComponent();
            click = new RoutedEventHandler((s, e) => { });
            MouseEnter += GrowableButton_MouseEnter;
            MouseLeave += GrowableButton_MouseLeave;
            Loaded += (s, e) =>
            {
                if (!DesignerProperties.GetIsInDesignMode(this))
                    ShrinkButton();
            };
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private void Control_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) =>
#pragma warning restore IDE0060 // Remove unused parameter
            VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", true);

        private void Control_MouseDown(object sender, MouseButtonEventArgs e) =>
            VisualStateManager.GoToState(this, "Pressed", true);

        private void Control_MouseUp(object sender, MouseButtonEventArgs e)
        {
            VisualStateManager.GoToState(this, "MouseOver", true);
            click(this, EventArgs.Empty as RoutedEventArgs);
        }

        private void Control_MouseEnter(object sender, MouseEventArgs e) =>
            VisualStateManager.GoToState(this, IsEnabled ? "MouseOver" : "Disabled", true);

        private void Control_MouseLeave(object sender, MouseEventArgs e) =>
            VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", true);

        private void GrowableButton_MouseEnter(object sender, MouseEventArgs e) => GrowButton();
        private void GrowableButton_MouseLeave(object sender, MouseEventArgs e) => ShrinkButton();

        /// <summary>
        /// Start the shrink anumation of this button.
        /// </summary>
        private void ShrinkButton()
        {
            // set the ease and duration values
            var ease = new QuarticEase() { EasingMode = EasingMode.EaseOut };
            TimeSpan duration = TimeSpan.FromMilliseconds(500);

            // create the animations
            ThicknessAnimation marginAnim = new ThicknessAnimation(new Thickness(), duration) { EasingFunction = ease };
            DoubleAnimation widthAnim = new DoubleAnimation(0.0, duration) { EasingFunction = ease };

            // start the animations
            contentBorder.BeginAnimation(MarginProperty, marginAnim);
            contentBorder.BeginAnimation(WidthProperty, widthAnim);
        }

        /// <summary>
        /// Start the grow animation of this button.
        /// </summary>
        private void GrowButton()
        {
            // set the ease and duration values
            var ease = new QuarticEase() { EasingMode = EasingMode.EaseOut };
            TimeSpan duration = TimeSpan.FromMilliseconds(500);

            // create the animations
            ThicknessAnimation marginAnim = new ThicknessAnimation(new Thickness(0, 0, Spacing, 0), duration) { EasingFunction = ease };
            DoubleAnimation widthAnim = new DoubleAnimation(DesiredWidth, duration) { EasingFunction = ease };

            // start the animations
            contentBorder.BeginAnimation(MarginProperty, marginAnim);
            contentBorder.BeginAnimation(WidthProperty, widthAnim);
        }

        /// <summary>
        /// Refresh the brackets thickness.
        /// </summary>
        private void UpdateBracketsThickness()
        {
            openingBracket.BorderThickness = new Thickness(BracketsThickness) { Right = 0 };
            closingBracket.BorderThickness = new Thickness(BracketsThickness) { Left = 0 };
        }

        /// <summary>
        /// Refresh the spacing between elements.
        /// </summary>
        private void UpdateSpacing()
        {
            prefixTextBlock.Margin = new Thickness(Spacing, 0, Spacing, 0);
            contentBorder.Padding = new Thickness(0) { Right = Spacing };
        }
    }
}