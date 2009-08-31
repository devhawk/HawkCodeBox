using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace DevHawk.Windows.Controls
{
    public abstract class HawkCodeBoxBase : TextBox
    {
        static HawkCodeBoxBase()
        {
            //override the metadata of Foreground and Background properties so that 
            //the control can set them back to the correct value if they are changed.
            Control.ForegroundProperty.OverrideMetadata(typeof(HawkCodeBoxBase),
                new FrameworkPropertyMetadata(Brushes.Transparent, OnForegroundChanged));
            Control.BackgroundProperty.OverrideMetadata(typeof(HawkCodeBoxBase),
                new FrameworkPropertyMetadata(OnBackgroundChanged));

            //Since this is a text box for code, the default settings for a few of the 
            //properties should be different (fixed width font, accepts return and tab)
            Control.FontFamilyProperty.OverrideMetadata(typeof(HawkCodeBoxBase),
                new FrameworkPropertyMetadata(new FontFamily("Consolas")));
            TextBoxBase.AcceptsReturnProperty.OverrideMetadata(typeof(HawkCodeBoxBase),
                new FrameworkPropertyMetadata(true));
            TextBoxBase.AcceptsTabProperty.OverrideMetadata(typeof(HawkCodeBoxBase),
                new FrameworkPropertyMetadata(true));
        }

        //Foreground must be the transparent brush for HawkCodeBoxBase to work. If someone tries to set it
        //To something other than Transparent, this function changes it back
        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != Brushes.Transparent)
            {
                ((HawkCodeBoxBase)d).Foreground = Brushes.Transparent;
            }
        }

        //Like Foreground, Background must also be transparent. if someone tries to change it, this function 
        //changes it back. However, decorations like the caret and text selection box base their color on the 
        //Background property. Thus, set the background to a transparent version of the background color, 
        //provided by the TransparentBackgroundColor property on HawkCodeBoxBase
        private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var codebox = (HawkCodeBoxBase)d;
            var transparent_bg = codebox.GetTransparentBackgroundColor();
            var bgbrush = e.NewValue as SolidColorBrush;
            if (bgbrush == null || bgbrush.Color != transparent_bg)
            {
                codebox.Background = new SolidColorBrush(transparent_bg);
            }
        }

        public HawkCodeBoxBase()
        {
            this.Loaded += (s, e) =>
                {
                    var c = VisualTreeHelper.GetChild(this, 0);
                    var sv = VisualTreeHelper.GetChild(c, 0) as ScrollViewer;
                    sv.ScrollChanged += (s2, e2) => { this.InvalidateVisual(); };
                };
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            this.InvalidateVisual();
        }

        //Using a DependencyProperty to manage the default foreground color for the text in the text box
        public static readonly DependencyProperty DefaultForegroundColorProperty =
            DependencyProperty.Register("ForegroundColor", typeof(Color), typeof(HawkCodeBoxBase),
                new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public Color DefaultForegroundColor
        {
            get { return (Color)GetValue(DefaultForegroundColorProperty); }
            set { SetValue(DefaultForegroundColorProperty, value); }
        }

        //using a DependencyProperty to manage the background color for the text box
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Color), typeof(HawkCodeBoxBase),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnBackgroundColorChanged));

        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        //If the background color changes, we need to reset the Background property to a new transparent 
        //brush in order to keep the caret and text selection adornments
        static void OnBackgroundColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var codebox = (HawkCodeBoxBase)obj;
            codebox.Background = new SolidColorBrush(codebox.GetTransparentBackgroundColor());
        }
        
        //helper property to provide a transparent version of the current Background color
        private Color GetTransparentBackgroundColor()
        {
            return Color.FromArgb(0, BackgroundColor.R, BackgroundColor.G, BackgroundColor.B);
        }

        // Using a DependencyProperty as the backing store for SyntaxColors collection.  
        public static readonly DependencyProperty SyntaxColorsProperty =
            DependencyProperty.Register("SyntaxColors", typeof(SyntaxItemCollection), typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(new SyntaxItemCollection(), OnSyntaxColorsChanged));

        public SyntaxItemCollection SyntaxColors
        {
            get { return (SyntaxItemCollection)GetValue(SyntaxColorsProperty); }
            set { SetValue(SyntaxColorsProperty, value); }
        }

        //if the syntax colors collection changes, discard the cached dictionary of syntax colors 
        static void OnSyntaxColorsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((HawkCodeBox)obj)._syntaxMap = null;
        }

        //Cached version of the syntax colors dictionary
        private Dictionary<TokenCategory, Brush> _syntaxMap;

        //Helper property to cache the syntax colors dictionary for future use, as well as specify 
        //a default color scheme if the user doesn't provide one
        protected IDictionary<TokenCategory, Brush> SyntaxMap
        {
            get
            {
                if (_syntaxMap == null)
                {
                    _syntaxMap = new Dictionary<TokenCategory, Brush>();

                    if (SyntaxColors.Count == 0)
                    {
                        //If there are not syntax colors specificed, use the following default color scheme
                        _syntaxMap[TokenCategory.NumericLiteral] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xEE, 0x98));
                        _syntaxMap[TokenCategory.Keyword] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x66, 0x00));
                        _syntaxMap[TokenCategory.Identifier] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xCC, 0x00));
                        _syntaxMap[TokenCategory.StringLiteral] = new SolidColorBrush(Color.FromArgb(0xFF, 0x66, 0xFF, 0x00));
                        _syntaxMap[TokenCategory.Comment] = new SolidColorBrush(Color.FromArgb(0xFF, 0xD1, 0x74, 0xFF));
                        _syntaxMap[TokenCategory.LineComment] = new SolidColorBrush(Color.FromArgb(0xFF, 0xD1, 0x74, 0xFF));
                        _syntaxMap[TokenCategory.Error] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00));
                    }
                    else
                    {
                        foreach (var s in SyntaxColors)
                        {
                            _syntaxMap[s.TokenCategory] = new SolidColorBrush(s.Color);
                        }
                    }
                }

                return _syntaxMap;
            }
        }

        protected abstract IEnumerable<TokenInfo> TokenizeText();

        //Render the text in the text box, using the specified background color and syntax
        //color scheme. The original rendering done by the text box is invisible as both 
        //the foreground and background brushes are transparent
        protected override void OnRender(DrawingContext dc)
        {
            var ft = new FormattedText(
                this.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(this.FontFamily.Source),
                this.FontSize,
                new SolidColorBrush(this.DefaultForegroundColor));

            //We specify the left and top margins to match the original rendering exactly. 
            //that way, the original caret and text selection adornments line up correctly.
            var left_margin = 4.0 + this.BorderThickness.Left;
            var top_margin = 2.0 + this.BorderThickness.Top;

            dc.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));
            dc.DrawRectangle(new SolidColorBrush(this.BackgroundColor), new Pen(), new Rect(0, 0, this.ActualWidth, this.ActualHeight));

            foreach (var t in TokenizeText())
            {
                if (SyntaxMap.ContainsKey(t.Category))
                {
                    ft.SetForegroundBrush(SyntaxMap[t.Category], t.SourceSpan.Start.Index, t.SourceSpan.Length);
                }

            }

            dc.DrawText(ft, new Point(left_margin - this.HorizontalOffset, top_margin - this.VerticalOffset));
        }        
    }

    /// <summary>
    /// Simple helper class to allow users to specify syntax color schemes in XAML
    /// </summary>
    public class SyntaxItem
    {
        public TokenCategory TokenCategory { get; set; }
        public Color Color { get; set; }
    }

    //XAML appears to have an issue with generic collections, so define a simple 
    //non-generic list to contain the SyntaxItems
    public class SyntaxItemCollection : List<SyntaxItem>
    {
    }
}
