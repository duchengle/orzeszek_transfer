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

namespace OrzeszekTransfer
{
	public enum MessageType
	{
		Info,
		Success,
		Error
	};

	public enum MessageLength
	{
		Short,
		Medium,
		Long,
		Indefinite,
		Infinite
	};

	public static class StatusBarManager
	{
		public static StatusBarControl StatusBar { get; set; }

		public static void AddMessage(string message, MessageType type, MessageLength length)
		{
			AddMessage(message, type, length, null, null);
		}

		public static void AddMessage(string message, MessageType type, MessageLength length, string linkText, string linkUri)
		{
			if (StatusBar != null)
				StatusBar.Dispatcher.BeginInvoke(new Action(delegate()
				{
					StatusBar.AddMessage(message, type, length, linkText, linkUri);
				}
				));
		}

		public static void ClearIndefiniteMessages()
		{
			if (StatusBar != null)
				StatusBar.Dispatcher.BeginInvoke(new Action(delegate()
					{
						StatusBar.ClearIndefiniteMessages();
					}
				));
		}
	}
}