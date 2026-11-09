// mOTP4dotNet - C# port of the original Visual Basic .NET application.
// Original VB.NET implementation by Lester Hightower, November 2011.
// The original source states that it is licensed under the GNU GPL version 3.0.

using System;
using System.Windows.Forms;

namespace mOTPWin;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
