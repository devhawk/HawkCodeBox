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
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:devhawk="clr-namespace:DevHawk.Windows.Controls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:devhawk="clr-namespace:DevHawk.Windows.Controls;assembly=HawkCodeBox"
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
    ///     <devhawk:HawkCodeBox/>
    ///
    /// </summary>
    public class HawkCodeBox : HawkCodeBoxBase
    {
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

        //Using a DependencyProperty to store the DLR language name used to colorize the text
        public static readonly DependencyProperty DlrLanguageProperty =
            DependencyProperty.Register("DlrLanguage", typeof(string), typeof(HawkCodeBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public string DlrLanguage
        {
            get { return (string)GetValue(DlrLanguageProperty); }
            set { SetValue(DlrLanguageProperty, value); }
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

        //Render the text in the text box, using the specified background color and syntax
        //color scheme. The original rendering done by the text box is invisible as both 
        //the foreground and background brushes are transparent
        protected override void OnRender(DrawingContext dc)
        {
            var ft = new FormattedText(
                this.Text,
                CultureInfo.CurrentCulture,
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

                    if (SyntaxMap.ContainsKey(t.Category))
                    {
                        ft.SetForegroundBrush(SyntaxMap[t.Category], t.SourceSpan.Start.Index, t.SourceSpan.Length);
                    }
                }
            }

            dc.DrawText(ft, new Point(left_margin - this.HorizontalOffset, top_margin - this.VerticalOffset));
        }        
    }


}
