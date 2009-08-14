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
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;

namespace DevHawk.Windows.Controls
{
    public class SyntaxItem
    {
        public TokenCategory TokenCategory { get; set; }
        public Color Color { get; set; }

        public SyntaxItem()
        {
        }

        public SyntaxItem(TokenCategory category, Color color)
        {
            TokenCategory = category;
            Color = color;
        }
    }

    public class SyntaxItemCollection : List<SyntaxItem>
    {
    }

    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:DevHawk.Windows.Controls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:DevHawk.Windows.Controls;assembly=HawkCodeBox"
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

            //Since this is a text box for code, the default settings for a few of the 
            //properties should be different (fixed width font, accepts return and tab)
            Control.FontFamilyProperty.OverrideMetadata(typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(new FontFamily("Consolas")));
            TextBoxBase.AcceptsReturnProperty.OverrideMetadata(typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(true));
            TextBoxBase.AcceptsTabProperty.OverrideMetadata(typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(true));
        }

        //Foreground must be the transparent brush for HawkCodeBox to work. If someone tries to set it
        //To something other than Transparent, this function changes it back
        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != Brushes.Transparent)
            {
                ((HawkCodeBox)d).Foreground = Brushes.Transparent;
            }
        }

        //Like Foreground, Background must also be transparent. if someone tries to change it, this function 
        //changes it back. However, decorations like the caret and text selection box base their color on the 
        //Background property. Thus, we set it to a transparent version of the background color, provided by 
        //the TransparentBackgroundColor property on HawkCodeBox
        private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var codebox = (HawkCodeBox)d;
            var bgbrush = e.NewValue as SolidColorBrush;
            if (bgbrush == null || bgbrush.Color != codebox.TransparentBackgroundColor)
            {
                codebox.Background = new SolidColorBrush(codebox.TransparentBackgroundColor);
            }
        }

        public HawkCodeBox()
        {
            //TODO: track where the text has changed so the control doesn't have to re-colorize the entire buffer
            this.TextChanged += (s, e) => { this.InvalidateVisual(); };
            this.Loaded += (s, e) =>
                {
                    var c = VisualTreeHelper.GetChild(this, 0);
                    var sv = VisualTreeHelper.GetChild(c, 0) as ScrollViewer;
                    sv.ScrollChanged += (s2, e2) => { this.InvalidateVisual(); };
                };
        }

        //Using a DependencyProperty to manage the default foreground color for the text in the text box
        public static readonly DependencyProperty DefaultForegroundColorProperty =
            DependencyProperty.Register("ForegroundColor", typeof(Color), typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public Color DefaultForegroundColor
        {
            get { return (Color)GetValue(DefaultForegroundColorProperty); }
            set { SetValue(DefaultForegroundColorProperty, value); }
        }

        //using a DependencyProperty to manage the background color for the text box
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Color), typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnBackgroundColorChanged));

        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        //If the background color changes, we need to reset the background to a new transparent brush
        static void OnBackgroundColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var codebox = (HawkCodeBox)obj;
            codebox.Background = new SolidColorBrush(codebox.TransparentBackgroundColor);
        }

        //Using a DependencyProperty to store the DLR language name used to colorize the text
        public static readonly DependencyProperty DlrLanguageProperty =
            DependencyProperty.Register("DlrLanguage", typeof(string), typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public string DlrLanguage
        {
            get { return (string)GetValue(DlrLanguageProperty); }
            set { SetValue(DlrLanguageProperty, value); }
        }
        
        //helper property to provide a transparent version of the current Background color
        private Color TransparentBackgroundColor
        {
            get
            {
                return Color.FromArgb(0, BackgroundColor.R, BackgroundColor.G, BackgroundColor.B);
            }
        }

        //helper property to retrieve the engine for the current language
        ScriptRuntime _runtime;
        private ScriptEngine Engine
        {
            get
            {
                if (_runtime == null)
                {
                    var setup = ScriptRuntimeSetup.ReadConfiguration();
                    _runtime = new ScriptRuntime(setup);
                }
                return _runtime.GetEngine(this.DlrLanguage);
            }
        }

        public SyntaxItemCollection SyntaxColors
        {
            get { return (SyntaxItemCollection)GetValue(SyntaxColorsProperty); }
            set { SetValue(SyntaxColorsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SyntaxColorsProperty =
            DependencyProperty.Register("SyntaxColors", typeof(SyntaxItemCollection), typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(new SyntaxItemCollection(), OnSyntaxColorsChanged));

        Dictionary<TokenCategory, Brush> _map;

        static void OnSyntaxColorsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((HawkCodeBox)obj)._map = null;
        }

        IDictionary<TokenCategory, Brush> _SyntaxMap
        {
            get
            {
                if (_map == null)
                {
                    _map = new Dictionary<TokenCategory, Brush>();

                    if (SyntaxColors.Count == 0)
                    {
                        _map[TokenCategory.NumericLiteral] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xEE, 0x98));
                        _map[TokenCategory.Keyword] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x66, 0x00));
                        _map[TokenCategory.Identifier] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xCC, 0x00));
                        _map[TokenCategory.StringLiteral] = new SolidColorBrush(Color.FromArgb(0xFF, 0x66, 0xFF, 0x00));
                        _map[TokenCategory.Comment] = new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0x33, 0xCC));
                        _map[TokenCategory.LineComment] = new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0x33, 0xCC));
                        _map[TokenCategory.Error] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00));
                    }
                    else
                    {
                        foreach (var s in SyntaxColors)
                        {
                            _map[s.TokenCategory] = new SolidColorBrush(s.Color);
                        }
                    }
                }

                return _map;
            }
        }

        //Render the text in the text box
        protected override void OnRender(DrawingContext dc)
        {
            var ft = new FormattedText(
                this.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(this.FontFamily.Source),
                this.FontSize,
                new SolidColorBrush(this.DefaultForegroundColor));

            var left_margin = 4.0 + this.BorderThickness.Left;
            var top_margin = 2.0 + this.BorderThickness.Top;

            dc.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));
            dc.DrawRectangle(new SolidColorBrush(this.BackgroundColor), new Pen(), new Rect(0, 0, this.ActualWidth, this.ActualHeight));

            //at design time, we won't have access to any DLR language assemblies, so don't try to colorize
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                var source = Engine.CreateScriptSourceFromString(this.Text);
                var tokenizer = Engine.GetService<TokenCategorizer>();
                tokenizer.Initialize(null, source, SourceLocation.MinValue);
                while (true)
                {
                    var t = tokenizer.ReadToken();
                    if (t.Category == TokenCategory.EndOfStream)
                        break;

                    if (_SyntaxMap.ContainsKey(t.Category))
                    {
                        ft.SetForegroundBrush(_map[t.Category], t.SourceSpan.Start.Index, t.SourceSpan.Length);
                    }
                }
            }

            dc.DrawText(ft, new Point(left_margin - this.HorizontalOffset, top_margin - this.VerticalOffset));
        }        
    }
}
