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
	/// Interaction logic for StatusBarControl.xaml
	/// </summary>
	public partial class StatusBarControl : UserControl
	{
		public StatusBarControl()
		{
			InitializeComponent();
		}

		public void AddMessage(string message, MessageType type, MessageLength length, string linkText, string linkUri)
		{
			TextBlock textBlock = new TextBlock();
			textBlock.Inlines.Add(new Run(message));

			if (!string.IsNullOrEmpty(linkText) && !string.IsNullOrEmpty(linkUri))
			{
				Hyperlink hl = new Hyperlink(new Run(linkText));
				hl.NavigateUri = new Uri(linkUri);
				hl.RequestNavigate += new RequestNavigateEventHandler(HyperlinkUtility.RequestNavigate);

				textBlock.Inlines.Add(new Run(" "));
				textBlock.Inlines.Add(hl);
			}

			Border border = new Border();
			border.Child = textBlock;

			SizerControl sizer = new SizerControl();
			sizer.Content = border;

			switch (type)
			{
				case MessageType.Info:
					border.Style = (Style)Resources["InfoStyle"];
					break;
				case MessageType.Success:
					border.Style = (Style)Resources["SuccessStyle"];
					break;
				case MessageType.Error:
					border.Style = (Style)Resources["ErrorStyle"];
					break;
				default:
					throw new ArgumentOutOfRangeException("type");
			}

			switch (length)
			{
				case MessageLength.Short:
					sizer.Tag = DateTime.Now.AddSeconds(2);
					break;
				case MessageLength.Medium:
					sizer.Tag = DateTime.Now.AddSeconds(5);
					break;
				case MessageLength.Long:
					sizer.Tag = DateTime.Now.AddSeconds(10);
					break;
				case MessageLength.Indefinite:
					sizer.Tag = DateTime.MaxValue;
					break;
				case MessageLength.Infinite:
					sizer.Tag = DateTime.MaxValue;
					break;
				default:
					throw new ArgumentOutOfRangeException("length");
			}

			AnimateAdd(sizer);
		}

		public void ClearIndefiniteMessages()
		{
			lock (MessagesStackPanel)
				foreach (SizerControl sizer in MessagesStackPanel.Children)
					if ((DateTime)sizer.Tag == DateTime.MaxValue)
						AnimateRemove(sizer);
		}

		public void UpdateLabels()
		{
			lock (MessagesStackPanel)
				foreach (SizerControl sizer in MessagesStackPanel.Children)
					if ((DateTime)sizer.Tag < DateTime.Now)
						AnimateRemove(sizer);
		}

		private void AnimateAdd(SizerControl innerSizer)
		{
			DoubleAnimationUsingKeyFrames da = new DoubleAnimationUsingKeyFrames();
			da.KeyFrames.Add(new SplineDoubleKeyFrame(0));
			da.KeyFrames.Add(new SplineDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));

			if (Visibility == Visibility.Collapsed)
			{
				Visibility = Visibility.Visible;
				MessagesStackPanel.Children.Add(innerSizer);
				Sizer.BeginAnimation(SizerControl.HeightFactorProperty, da);
			}
			else
			{
				innerSizer.HeightFactor = 0;
				MessagesStackPanel.Children.Add(innerSizer);
				innerSizer.BeginAnimation(SizerControl.HeightFactorProperty, da);
			}
		}

		private void AnimateRemove(SizerControl innerSizer)
		{
			DoubleAnimationUsingKeyFrames da = new DoubleAnimationUsingKeyFrames();
			da.KeyFrames.Add(new SplineDoubleKeyFrame(1));
			da.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
			da.Completed += new EventHandler(delegate(object o, EventArgs ea)
				{
					lock (MessagesStackPanel)
					{
						MessagesStackPanel.Children.Remove(innerSizer);
						Visibility = MessagesStackPanel.Children.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
					}
				}
			);

			if (MessagesStackPanel.Children.Count == 1)
				Sizer.BeginAnimation(SizerControl.HeightFactorProperty, da);
			else
				innerSizer.BeginAnimation(SizerControl.HeightFactorProperty, da);
		}
	}
}