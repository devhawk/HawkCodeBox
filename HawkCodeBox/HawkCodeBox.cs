using System;
using System.Collections.Generic;
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
using System.Windows.Controls.Primitives;
using System.Globalization;

namespace DevHawk.WPF
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:DevHawk.WPF"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:DevHawk.WPF;assembly=DevHawk.WPF"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:HawkCodeBox/>
    ///
    /// </summary>
    public class HawkCodeBox : TextBox
    {
        static HawkCodeBox()
        {
            Control.ForegroundProperty.OverrideMetadata(typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(Brushes.Transparent, OnForegroundChanged));
            Control.BackgroundProperty.OverrideMetadata(typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(OnBackgroundChanged));

            TextBoxBase.AcceptsReturnProperty.OverrideMetadata(typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(true));
            TextBoxBase.AcceptsTabProperty.OverrideMetadata(typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(true));
        }

        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var codebox = (HawkCodeBox)d;
            if (codebox.Foreground != Brushes.Transparent)
                codebox.Foreground = Brushes.Transparent;
        }

        private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var codebox = (HawkCodeBox)d;
            var bgbrush = codebox.Background as SolidColorBrush;
            if (bgbrush == null || bgbrush.Color != codebox.TransparentBackgroundColor)
                codebox.Background = new SolidColorBrush(codebox.TransparentBackgroundColor);
        }

        public Color ForegroundColor
        {
            get { return (Color)GetValue(ForegroundColorProperty); }
            set { SetValue(ForegroundColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ForegroundColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundColorProperty =
            DependencyProperty.Register("ForegroundColor", typeof(Color), typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.AffectsRender));

        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Color), typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnBackgroundColorChanged));

        static void OnBackgroundColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var codebox = (HawkCodeBox)obj;
            codebox.Background = new SolidColorBrush(codebox.TransparentBackgroundColor);
        }

        private Color TransparentBackgroundColor
        {
            get
            {
                return Color.FromArgb(0, BackgroundColor.R, BackgroundColor.G, BackgroundColor.B);
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            var ft = new FormattedText(
                this.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(this.FontFamily.Source),
                this.FontSize,
                new SolidColorBrush(this.ForegroundColor));

            var left_margin = 4.0 + this.BorderThickness.Left;
            var top_margin = 2.0 + this.BorderThickness.Top;

            dc.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));

            dc.DrawRectangle(new SolidColorBrush(this.BackgroundColor), new Pen(), new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            dc.DrawText(ft, new Point(left_margin - this.HorizontalOffset, top_margin - this.VerticalOffset));
        }        
    }
}
