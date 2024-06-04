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
using Microsoft.Win32;

namespace OrzeszekTransfer
{
	public static class ContextMenuUtility
	{
		public static void AddContextMenu(string fileType, string keyName, string menuText, string menuCommand)
		{
			string mainPath = "Software\\Classes\\" + fileType + "\\shell\\" + keyName;
			string commandPath = mainPath + "\\command";

			using (RegistryKey key = Registry.CurrentUser.CreateSubKey(mainPath))
				key.SetValue(null, menuText);

			using (RegistryKey key = Registry.CurrentUser.CreateSubKey(commandPath))
				key.SetValue(null, menuCommand);
		}

		public static bool HasContextMenu(string fileType, string keyName)
		{
			try
			{
				string mainPath = "Software\\Classes\\" + fileType + "\\shell\\" + keyName;
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(mainPath))
					return key != null;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static void RemoveContextMenu(string fileType, string keyName)
		{
			string mainPath = "Software\\Classes\\" + fileType + "\\shell\\" + keyName;
			Registry.CurrentUser.DeleteSubKeyTree(mainPath);
		}
	}
}