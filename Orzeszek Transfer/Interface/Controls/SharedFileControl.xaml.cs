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
using System.Diagnostics;
using System.Linq;
using System.Text;
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
	/// Interaction logic for SharedFileControl.xaml
	/// </summary>
	public partial class SharedFileControl : UserControl
	{
		private double complete;
		private double speed;
		private TimeSpan timeRemaining;
		private long fileSize;
		private long uploaded;
		private long requested;

		private int completed;
		private int running;

		public double Complete
		{
			get { return complete; }
			set
			{
				if (value < 0 || value > 1)
					throw new ArgumentOutOfRangeException("value");

				complete = value;
				CompleteLabel.Content = string.Format(OrzeszekTransfer.Resources.StatusComplete, complete);
				OverallProgressBar.Value = 100 * complete;
			}
		}

		public double Speed
		{
			get { return speed; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value");

				speed = value;

				if (speed == 0)
					SpeedLabel.Content = OrzeszekTransfer.Resources.StatusSpeedIdle;
				else
					SpeedLabel.Content = string.Format(OrzeszekTransfer.Resources.StatusSpeed, speed.ToSpeedString());
			}
		}

		public TimeSpan TimeRemaining
		{
			get { return timeRemaining; }
			set
			{
				if (value.TotalMilliseconds < 0 && value != TimeSpan.MinValue)
					throw new ArgumentOutOfRangeException("value");

				timeRemaining = value;

				if (timeRemaining == TimeSpan.MinValue || timeRemaining == TimeSpan.MaxValue)
					TimeRemainingLabel.Content = OrzeszekTransfer.Resources.StatusTimeRemainingIdle;
				else
					TimeRemainingLabel.Content = string.Format(OrzeszekTransfer.Resources.StatusTimeRemaining, string.Format(OrzeszekTransfer.Resources.TimeSpanFormat, (int)timeRemaining.TotalHours, timeRemaining.Minutes, timeRemaining.Seconds));
			}
		}

		public long FileSize
		{
			get { return fileSize; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value");

				fileSize = value;
				FileSizeLabel.Content = string.Format(OrzeszekTransfer.Resources.StatusFileSize, fileSize.ToSizeString());
			}
		}

		public long Uploaded
		{
			get { return uploaded; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value");

				uploaded = value;

				if (uploaded == 0)
					UploadedLabel.Content = OrzeszekTransfer.Resources.StatusUploadedNone;
				else
					UploadedLabel.Content = string.Format(OrzeszekTransfer.Resources.StatusUploaded, uploaded.ToSizeString());
			}
		}

		public long Requested
		{
			get { return requested; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value");

				requested = value;

				if (requested == 0)
					RequestedLabel.Content = OrzeszekTransfer.Resources.StatusRequestedNone;
				else
					RequestedLabel.Content = string.Format(OrzeszekTransfer.Resources.StatusRequested, requested.ToSizeString());
			}
		}

		public int Completed
		{
			get { return completed; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value");

				completed = value;
				CompletedCountLabel.Content = string.Format(OrzeszekTransfer.Resources.StatusCompletedCount, completed);
				CompletedCountLabel.Visibility = completed == 0 ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		public int Running
		{
			get { return running; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value");

				running = value;
				StatusLabel.Content = running == 0 ? OrzeszekTransfer.Resources.StatusIdle : string.Format(OrzeszekTransfer.Resources.StatusRunningCount, running);
			}
		}

		public event EventHandler ClickRemove;

		public SharedFileControl(SharedFile sf)
		{
			InitializeComponent();

			FileNameTextBlock.Text = sf.Name;
			UrlHyperlink.Inlines.Add(new Run(sf.Url));
			UrlHyperlink.NavigateUri = new Uri(sf.Url);
			UrlHyperlink.RequestNavigate += new RequestNavigateEventHandler(HyperlinkUtility.RequestNavigate);

			Complete = 0;
			Speed = 0;
			TimeRemaining = TimeSpan.MinValue;
			FileSize = sf.Size;
			Uploaded = 0;
			Requested = 0;

			Completed = 0;
			Running = 0;

			Tag = sf;
			sf.Tag = this;
		}

		public void Add(StackPanel sp)
		{
			sp.Children.Add(this);

			DoubleAnimationUsingKeyFrames da = new DoubleAnimationUsingKeyFrames();
			da.KeyFrames.Add(new SplineDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
			Sizer.BeginAnimation(SizerControl.HeightFactorProperty, da);
		}

		public void AnimateAdd(StackPanel sp)
		{
			sp.Children.Add(this);

			DoubleAnimationUsingKeyFrames da = new DoubleAnimationUsingKeyFrames();
			da.KeyFrames.Add(new SplineDoubleKeyFrame(0));
			da.KeyFrames.Add(new SplineDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
			Sizer.BeginAnimation(SizerControl.HeightFactorProperty, da);
		}

		public void AnimateRemove(StackPanel sp)
		{
			DoubleAnimationUsingKeyFrames da = new DoubleAnimationUsingKeyFrames();
			da.KeyFrames.Add(new SplineDoubleKeyFrame(1));
			da.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
			da.Completed += new EventHandler(delegate(object o, EventArgs ea)
				{
					sp.Children.Remove(this);
				}
			);
			Sizer.BeginAnimation(SizerControl.HeightFactorProperty, da);
		}

		private void MinusButton_Click(object sender, RoutedEventArgs e)
		{
			if (ClickRemove != null)
				ClickRemove(this, new EventArgs());
		}

		private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(((SharedFile)Tag).Url);
		}
	}
}