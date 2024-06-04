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
using System.Windows.Input;

namespace OrzeszekTransfer
{
	public static class AccessKeyScoper
	{
		public static readonly DependencyProperty IsAccessKeyScopeProperty = DependencyProperty.RegisterAttached("IsAccessKeyScope", typeof(bool), typeof(AccessKeyScoper), new FrameworkPropertyMetadata(false, HandleIsAccessKeyScopePropertyChanged));

		public static bool GetIsAccessKeyScope(DependencyObject o)
		{
			return (bool)o.GetValue(AccessKeyScoper.IsAccessKeyScopeProperty);
		}

		public static void SetIsAccessKeyScope(DependencyObject o, bool value)
		{
			o.SetValue(AccessKeyScoper.IsAccessKeyScopeProperty, value);
		}

		private static void HandleIsAccessKeyScopePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue.Equals(true))
				AccessKeyManager.AddAccessKeyPressedHandler(o, HandleScopedElementAccessKeyPressed);
			else
				AccessKeyManager.RemoveAccessKeyPressedHandler(o, HandleScopedElementAccessKeyPressed);
		}

		private static void HandleScopedElementAccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
		{
			if (!Keyboard.IsKeyDown(Key.LeftAlt) && !Keyboard.IsKeyDown(Key.RightAlt) && GetIsAccessKeyScope((DependencyObject)sender))
			{
				e.Scope = sender;
				e.Handled = true;
			}
		}
	}
}