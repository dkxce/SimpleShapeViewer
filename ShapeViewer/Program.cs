using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ShapeViewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            frmMain frm = new frmMain();
            if((args != null) && (args.Length > 0))
                frm.File2Open = args[0];
            Application.Run(frm);
        }
    }
}