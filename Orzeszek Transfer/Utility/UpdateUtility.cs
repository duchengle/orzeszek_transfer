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
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace OrzeszekTransfer
{
	public class UpdateInfo
	{
		public string LatestVersion { get; set; }
		public string UpdateUrl { get; set; }
		public string Message { get; set; }
	}

	public static class UpdateUtility
	{
		private static UpdateInfo updateInfo;
		private static readonly string updateUrl = @"http://www.orzeszek.org/update/transfer.xml";
		private static readonly object syncUpdateInfo = new object();

		public static UpdateInfo GetLatestUpdate()
		{
			lock (syncUpdateInfo)
				try
				{
					if (updateInfo == null)
						using (WebClient wc = new WebClient())
						using (Stream s = wc.OpenRead(updateUrl))
						{
							XmlSerializer xs = new XmlSerializer(typeof(UpdateInfo));
							updateInfo = (UpdateInfo)xs.Deserialize(s);
						}

					if (updateInfo != null && new Version(updateInfo.LatestVersion) > Assembly.GetExecutingAssembly().GetName().Version)
						return updateInfo;
					else
						return null;
				}
				catch (Exception)
				{
					return null;
				}
		}
	}
}