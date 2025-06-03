using System.IO;
using System.Text;
using System.Windows.Forms;

namespace akUtil
{
    //  http://csharphelper.com/blog/2018/08/redirect-console-window-output-to-a-textbox-in-c/
    //  https://stackoverflow.com/a/18727100/1911064

    /// Install a new output TextWriter for the Console window.
    //  private void Form1_Load(object sender, EventArgs e)
    //  {
    //      TextBoxWriter writer = new TextBoxWriter(txtConsole);
    //      Console.SetOut(writer);
    //  }

    public class TextBoxWriter(Control control) : TextWriter
    {
        // The control where we will write text.
        private readonly Control MyControl = control;

        public override void Write(char value)
        {
            Write($"{value}");
        }

        public override void Write(string value)
        {
            if (MyControl.Visible)
            {
                MyControl.BeginInvoke((MethodInvoker)delegate
                {
                    MyControl.Text += value;
                });
                Unselect();
            }
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }

        private void Unselect()
        {
            if (MyControl is TextBox tb)
            {
                tb.BeginInvoke((MethodInvoker)delegate
                {
                    tb.SelectionStart = tb.Text.Length - 1;
                    tb.SelectionLength = 0;
                });
            }
        }
    }
}
