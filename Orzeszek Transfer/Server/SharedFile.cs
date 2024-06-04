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
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace OrzeszekTransfer
{
	public class HttpConnectionEventArgs : EventArgs
	{
		public HttpConnection Connection { get; private set; }

		public HttpConnectionEventArgs(HttpConnection upload)
		{
			Connection = upload;
		}
	}

	public class SharedFile : IDisposable
	{
		private bool disposed;

		private List<HttpConnection> uploads = new List<HttpConnection>();

		public DateTime Created { get; set; }

		public string FullName { get; set; }
		public string Name { get; set; }
		public string ID { get; set; }
		public long Size { get; set; }

		public string Url
		{
			get
			{
				string externalAddress = string.IsNullOrEmpty(Settings.Default.ExternalAddress) ? ServerUtility.GetExternalAddress() : Settings.Default.ExternalAddress;
				int port = Settings.Default.Port;
				string id = ID;
				string name = Uri.EscapeDataString(Name);

				if (port == 80)
				{
					return string.Format(CultureInfo.InvariantCulture, Resources.SharedFileUrlWithoutPort, externalAddress, id, name);
				}
				else
				{
					return string.Format(CultureInfo.InvariantCulture, Resources.SharedFileUrl, externalAddress, port, id, name);
				}
			}
		}

		public object Tag { get; set; }

		public bool IsDisposed { get { return disposed; } }

		public event EventHandler<HttpConnectionEventArgs> UploadStarted;
		public event EventHandler<HttpConnectionEventArgs> UploadStopped;

		public SharedFile()
		{
			Created = DateTime.Now;
		}

		public static SharedFileInfo ToSharedFileInfo(SharedFile sf)
		{
			return new SharedFileInfo
			{
				FullName = sf.FullName,
				Name = sf.Name,
				ID = sf.ID
			};
		}

		public void OnUploadStarted(HttpConnection connection)
		{
			lock (uploads)
				uploads.Add(connection);

			if (UploadStarted != null)
				UploadStarted(this, new HttpConnectionEventArgs(connection));
		}

		public void OnUploadStopped(HttpConnection connection)
		{
			lock (uploads)
				uploads.Remove(connection);

			if (UploadStopped != null)
				UploadStopped(this, new HttpConnectionEventArgs(connection));
		}

		public void Abort()
		{
			Dispose(false /* disposing */, true /* aborting */);
			GC.SuppressFinalize(this);
		}

		public void Dispose()
		{
			Dispose(true /* disposing */, false /* aborting */);
			GC.SuppressFinalize(this);
		}

		public void Dispose(bool disposing, bool aborting)
		{
			if (disposed)
				return;

			disposed = true;

			if (disposing || aborting)
			{
				lock (uploads)
				{
					foreach (HttpConnection upload in uploads)
					{
						if (aborting)
							upload.Abort();
						else
							upload.Dispose();
					}
				}
			}
		}
	}
}