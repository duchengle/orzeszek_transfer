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
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OrzeszekTransfer
{
	public class HttpConnection : IDisposable, ISpeedTrackable
	{
		private class HttpGetRequest
		{
			public string ID { get; set; }
			public long Start { get; set; }
			public long End { get; set; }
		}

		private bool disposed;

		private Socket socket;
		private StreamReader streamReader;
		private Thread thread;

		public bool IsFailed { get; private set; }
		public long Position { get; private set; }
		public double Speed { get; set; }
		public object Tag { get; set; }

		public event EventHandler Stopped;

		public HttpConnection(Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException("socket");

			this.socket = socket;
			this.streamReader = new StreamReader(new NetworkStream(socket));
		}

		public void Start()
		{
			if (disposed || thread != null)
				throw new InvalidOperationException();

			thread = new Thread(new ThreadStart(Process));
			thread.Start();
		}

		private void Process()
		{
			try
			{
				HttpGetRequest request = GetRequest();
				if (request != null)
				{
					SharedFile sf = SharedFileManager.Get(request.ID);
					if (sf != null)
						if (CheckRanges(request, sf))
							UploadFile(request, sf);
						else
							SendErrorBadRequest();
					else
						SendErrorNotFound();
				}
				else
				{
					IsFailed = true;
				}
			}
			catch (Exception)
			{
				IsFailed = true;
			}

			if (Stopped != null)
				Stopped(this, new EventArgs());
		}

		private HttpGetRequest GetRequest()
		{
			List<string> headers = new List<string>();
			for (string line = streamReader.ReadLine(); !string.IsNullOrEmpty(line); line = streamReader.ReadLine())
				headers.Add(line);

			if (headers.Count == 0)
			{
				SendErrorBadRequest();
				return null;
			}
			else if (headers[0].StartsWith("GET "))
			{
				string[] parts = headers[0].Split(' ');

				if (parts.Length < 3)
				{
					SendErrorBadRequest();
					return null;
				}
				else
				{
					string[] file = parts[1].Split('/');

					if (file.Length != 3 || file[0].Length != 0 || file[1].Length == 0 || file[2].Length == 0)
					{
						SendErrorNotFound();
						return null;
					}
					else
					{
						HttpGetRequest request = new HttpGetRequest();

						request.ID = file[1].ToLowerInvariant();
						request.Start = -1;
						request.End = -1;

						try
						{
							foreach (string line in headers)
								if (line.StartsWith("Range: bytes="))
									if (line.Contains(','))
										SendErrorNotImplemented();
									else
									{
										string[] startEnd = line.Substring("Range: bytes=".Length).Split('-');

										if (startEnd.Length != 2)
											SendErrorBadRequest();
										else
										{
											request.Start = startEnd[0].Length == 0 ? -1 : long.Parse(startEnd[0]);
											request.End = startEnd[1].Length == 0 ? -1 : long.Parse(startEnd[1]);
										}

										break;
									}
						}
						catch (Exception)
						{
							SendErrorBadRequest();
							return null;
						}

						return request;
					}
				}
			}
			else
			{
				SendErrorMethodNotAllowed();
				return null;
			}
		}

		private bool CheckRanges(HttpGetRequest request, SharedFile sf)
		{
			if (request.Start < -1 || request.End < -1)
				return false;
			else if (request.Start == -1 && request.End == -1)
			{
				request.Start = 0;
				request.End = sf.Size - 1;
			}
			else if (request.End == -1)
				request.End = sf.Size - 1;
			else if (request.Start == -1)
				request.Start = sf.Size - request.End;

			if (request.Start > request.End)
				return false;

			return true;
		}

		private void UploadFile(HttpGetRequest request, SharedFile sf)
		{
			try
			{
				using (FileStream fileStream = new FileStream(sf.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
					if (fileStream.Length == sf.Size && fileStream.Seek(request.Start, SeekOrigin.Begin) == request.Start)
					{
						sf.OnUploadStarted(this);

						try
						{
							if (sf.IsDisposed)
								SendErrorNotFound();
							else if (request.Start == 0 && request.End == sf.Size - 1)
								SendFileHeaders(sf.Created, sf.ID, sf.Size);
							else
								SendFileHeaders(sf.Created, sf.ID, request.Start, request.End, sf.Size);

							byte[] buf = new byte[4096];
							long left = request.End - request.Start + 1;

							SpeedUtility.StartTracking(this);

							while (left > 0)
							{
								int len = fileStream.Read(buf, 0, (int)Math.Min(left, buf.Length));
								socket.Send(buf, len, SocketFlags.None);
								left -= len;

								Position = fileStream.Position;
							}

							SpeedUtility.StopTracking(this);
						}
						catch (Exception)
						{
							IsFailed = true;
						}

						sf.OnUploadStopped(this);
					}
					else
					{
						SendErrorNotFound();
						StatusBarManager.AddMessage(string.Format(OrzeszekTransfer.Resources.TransferFailedFileMismatch, sf.Name), MessageType.Error, MessageLength.Medium);
					}
			}
			catch (FileNotFoundException)
			{
				SendErrorNotFound();
				StatusBarManager.AddMessage(string.Format(OrzeszekTransfer.Resources.TransferFailedNotFound, sf.Name), MessageType.Error, MessageLength.Medium);
			}
			catch (Exception)
			{
				SendErrorInternalServerError();
				StatusBarManager.AddMessage(string.Format(OrzeszekTransfer.Resources.TransferFailed, sf.Name), MessageType.Error, MessageLength.Medium);
			}
			finally
			{
				Dispose();
			}
		}

		private string GetErrorXhtml(string error)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			sb.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\">");
			sb.Append("<head>");
			sb.Append("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />");
			sb.Append("<title>Orzeszek Transfer</title>");
			sb.Append("<style type=\"text/css\">");
			sb.Append("body");
			sb.Append("{");
			sb.Append("background: #fff;");
			sb.Append("color: #000;");
			sb.Append("font-family: Arial, Helvetica, sans-serif;");
			sb.Append("font-size: 13p;");
			sb.Append("margin: 72pt;");
			sb.Append("text-align: center;");
			sb.Append("}");
			sb.Append("a");
			sb.Append("{");
			sb.Append("color: #2361a1;");
			sb.Append("text-decoration: none;");
			sb.Append("}");
			sb.Append("a:hover");
			sb.Append("{");
			sb.Append("text-decoration: underline;");
			sb.Append("}");
			sb.Append("p.link");
			sb.Append("{");
			sb.Append("font-size: 9pt;");
			sb.Append("}");
			sb.Append("</style>");
			sb.Append("</head>");
			sb.Append("<body>");
			sb.Append("<p>" + error + "</p>");
			sb.Append("<p class=\"link\"><a href=\"http://www.orzeszek.org/dev/transfer/\">Orzeszek Transfer</a></p>");
			sb.Append("</body>");
			sb.Append("</html>");
			return sb.ToString();
		}

		private string GetHttpDate()
		{
			return DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT";
		}

		private string GetHttpDate(DateTime date)
		{
			return date.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT";
		}

		private void SendFileHeaders(DateTime date, string fileId, long length)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("HTTP/1.1 200 OK");
			sb.AppendLine("Date: " + GetHttpDate(date));
			sb.AppendLine("ETag: " + fileId);
			sb.AppendLine("Cache-Control: s-maxage=0, proxy-revalidate");
			sb.AppendLine("Content-Type: application/octet-stream");
			sb.AppendLine("Content-Length: " + length.ToString());
			sb.AppendLine("Connection: close");
			sb.AppendLine();

			byte[] headers = Encoding.ASCII.GetBytes(sb.ToString());
			socket.Send(headers);
		}

		private void SendFileHeaders(DateTime date, string fileId, long from, long to, long totalLength)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("HTTP/1.1 206 Partial Content");
			sb.AppendLine("Date: " + GetHttpDate(date));
			sb.AppendLine("ETag: " + fileId);
			sb.AppendLine("Cache-Control: s-maxage=0, proxy-revalidate");
			sb.AppendLine("Content-Type: application/octet-stream");
			sb.AppendLine("Content-Range: bytes " + from.ToString() + "-" + to.ToString() + "/" + totalLength.ToString());
			sb.AppendLine("Content-Length: " + (to - from + 1).ToString());
			sb.AppendLine("Connection: close");
			sb.AppendLine();

			byte[] headers = Encoding.ASCII.GetBytes(sb.ToString());
			socket.Send(headers);
		}

		private void SendErrorBadRequest()
		{
			byte[] content = Encoding.UTF8.GetBytes(GetErrorXhtml("400 Bad Request: <strong>The request was malformed.</strong>"));

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("HTTP/1.1 400 Bad Request");
			sb.AppendLine("Date: " + GetHttpDate());
			sb.AppendLine("Cache-Control: no-cache");
			sb.AppendLine("Content-Type: text/html");
			sb.AppendLine("Content-Length: " + content.Length.ToString());
			sb.AppendLine("Connection: close");
			sb.AppendLine();

			byte[] headers = Encoding.ASCII.GetBytes(sb.ToString());
			socket.Send(headers);
			socket.Send(content);

			Dispose();
		}

		private void SendErrorNotFound()
		{
			byte[] content = Encoding.UTF8.GetBytes(GetErrorXhtml("404 Not Found: <strong>The requested file was not found.</strong>"));

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("HTTP/1.1 404 Not Found");
			sb.AppendLine("Date: " + GetHttpDate());
			sb.AppendLine("Cache-Control: no-cache");
			sb.AppendLine("Content-Type: text/html");
			sb.AppendLine("Content-Length: " + content.Length.ToString());
			sb.AppendLine("Connection: close");
			sb.AppendLine();

			byte[] headers = Encoding.ASCII.GetBytes(sb.ToString());
			socket.Send(headers);
			socket.Send(content);

			Dispose();
		}

		private void SendErrorRequestedRangeNotSatisfiable(long length)
		{
			byte[] content = Encoding.UTF8.GetBytes(GetErrorXhtml("416 Requested Range Not Satisfiable: <strong>The client tried to resume the file at an invalid offset.</strong>"));

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("HTTP/1.1 416 Requested Range Not Satisfiable");
			sb.AppendLine("Date: " + GetHttpDate());
			sb.AppendLine("Content-Range: bytes */" + length.ToString());
			sb.AppendLine("Cache-Control: no-cache");
			sb.AppendLine("Content-Type: text/html");
			sb.AppendLine("Content-Length: " + content.Length.ToString());
			sb.AppendLine("Connection: close");
			sb.AppendLine();

			byte[] headers = Encoding.ASCII.GetBytes(sb.ToString());
			socket.Send(headers);
			socket.Send(content);

			Dispose();
		}

		private void SendErrorMethodNotAllowed()
		{
			byte[] content = Encoding.UTF8.GetBytes(GetErrorXhtml("405 Method Not Allowed: <strong>This server supports only the GET method.</strong>"));

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("HTTP/1.1 405 Method Not Allowed");
			sb.AppendLine("Date: " + GetHttpDate());
			sb.AppendLine("Accept: GET");
			sb.AppendLine("Cache-Control: no-cache");
			sb.AppendLine("Content-Type: text/html");
			sb.AppendLine("Content-Length: " + content.Length.ToString());
			sb.AppendLine("Connection: close");
			sb.AppendLine();

			byte[] headers = Encoding.ASCII.GetBytes(sb.ToString());
			socket.Send(headers);
			socket.Send(content);

			Dispose();
		}

		private void SendErrorInternalServerError()
		{
			byte[] content = Encoding.UTF8.GetBytes(GetErrorXhtml("500 Internal Server Error: <strong>An unknown error occurred on the server.</strong>"));

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("HTTP/1.1 500 Internal Server Error");
			sb.AppendLine("Date: " + GetHttpDate());
			sb.AppendLine("Cache-Control: no-cache");
			sb.AppendLine("Content-Type: text/html");
			sb.AppendLine("Content-Length: " + content.Length.ToString());
			sb.AppendLine("Connection: close");
			sb.AppendLine();

			byte[] headers = Encoding.ASCII.GetBytes(sb.ToString());
			socket.Send(headers);
			socket.Send(content);

			Dispose();
		}

		private void SendErrorNotImplemented()
		{
			byte[] content = Encoding.UTF8.GetBytes(GetErrorXhtml("501 Not Implemented: <strong>The server does not support the requested operation.</strong>"));

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("HTTP/1.1 501 Not Implemented");
			sb.AppendLine("Date: " + GetHttpDate());
			sb.AppendLine("Cache-Control: no-cache");
			sb.AppendLine("Content-Type: text/html");
			sb.AppendLine("Content-Length: " + content.Length.ToString());
			sb.AppendLine("Connection: close");
			sb.AppendLine();

			byte[] headers = Encoding.ASCII.GetBytes(sb.ToString());
			socket.Send(headers);
			socket.Send(content);

			Dispose();
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
				if (thread != null && thread != Thread.CurrentThread)
					thread.Abort();

				if (streamReader != null)
					streamReader.Close();

				if (socket != null)
				{
					if (aborting)
						socket.LingerState = new LingerOption(true /* enable */, 0 /* seconds */);

					socket.Close();
				}
			}
		}
	}
}