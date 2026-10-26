using System;
using System.Windows.Forms;
using Lavrinovich.Forms;

namespace Lavrinovich
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UsersForm());
        }
    }
}
