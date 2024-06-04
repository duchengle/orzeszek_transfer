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
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace OrzeszekTransfer
{
	public class MessageEventArgs : EventArgs
	{
		public string Message { get; private set; }

		public MessageEventArgs(string message)
		{
			Message = message;
		}
	}

	public static class MessageUtility
	{
		private const uint CopyDataMessage = 0x004A;

		[Flags]
		private enum TimeoutFlags : uint
		{
			Normal = 0x0000,
			Block = 0x0001,
			AbortIfHung = 0x0002,
			NoTimeoutIfNotHung = 0x0008
		}

		private struct CopyData
		{
			public IntPtr Nothing;
			public int Length;
			public IntPtr Data;
		}

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessageTimeout(IntPtr handle, uint messageType, IntPtr senderHandle, IntPtr dataPtr, TimeoutFlags timeoutFlags, uint timeout, out IntPtr result);

		public static event EventHandler<MessageEventArgs> MessageReceived;

		public static void RegisterWindow(Window window)
		{
			WindowInteropHelper interopHelper = new WindowInteropHelper(window);
			HwndSource src = HwndSource.FromHwnd(interopHelper.Handle);
			src.AddHook(new HwndSourceHook(WindowHook));
		}

		public static void SendMessage(string message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			foreach (Process proc in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
				if (proc.Id != Process.GetCurrentProcess().Id)
				{
					CopyData data = new CopyData();
					data.Nothing = IntPtr.Zero;
					data.Length = 2 * message.Length;
					data.Data = Marshal.StringToHGlobalUni(message);

					IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(CopyData)));
					Marshal.StructureToPtr(data, ptr, false);

					IntPtr res;
					SendMessageTimeout(proc.MainWindowHandle, CopyDataMessage, IntPtr.Zero, ptr, TimeoutFlags.Block | TimeoutFlags.AbortIfHung, 5000, out res);

					Marshal.FreeHGlobal(ptr);
					Marshal.FreeHGlobal(data.Data);
				}
		}

		private static IntPtr WindowHook(IntPtr handle, int messageType, IntPtr senderHandle, IntPtr dataPtr, ref bool handled)
		{
			if (messageType == CopyDataMessage)
			{
				CopyData data = (CopyData)Marshal.PtrToStructure(dataPtr, typeof(CopyData));
				string message = Marshal.PtrToStringUni(data.Data, data.Length / 2);

				if (MessageReceived != null)
					MessageReceived(null, new MessageEventArgs(message));
			}

			return IntPtr.Zero;
		}
	}
}