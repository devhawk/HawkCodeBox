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
        protected override IEnumerable<TokenInfo> TokenizeText()
        {
            //at design time, we won't have access to any DLR language assemblies, so don't try to colorize
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                var source = Engine.CreateScriptSourceFromString(this.Text);
                var tokenizer = Engine.GetService<TokenCategorizer>();
                tokenizer.Initialize(null, source, SourceLocation.MinValue);
                while (true)
                {
                    var t = tokenizer.ReadToken();
                    yield return t;
                    if (t.Category == TokenCategory.EndOfStream)
                        break;
                }
            }
        }
    }


}
