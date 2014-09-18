using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace clientRedundancy
{
	class UserInterface
	{
		public frmSettings settingsForm;
		public NotifyIcon trayIcon;

		public UserInterface(){
			InitializeTrayIcon();
			InitializeSettingsWindow();
			Application.Run();
		}

		public void InitializeTrayIcon()
		{
			trayIcon = new NotifyIcon();
			trayIcon.Text = "Redundancy for Windows";
			trayIcon.Icon = Properties.Resources.iconRedundancy;
			
			ContextMenu ctxMenu = new ContextMenu();
			ctxMenu.MenuItems.AddRange(new MenuItem[] {new MenuItem("Settings", onClickSettings), new MenuItem("Exit", onClickExit)});

			trayIcon.ContextMenu = ctxMenu;
			trayIcon.Visible = true;
		}

		public void InitializeSettingsWindow()
		{
			settingsForm = new frmSettings();
		}


		#region Events

		private void onClickSettings(object sender, EventArgs e)
		{
			settingsForm.Show();
		}

		private void onClickExit(object sender, EventArgs e)
		{
			Application.Exit();
		}

		#endregion

	}
}
