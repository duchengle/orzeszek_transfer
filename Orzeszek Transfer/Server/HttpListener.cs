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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OrzeszekTransfer
{
	public class HttpListener : IDisposable
	{
		private bool disposed;
		private int port = -1;

		private Socket socket;
		private Thread thread;
		private List<HttpConnection> connections = new List<HttpConnection>();

		public int Port { get { return port; } }

		public void Start(int port)
		{
			if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
				throw new ArgumentOutOfRangeException("port", port, string.Format("port must be between {0} and {1}.", IPEndPoint.MinPort, IPEndPoint.MaxPort));

			if (disposed || socket != null || thread != null)
				throw new InvalidOperationException();

			this.port = port;

			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(new IPEndPoint(0, port));
			socket.Listen(10);

			thread = new Thread(new ThreadStart(Process));
			thread.Start();
		}

		public void Stop()
		{
			if (disposed)
				throw new InvalidOperationException();

			if (socket != null)
				socket.Close();

			if (thread != null)
				thread.Abort();

			socket = null;
			thread = null;
		}

		private void Process()
		{
			try
			{
				while (true)
				{
					HttpConnection connection = new HttpConnection(socket.Accept());

					lock (connections)
						connections.Add(connection);

					connection.Stopped += new EventHandler(ConnectionStopped);
					connection.Start();
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception)
			{
				StatusBarManager.AddMessage(string.Format(OrzeszekTransfer.Resources.ListenerFailedAccept, port), MessageType.Error, MessageLength.Indefinite);
			}
		}

		private void ConnectionStopped(object sender, EventArgs e)
		{
			lock (connections)
				connections.Remove((HttpConnection)sender);
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
				if (thread != null)
					thread.Abort();

				if (socket != null)
				{
					if (aborting)
						socket.LingerState = new LingerOption(true /* enable */, 0 /* seconds */);

					socket.Close();
				}

				lock (connections)
				{
					foreach (HttpConnection connection in connections)
					{
						if (aborting)
							connection.Abort();
						else 
							connection.Dispose();
					}
				}
			}
		}
	}
}