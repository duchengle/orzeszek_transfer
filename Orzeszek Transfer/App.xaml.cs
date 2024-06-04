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
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;

namespace OrzeszekTransfer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private Mutex mutex = new Mutex(true, "{985EF5A1-85C1-43a3-BFC9-4F172F354DCD}");

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			if (!mutex.WaitOne(TimeSpan.Zero, true))
			{
				mutex = null;
				MessageUtility.SendMessage(e.Args.Length > 0 ? e.Args[0] : string.Empty);
				Shutdown();
			}
			else
				Properties["PathArgument"] = e.Args.Length > 0 ? e.Args[0] : string.Empty;
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			if (mutex != null)
				mutex.ReleaseMutex();
		}
	}
}