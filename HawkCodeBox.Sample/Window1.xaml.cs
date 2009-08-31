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

namespace HawkCodeBox.Sample
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        //This is just some random sample Python code I had
        string _python_code = @"#this is a Python test
import clr
clr.AddReference(""pygments"")

from pygments import highlight
from pygments.lexers import get_all_lexers, get_lexer_by_name
from pygments.styles import get_all_styles, get_style_by_name

from devhawk_formatter import DevHawkHtmlFormatter

def get_lexers(): 
  return get_all_lexers()

def get_styles(): 
  return get_all_styles()

def generate_html(code, lexer_name, style_name):
  if not lexer_name: lexer_name = ""text""
  if not style_name: style_name = ""default""
  lexer = get_lexer_by_name(lexer_name)
  return highlight(code, lexer, DevHawkHtmlFormatter(style=style_name))";

        //This is just some random sample Ruby code I had
        string _ruby_code = @"#this is a Ruby test
require 'erb'

class App 
  def call env
    request  = Rack::Request.new env
    response = Rack::Response.new

    response.header['Content-Type'] = 'text/html'

    @msg1 = ""Hello""
    @msg2 = ""World""
    msg = ERB.new('IronRuby running Rack says ""<%= @msg1 %>, <b><%= @msg2 %></b>"" at <%= Time.now %>').result(binding)

    response.write msg

    response.finish
  end
end";

        public Window1()
        {
            InitializeComponent();
            SetPython();
        }

        private void PythonMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetPython();
        }

        private void RubyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetRuby();
        }

        void SetPython()
        {
            codebox.Language = "Python";
            codebox.Text = _python_code;
        }

        void SetRuby()
        {
            codebox.Language = "Ruby";
            codebox.Text = _ruby_code;
        }
    }
}
