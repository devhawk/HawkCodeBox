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
        //Using a DependencyProperty to store the DLR language name used to colorize the text
        public static readonly DependencyProperty LanguageProperty =
            DependencyProperty.Register("Language", typeof(string), typeof(HawkCodeBoxBase),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnLanguageChanged));

        public string Language
        {
            get { return (string)GetValue(LanguageProperty); }
            set { SetValue(LanguageProperty, value); }
        }

        static void OnLanguageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((HawkCodeBox)obj).BuildTokenCache(0);
        }

        //helper property to retrieve the engine for the current language
        ScriptRuntime _runtime;
        protected ScriptEngine Engine
        {
            get
            {
                if (_runtime == null)
                {
                    var setup = ScriptRuntimeSetup.ReadConfiguration();
                    _runtime = new ScriptRuntime(setup);
                }
                return _runtime.GetEngine(this.Language);
            }
        }

        class CachedToken
        {
            public TokenInfo Token { get; set; }
            public object TokenizerState { get; set; }
        }

        List<CachedToken> tokens = new List<CachedToken>();

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            var offset = e.Changes.Select(c => c.Offset).Min();
            BuildTokenCache(offset);
        }

        private void BuildTokenCache(int offset)
        {
            //at design time, we won't have access to any DLR language assemblies, so don't try to colorize
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            //If the tokenizer isn't restartable, clear the cached tokens
            var tokenizer = Engine.GetService<TokenCategorizer>();
            if (!tokenizer.IsRestartable)
                tokens.Clear();

            var last_valid_token = tokens.FindLast(ct => ct.Token.SourceSpan.End.Index < offset);

            if (last_valid_token != null)
            {
                var i = tokens.IndexOf(last_valid_token) + 1;
                tokens.RemoveRange(i, tokens.Count - i);

                tokenizer.Initialize(
                    last_valid_token.TokenizerState,
                    Engine.CreateScriptSourceFromString(
                        Text.Substring(last_valid_token.Token.SourceSpan.End.Index)),
                    last_valid_token.Token.SourceSpan.End);
            }
            else
            {
                tokenizer.Initialize(
                    null,
                    Engine.CreateScriptSourceFromString(Text),
                    SourceLocation.MinValue);
            }

            TokenInfo token;
            while ((token = tokenizer.ReadToken()).Category != TokenCategory.EndOfStream)
            {
                tokens.Add(new CachedToken()
                {
                    Token = token,
                    TokenizerState = tokenizer.CurrentState
                });
            }
        }

        protected override IEnumerable<TokenInfo> TokenizeText()
        {
            return tokens.Select(ct => ct.Token);
        }
    }


}
