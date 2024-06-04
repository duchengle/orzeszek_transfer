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

namespace OrzeszekTransfer
{
	public class SizerControl : ContentControl
	{
		public static readonly DependencyProperty HeightFactorProperty = DependencyProperty.Register("HeightFactor", typeof(double), typeof(SizerControl), new FrameworkPropertyMetadata((double)1, FrameworkPropertyMetadataOptions.AffectsMeasure));
		public static readonly DependencyProperty WidthFactorProperty = DependencyProperty.Register("WidthFactor", typeof(double), typeof(SizerControl), new FrameworkPropertyMetadata((double)1, FrameworkPropertyMetadataOptions.AffectsMeasure));
		public static readonly DependencyProperty UseMinHeightProperty = DependencyProperty.Register("UseMinHeight", typeof(bool), typeof(SizerControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));
		public static readonly DependencyProperty UseMinWidthProperty = DependencyProperty.Register("UseMinWidth", typeof(bool), typeof(SizerControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

		public double HeightFactor
		{
			get { return (double)GetValue(SizerControl.HeightFactorProperty); }
			set { SetValue(SizerControl.HeightFactorProperty, value); }
		}

		public double WidthFactor
		{
			get { return (double)GetValue(SizerControl.WidthFactorProperty); }
			set { SetValue(SizerControl.WidthFactorProperty, value); }
		}

		public bool UseMinHeight
		{
			get { return (bool)GetValue(SizerControl.UseMinHeightProperty); }
			set { SetValue(SizerControl.UseMinHeightProperty, value); }
		}

		public bool UseMinWidth
		{
			get { return (bool)GetValue(SizerControl.UseMinWidthProperty); }
			set { SetValue(SizerControl.UseMinWidthProperty, value); }
		}

		protected override Size MeasureOverride(Size constraint)
		{
			Size size = base.MeasureOverride(constraint);

			size.Height = UseMinHeight ? (size.Height - MinHeight) * HeightFactor + MinHeight : HeightFactor * size.Height;
			size.Width = UseMinWidth ? (size.Width - MinWidth) * WidthFactor + MinWidth : WidthFactor * size.Width;

			size.Height = Math.Min(size.Height, constraint.Height);
			size.Width = Math.Min(size.Width, constraint.Width);

			return size;
		}
	}
}