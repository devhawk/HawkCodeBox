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
        class CachedToken
        {
            public TokenInfo Token { get; set; }
            public object TokenizerState { get; set; }
        }

        List<CachedToken> tokens;

        private CachedToken FindChangePoint(int offset)
        {
            CachedToken prev = null;
            foreach (var token in tokens)
            {
                if (token.Token.SourceSpan.End.Index < offset)
                {
                    prev = token;
                    continue;
                }

                return prev;
            }

            return null;
        }

        private void InitTokenizer(TokenCategorizer tokenizer)
        {
            if (tokens.Count > 0)
            {
                var lastToken = tokens.Last();
                var source = Engine.CreateScriptSourceFromString(
                    this.Text.Substring(lastToken.Token.SourceSpan.End.Index));
                tokenizer.Initialize(
                    lastToken.TokenizerState, 
                    source,
                    lastToken.Token.SourceSpan.End);
            }
            else
            {
                var source = Engine.CreateScriptSourceFromString(this.Text);
                tokenizer.Initialize(null, source, SourceLocation.MinValue);
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            //at design time, we won't have access to any DLR language assemblies, so don't try to colorize
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                var tokenizer = Engine.GetService<TokenCategorizer>();
                if (tokenizer.IsRestartable)
                {
                    if (tokens == null) tokens = new List<CachedToken>();
                    foreach (var change in e.Changes)
                    {
                        CachedToken lastgood = FindChangePoint(change.Offset);
                        if (lastgood != null)
                        {
                            var i = tokens.IndexOf(lastgood);
                            tokens.RemoveRange(i+1, tokens.Count - i - 1);
                        }
                    }

                    InitTokenizer(tokenizer);
                    while (true)
                    {
                        var t = tokenizer.ReadToken();
                        if (t.Category == TokenCategory.EndOfStream)
                            break;

                        tokens.Add(new CachedToken()
                        {
                            Token = t,
                            TokenizerState = tokenizer.CurrentState
                        });
                    }
                }
            }

            base.OnTextChanged(e);
        }

        protected override IEnumerable<TokenInfo> TokenizeText()
        {
            //at design time, we won't have access to any DLR language assemblies, so don't try to colorize
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                var tokenizer = Engine.GetService<TokenCategorizer>();
                if (!tokenizer.IsRestartable)
                {
                    var source = Engine.CreateScriptSourceFromString(this.Text);
                    tokenizer.Initialize(null, source, SourceLocation.MinValue);
                    while (true)
                    {
                        var t = tokenizer.ReadToken();
                        if (t.Category == TokenCategory.EndOfStream)
                            break;
                        yield return t;
                    }
                }
                else
                {
                    foreach (var ct in tokens)
                    {
                        yield return ct.Token;
                    }
                }
            }
        }
    }


}
