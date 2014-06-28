using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            

            /*  INIT USERINTERFACE */
            Console.WriteLine("Initialising GUI...");

            NotifyIcon trayIcon = new NotifyIcon();
            trayIcon.Text = "Redundancy Client";
            trayIcon.Icon = Client.Properties.Resources.redundancy;

            ContextMenu trayMenu = new ContextMenu();

            MenuItem mItemSettings = new MenuItem("Settings", new EventHandler(mItemSettings_Click));
            MenuItem mItemExit = new MenuItem("Exit", new EventHandler(mItemExit_Click));
            trayMenu.MenuItems.Add(mItemSettings);
            trayMenu.MenuItems.Add(mItemExit);
            
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            
            Application.Run();
        }

        private static void mItemSettings_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException("TODO: Implement settings dialog.");
        }

        private static void mItemExit_Click(object sender, EventArgs e)
        {
            Application.Exit(); //Removes trayIcon from Tray.
        }
    }
}
