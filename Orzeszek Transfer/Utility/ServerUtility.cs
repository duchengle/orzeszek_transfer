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
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace OrzeszekTransfer
{
	public delegate void CheckIfPortIsOpenDelegate(string host, int port, bool? portIsOpen);

	public static class ServerUtility
	{
		private static string externalAddress;
		private static readonly string[] externalAddressProviderUrls = { "http://icanhazip.com/", "http://www.telize.com/ip", "http://checkip.dyndns.org/" };
		private static readonly Regex ipAddressRegex = new Regex(@"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b");
		private static readonly object syncExternalAddress = new object();

		public static string GetExternalAddress()
		{
			lock (syncExternalAddress)
			{
				if (string.IsNullOrEmpty(externalAddress))
				{
					// Load the last IP address we saved as a default
					externalAddress = Settings.Default.DetectedExternalAddress;

					// Update the external IP address from the first provider that works
					using (WebClient wc = new WebClient())
					{
						foreach (string url in externalAddressProviderUrls)
						{
							try
							{
								string downloadedString = wc.DownloadString(url);
								Match ipAddressMatch = ipAddressRegex.Match(downloadedString);
								if (ipAddressMatch.Success)
								{
									IPAddress ipAddress = IPAddress.Parse(ipAddressMatch.ToString());
									externalAddress = ipAddress.ToString();
									break;
								}
							}
							catch (Exception)
							{
								// Just try the next IP provider or give up
							}
						}
					}

					// Save the IP address we found
					try
					{
						Settings.Default.DetectedExternalAddress = externalAddress;
						Settings.Default.Save();
					}
					catch (Exception)
					{
						// Not worth reporting this
					}
				}

				return string.IsNullOrEmpty(externalAddress) ? "(Unknown)" : externalAddress;
			}
		}

		private delegate bool CheckIfPortIsOpenImplementation(string host, int port, CheckIfPortIsOpenDelegate callback);

		private static readonly CheckIfPortIsOpenImplementation[] checkIfPortIsOpenImplementations = { CheckIfPortIsOpenCanYouSeeMe, CheckIfPortIsOpenPingEu };

		public static void CheckIfPortIsOpen(string host, int port, CheckIfPortIsOpenDelegate callback)
		{
			Thread thread = new Thread(() => CheckIfPortIsOpenInternal(host, port, callback));
			thread.Start();
		}

		private static void CheckIfPortIsOpenInternal(string host, int port, CheckIfPortIsOpenDelegate callback)
		{
			foreach (CheckIfPortIsOpenImplementation implementation in checkIfPortIsOpenImplementations)
			{
				if (implementation(host, port, callback))
				{
					return;
				}
			}
		}

		private static bool CheckIfPortIsOpenCanYouSeeMe(string host, int port, CheckIfPortIsOpenDelegate callback)
		{
			string returnString = string.Empty;
			try
			{
				using (WebClient wc = new WebClient())
				{
					string portCheckUrl = "http://www.canyouseeme.org/";

					NameValueCollection postData = new NameValueCollection();
					postData["port"] = port.ToString();
					postData["IP"] = host;

					byte[] returnData = wc.UploadValues(portCheckUrl, postData);
					returnString = Encoding.UTF8.GetString(returnData);
				}
			}
			catch
			{
			}

			if (Regex.IsMatch(returnString, @"Success.*I can see your service", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
			{
				callback(host, port, true);
				return true;
			}
			else if (Regex.IsMatch(returnString, @"Error.*I could \<b\>not\<\/b\> see your service on ", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
			{
				callback(host, port, false);
				return true;
			}
			else
			{
				callback(host, port, null);
				return false;
			}
		}

		private static bool CheckIfPortIsOpenPingEu(string host, int port, CheckIfPortIsOpenDelegate callback)
		{
			string returnString = string.Empty;
			try
			{
				using (WebClient wc = new WebClient())
				{
					string portCheckUrl = "http://ping.eu/action.php?atype=5";

					NameValueCollection postData = new NameValueCollection();
					postData["host"] = host;
					postData["port"] = port.ToString();
					postData["go"] = "Go";

					byte[] returnData = wc.UploadValues(portCheckUrl, postData);
					returnString = Encoding.UTF8.GetString(returnData);
				}
			}
			catch
			{
			}

			if (Regex.IsMatch(returnString, @"port is.*open", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
			{
				callback(host, port, true);
				return true;
			}
			else if (Regex.IsMatch(returnString, @"port is.*closed", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
			{
				callback(host, port, false);
				return true;
			}
			else
			{
				callback(host, port, null);
				return false;
			}
		}
	}
}