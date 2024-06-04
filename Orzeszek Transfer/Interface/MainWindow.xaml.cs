//
// Copyright (C) 2014 Chris Dziemborowicz
//
// This file is part of Orzeszek Transfer.
//
// Orzeszek Transfer is free software: you can redistribute it and/or
// modify it under the terms of the GNU General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// Orzeszek Transfer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ContextMenu = System.Windows.Forms.ContextMenu;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace OrzeszekTransfer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private HttpListener listener;
		private Thread systemUpdaterThread;
		private Thread interfaceUpdaterThread;
		private NotifyIcon notifyIcon;
		private WindowInteropHelper interopHelper;
		private WindowState restoreState;

		public MainWindow()
		{
			InitializeComponent();

			if (Settings.Default.WindowSettingsSaved)
				try
				{
					Height = Settings.Default.WindowHeight;
					Width = Settings.Default.WindowWidth;
					Left = Settings.Default.WindowLeft;
					Top = Settings.Default.WindowTop;
					WindowState = Settings.Default.WindowState;
				}
				catch (Exception)
				{
				}

			restoreState = WindowState;

			UpdateNotifyIcon();

			ShellIntegrationManager.EnsureShellIntegration();

			// Prefetch external IP address
			Thread prefetchThread = new Thread(() => ServerUtility.GetExternalAddress());
			prefetchThread.Start();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			StatusBarManager.StatusBar = StatusBar;

			try
			{
				listener = new HttpListener();
				listener.Start(Settings.Default.Port);
			}
			catch (Exception)
			{
				StatusBarManager.AddMessage(string.Format(OrzeszekTransfer.Resources.ListenerFailedStart, Settings.Default.Port), MessageType.Error, MessageLength.Indefinite);
			}

			interfaceUpdaterThread = new Thread(new ThreadStart(ProcessInterfaceUpdates));
			interfaceUpdaterThread.Start();

			systemUpdaterThread = new Thread(new ThreadStart(ProcessSystemUpdates));
			systemUpdaterThread.Start();

			interopHelper = new WindowInteropHelper(this);

			SharedFileManager.LoadFromSettings();
			foreach (SharedFile sf in SharedFileManager.GetSharedFiles())
			{
				AddFile(sf, false /* animate */);
			}

			MessageUtility.RegisterWindow(this);
			MessageUtility.MessageReceived += new EventHandler<MessageEventArgs>(MessageReceived);

			string sharedFilePath = (string)App.Current.Properties["PathArgument"];
			if (!string.IsNullOrEmpty(sharedFilePath))
				AddFile(sharedFilePath);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
			{
				Settings.Default.WindowHeight = RestoreBounds.Height;
				Settings.Default.WindowWidth = RestoreBounds.Width;
				Settings.Default.WindowLeft = RestoreBounds.Left;
				Settings.Default.WindowTop = RestoreBounds.Top;
				Settings.Default.WindowState = WindowState;
				Settings.Default.WindowSettingsSaved = true;
				Settings.Default.Save();
			}
			catch (Exception)
			{
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			if (listener != null)
				listener.Abort();

			if (systemUpdaterThread != null)
				systemUpdaterThread.Abort();

			if (interfaceUpdaterThread != null)
				interfaceUpdaterThread.Abort();

			if (notifyIcon != null)
				notifyIcon.Dispose();

			SpeedUtility.Dispose();

			SharedFileManager.SaveToSettings();

			if (PortableSettingsProvider.IsPortable)
				ShellIntegrationManager.RemoveShellIntegration();
		}

		private void Window_StateChanged(object sender, EventArgs e)
		{
			if (WindowState != WindowState.Minimized)
				restoreState = WindowState;
			else if (Settings.Default.MinimizeToTray)
				Hide();
		}

		private void SettingsControl_SettingsChanged(object sender, EventArgs e)
		{
			foreach (SharedFileControl sfc in FilesStackPanel.Children)
			{
				SharedFile sf = (SharedFile)sfc.Tag;
				sfc.UrlHyperlink.Inlines.Clear();
				sfc.UrlHyperlink.Inlines.Add(new Run(sf.Url));
				sfc.UrlHyperlink.NavigateUri = new Uri(sf.Url);
			}

			try
			{
				if (listener.Port != Settings.Default.Port)
				{
					StatusBarManager.ClearIndefiniteMessages();

					listener.Stop();
					listener.Start(Settings.Default.Port);
				}
			}
			catch (Exception)
			{
				StatusBarManager.AddMessage(string.Format(OrzeszekTransfer.Resources.ListenerFailedStart, Settings.Default.Port), MessageType.Error, MessageLength.Indefinite);
			}

			UpdateNotifyIcon();
		}

		private void ShowAndActivateWindow()
		{
			Show();

			if (WindowState == WindowState.Minimized)
				WindowState = restoreState;

			Activate();
			Focus();
		}

		private void MessageReceived(object sender, MessageEventArgs e)
		{
			ShowAndActivateWindow();

			if (!string.IsNullOrEmpty(e.Message))
				AddFile(e.Message);
		}

		private void AddFileControl_Click(object sender, EventArgs e)
		{
			System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
			if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				AddFile(ofd.FileName);
		}

		private void AddFile(string filename)
		{
			try
			{
				SharedFile sf = SharedFileManager.GetByFileName(filename);

				if (sf == null)
				{
					sf = SharedFileManager.Add(filename);
					SharedFileManager.SaveToSettings();

					AddFile(sf, true /* animate */);
				}
				
				Clipboard.SetText(sf.Url);
				StatusBarManager.AddMessage(OrzeszekTransfer.Resources.UrlCopiedToClipboard, MessageType.Info, MessageLength.Short);
			}
			catch (FileNotFoundException)
			{
				StatusBarManager.AddMessage(OrzeszekTransfer.Resources.FailedToMakeAvailableNotFound, MessageType.Error, MessageLength.Medium);
			}
			catch (Exception)
			{
				StatusBarManager.AddMessage(OrzeszekTransfer.Resources.FailedToMakeAvailable, MessageType.Error, MessageLength.Medium);
			}
		}

		private void AddFile(SharedFile sf, bool animate)
		{
			SharedFileControl sfc = new SharedFileControl(sf);

			sfc.ClickRemove += new EventHandler(FileShareRemoved);
			sf.UploadStarted += new EventHandler<HttpConnectionEventArgs>(UploadStarted);
			sf.UploadStopped += new EventHandler<HttpConnectionEventArgs>(UploadStopped);

			if (animate)
				sfc.AnimateAdd(FilesStackPanel);
			else
				sfc.Add(FilesStackPanel);
		}

		private void FileShareRemoved(object sender, EventArgs e)
		{
			SharedFileControl sfc = (SharedFileControl)sender;
			SharedFile sf = (SharedFile)sfc.Tag;

			SharedFileManager.Remove(sf);
			sfc.AnimateRemove(FilesStackPanel);

			SharedFileManager.SaveToSettings();
		}

		private void UploadStarted(object sender, HttpConnectionEventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(delegate()
				{
					SharedFile sf = (SharedFile)sender;
					SharedFileControl sfc = (SharedFileControl)sf.Tag;

					ProgressBar pb = new ProgressBar();
					e.Connection.Tag = pb;
					pb.Tag = e.Connection;
					pb.Value = 100.0 * e.Connection.Position / sf.Size;
					sfc.ProgressBarsStackPanel.Children.Add(pb);
					sfc.Running++;

					UpdateInterface();
				}
			));
		}

		private void UploadStopped(object sender, HttpConnectionEventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(delegate()
				{
					SharedFile sf = (SharedFile)sender;
					SharedFileControl sfc = (SharedFileControl)sf.Tag;

					ProgressBar pb = (ProgressBar)e.Connection.Tag;

					if (e.Connection.Position == sf.Size)
					{
						pb.Style = (Style)sfc.ProgressBarsStackPanel.Resources["CompletedTransferStyle"];
						pb.Value = 100;
						sfc.Completed++;
						StatusBarManager.AddMessage(string.Format(OrzeszekTransfer.Resources.TransferSuccess, sf.Name), MessageType.Success, MessageLength.Medium);
					}
					else
					{
						sfc.ProgressBarsStackPanel.Children.Remove(pb);

						if (e.Connection.IsFailed)
							StatusBarManager.AddMessage(string.Format(OrzeszekTransfer.Resources.TransferInterrupted, sf.Name), MessageType.Error, MessageLength.Medium);
					}

					sfc.Running--;

					UpdateInterface();
				}
			));
		}

		private void ProcessInterfaceUpdates()
		{
			while (true)
			{
				Dispatcher.BeginInvoke(new Action(UpdateInterface));
				Thread.Sleep(1000);
			}
		}

		private void UpdateInterface()
		{
			long totalUploaded = 0;
			long totalRequested = 0;

			foreach (SharedFileControl sfc in FilesStackPanel.Children)
			{
				SharedFile sf = (SharedFile)sfc.Tag;

				double speed = 0;
				long uploaded = 0;
				long requested = 0;

				foreach (ProgressBar pb in sfc.ProgressBarsStackPanel.Children)
				{
					HttpConnection upload = (HttpConnection)pb.Tag;
					pb.Value = 100.0 * upload.Position / sf.Size;
					speed += upload.Speed;
					uploaded += upload.Position;
					requested += sf.Size;
				}

				totalUploaded += uploaded;
				totalRequested += requested;

				sfc.Complete = requested == 0 ? 0.0 : (double)uploaded / requested;
				sfc.Speed = speed;
				sfc.TimeRemaining = speed == 0 ? TimeSpan.MinValue : TimeSpan.FromSeconds((requested - uploaded) / speed);
				sfc.Uploaded = uploaded;
				sfc.Requested = requested;
			}

			if (totalUploaded == totalRequested)
				TaskbarUtility.SetProgressState(interopHelper.Handle, TaskbarProgressState.NoProgress);
			else
				TaskbarUtility.SetProgressValue(interopHelper.Handle, totalRequested == 0 ? 0 : (ulong)(100.0 * totalUploaded / totalRequested), 100);

			StatusBar.UpdateLabels();
		}

		private void UpdateNotifyIcon()
		{
			if (Settings.Default.MinimizeToTray && notifyIcon == null)
			{
				notifyIcon = new NotifyIcon();
				notifyIcon.Text = OrzeszekTransfer.Resources.OrzeszekTransfer;
				notifyIcon.Icon = OrzeszekTransfer.Resources.Icon;
				notifyIcon.Click += (sender, args) => ShowAndActivateWindow();
				notifyIcon.ContextMenu = new ContextMenu();
				notifyIcon.ContextMenu.MenuItems.Add(OrzeszekTransfer.Resources.TrayIconShow, (sender, args) => ShowAndActivateWindow());
				notifyIcon.ContextMenu.MenuItems.Add(OrzeszekTransfer.Resources.TrayIconExit, (sender, args) => Close());
				notifyIcon.Visible = true;
			}
			else if (!Settings.Default.MinimizeToTray && notifyIcon != null)
			{
				notifyIcon.Dispose();
				notifyIcon = null;
			}
		}

		private void ProcessSystemUpdates()
		{
			if (string.IsNullOrEmpty(Settings.Default.ExternalAddress))
				ServerUtility.GetExternalAddress();

			UpdateInfo ui = UpdateUtility.GetLatestUpdate();
			if (ui != null)
				StatusBarManager.AddMessage(string.IsNullOrEmpty(ui.Message) ? string.Format(OrzeszekTransfer.Resources.NewVersionAvailable, ui.LatestVersion, ui.UpdateUrl.Replace("http://", string.Empty)) : ui.Message, MessageType.Info, MessageLength.Infinite, OrzeszekTransfer.Resources.DownloadLinkText, ui.UpdateUrl);
		}
	}
}