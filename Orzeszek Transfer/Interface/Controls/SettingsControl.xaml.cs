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
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OrzeszekTransfer
{
	/// <summary>
	/// Interaction logic for SettingsControl.xaml
	/// </summary>
	public partial class SettingsControl : UserControl
	{
		private class SettingsData : INotifyPropertyChanged
		{
			private static Regex dnsNameRegex = new Regex(@"^((([a-z0-9]{1,2}))|([a-z0-9][a-z0-9-]{0,61}[a-z0-9]))(\.((([a-z0-9]{1,2}))|([a-z0-9][a-z0-9-]{0,61}[a-z0-9])))*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

			private bool detectExternalAddress;
			private string externalAddress;
			private int port;
			private bool shellIntegrate;
			private bool minimizeToTray;

			public event PropertyChangedEventHandler PropertyChanged;

			public bool DetectExternalAddress
			{
				get { return detectExternalAddress; }
				set
				{
					detectExternalAddress = value;

					if (PropertyChanged != null)
						PropertyChanged(this, new PropertyChangedEventArgs("DetectExternalAddress"));
				}
			}

			public string ExternalAddress
			{
				get { return externalAddress; }
				set
				{
					try
					{
						IPAddress.Parse(value);
					}
					catch (FormatException)
					{
						if (value.Length > 255 || !dnsNameRegex.IsMatch(value))
							throw new ArgumentException("value is not a valid IP address or DNS name.", "value");
					}

					externalAddress = value;

					if (PropertyChanged != null)
						PropertyChanged(this, new PropertyChangedEventArgs("ExternalAddress"));
				}
			}

			public int Port
			{
				get { return port; }
				set
				{
					if (value < IPEndPoint.MinPort || value > IPEndPoint.MaxPort)
						throw new ArgumentOutOfRangeException("value", port, string.Format("value must be between {0} and {1}.", IPEndPoint.MinPort, IPEndPoint.MaxPort));

					port = value;

					if (PropertyChanged != null)
						PropertyChanged(this, new PropertyChangedEventArgs("Port"));
				}
			}

			public bool ShellIntegrate
			{
				get { return shellIntegrate; }
				set
				{
					shellIntegrate = value;

					if (PropertyChanged != null)
						PropertyChanged(this, new PropertyChangedEventArgs("ShellIntegrate"));
				}
			}

			public bool MinimizeToTray
			{
				get { return minimizeToTray; }
				set
				{
					minimizeToTray = value;

					if (PropertyChanged != null)
						PropertyChanged(this, new PropertyChangedEventArgs("MinimizeToTray"));
				}
			}
		}

		private bool loaded = false;
		private SettingsData data = new SettingsData();

		public event EventHandler SettingsChanged;

		public SettingsControl()
		{
			InitializeComponent();

			DataContext = data;
			data.PropertyChanged += new PropertyChangedEventHandler(SettingsDataChanged);

			VersionHyperlink.Inlines.Add(new Run(string.Format(OrzeszekTransfer.Resources.OrzeszekTransferVersion, Assembly.GetExecutingAssembly().GetName().Version)));
			VersionHyperlink.NavigateUri = new Uri(OrzeszekTransfer.Resources.OrzeszekTransferUri);
			VersionHyperlink.RequestNavigate += new RequestNavigateEventHandler(HyperlinkUtility.RequestNavigate);
			LicenseHyperlink.RequestNavigate += new RequestNavigateEventHandler(HyperlinkUtility.RequestNavigate);
		}

		private void SettingsControl_OnLoaded(object sender, RoutedEventArgs e)
		{
			CheckIfPortIsOpen();
		}

		private void CheckIfPortIsOpen()
		{
			if (!loaded)
				LoadSettings();

			PortCheckLabel.Content = OrzeszekTransfer.Resources.CheckingIfPortIsOpen;
			PortCheckLabel.Foreground = Brushes.Gray;

			string host = data.DetectExternalAddress ? ServerUtility.GetExternalAddress() : data.ExternalAddress;
			int port = data.Port;
			ServerUtility.CheckIfPortIsOpen(host, port, CheckIfPortIsOpenCallback);
		}

		private void CheckIfPortIsOpenCallback(string host, int port, bool? portIsOpen)
		{
			Dispatcher.Invoke(new Action(() =>
			{
				string actualHost = data.DetectExternalAddress ? ServerUtility.GetExternalAddress() : data.ExternalAddress;
				int actualPort = data.Port;

				if (host == actualHost && port == actualPort)
				{
					if (!portIsOpen.HasValue)
					{
						PortCheckLabel.Content = OrzeszekTransfer.Resources.CouldNotCheckIfPortIsOpen;
						PortCheckLabel.Foreground = Brushes.Gray;
					}
					else if (portIsOpen.Value)
					{
						PortCheckLabel.Content = OrzeszekTransfer.Resources.ThePortAppearsToBeOpen;
						PortCheckLabel.Foreground = Brushes.DarkGreen;
					}
					else
					{
						PortCheckLabel.Content = OrzeszekTransfer.Resources.ThePortAppearsToBeClosed;
						PortCheckLabel.Foreground = Brushes.DarkRed;
					}
				}
			}));
		}

		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			if (Sizer.HeightFactor == 0)
				AnimateShow();
			else if (Sizer.HeightFactor == 1)
				AnimateHide();
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			if (Validation.GetHasError(AutoDetectIPCheckBox))
				return;

			if (Validation.GetHasError(IPAddressTextBox))
				return;

			if (Validation.GetHasError(PortTextBox))
				return;

			try
			{
				Settings.Default.ExternalAddress = data.DetectExternalAddress ? string.Empty : data.ExternalAddress;
				Settings.Default.Port = data.Port;
				Settings.Default.ShellIntegrate = data.ShellIntegrate;
				Settings.Default.MinimizeToTray = data.MinimizeToTray;
				Settings.Default.Save();
			}
			catch (Exception)
			{
			}

			ShellIntegrationManager.EnsureShellIntegration();

			loaded = false;
			AnimateHide();

			if (SettingsChanged != null)
				SettingsChanged(this, new EventArgs());
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			loaded = false;
			AnimateHide();
		}

		private void SettingsDataChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "DetectExternalAddress")
				try
				{
					IPAddressTextBox.IsEnabled = !data.DetectExternalAddress;
					IPAddressTextBox.Text = data.DetectExternalAddress ? ServerUtility.GetExternalAddress() : Settings.Default.ExternalAddress;
				}
				catch (Exception)
				{
				}

			if (loaded && (e.PropertyName == "DetectExternalAddress" || e.PropertyName == "ExternalAddress" || e.PropertyName == "Port"))
			{
				CheckIfPortIsOpen();
			}
		}

		private void AnimateShow()
		{
			if (!loaded)
				LoadSettings();

			SettingsButton.Template = (ControlTemplate)Resources["ActiveButtonTemplate"];

			Sizer.IsEnabled = true;

			DoubleAnimationUsingKeyFrames da = new DoubleAnimationUsingKeyFrames();
			da.KeyFrames.Add(new SplineDoubleKeyFrame(0));
			da.KeyFrames.Add(new SplineDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
			Sizer.BeginAnimation(SizerControl.HeightFactorProperty, da);
		}

		private void AnimateHide()
		{
			SettingsButton.Template = (ControlTemplate)Resources["InactiveButtonTemplate"];

			DoubleAnimationUsingKeyFrames da = new DoubleAnimationUsingKeyFrames();
			da.KeyFrames.Add(new SplineDoubleKeyFrame(1));
			da.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
			da.Completed += new EventHandler(delegate(object o, EventArgs ea)
				{
					Sizer.IsEnabled = false;
					CheckIfPortIsOpen();
				}
			);
			Sizer.BeginAnimation(SizerControl.HeightFactorProperty, da);
		}

		private void LoadSettings()
		{
			try
			{
				data.DetectExternalAddress = string.IsNullOrEmpty(Settings.Default.ExternalAddress);
				data.ExternalAddress = data.DetectExternalAddress ? ServerUtility.GetExternalAddress() : Settings.Default.ExternalAddress;
				data.Port = Settings.Default.Port;
				data.ShellIntegrate = Settings.Default.ShellIntegrate;
				data.MinimizeToTray = Settings.Default.MinimizeToTray;

				loaded = true;
			}
			catch (Exception)
			{
			}
		}
	}
}