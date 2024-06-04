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
using System.Text;

namespace OrzeszekTransfer
{
	public static class SharedFileManager
	{
		private static Dictionary<string, SharedFile> fileShares = new Dictionary<string, SharedFile>();

		public static SharedFile Add(string fullName)
		{
			FileInfo fi = new FileInfo(fullName);

			if (!fi.Exists)
				throw new FileNotFoundException();

			string id = GetNextRandomID();
			return Add(fi.FullName, fi.Name, id, fi.Length);
		}

		public static SharedFile Add(SharedFileInfo info)
		{
			FileInfo fi = new FileInfo(info.FullName);

			if (!fi.Exists)
				throw new FileNotFoundException();

			return Add(info.FullName, info.Name, info.ID, fi.Length);
		}

		private static SharedFile Add(string fullName, string name, string id, long size)
		{
			SharedFile fileShare = new SharedFile();

			fileShare.FullName = fullName;
			fileShare.Name = name;
			fileShare.ID = id;
			fileShare.Size = size;

			lock (fileShares)
			{
				while (fileShares.ContainsKey(fileShare.ID))
					fileShare.ID = GetNextRandomID();

				fileShares.Add(fileShare.ID, fileShare);
			}

			return fileShare;
		}

		private static string GetNextRandomID()
		{
			int length = Math.Max(Settings.Default.FileShareIdLength, 4);
			return RandomUtility.GetRandomString(length);
		}

		public static SharedFile Get(string id)
		{
			lock (fileShares)
			{
				SharedFile sf = null;
				fileShares.TryGetValue(id, out sf);
				return sf;
			}
		}

		public static SharedFile GetByFileName(string filename)
		{
			string fullName = Path.GetFullPath(filename);

			lock (fileShares)
			{
				foreach (SharedFile sf in fileShares.Values)
				{
					if (sf.FullName == fullName)
						return sf;
				}
				return null;
			}
		}

		public static IEnumerable<SharedFile> GetSharedFiles()
		{
			lock (fileShares)
			{
				return fileShares.Values.ToArray();
			}
		}

		public static void Remove(SharedFile sf)
		{
			lock (fileShares)
				fileShares.Remove(sf.ID);

			sf.Abort();
		}

		public static void LoadFromSettings()
		{
			if (Settings.Default.SharedFiles == null)
				return;

			foreach (SharedFileInfo info in Settings.Default.SharedFiles)
			{
				try
				{
					Add(info);
				}
				catch
				{
					// If we can't restore a saved share, just skip it
				}
			}
		}

		public static void SaveToSettings()
		{
			try
			{
				SharedFileInfoList list = new SharedFileInfoList();
				list.AddRange(fileShares.Values.Select(SharedFile.ToSharedFileInfo));
				Settings.Default.SharedFiles = list;
				Settings.Default.Save();
			}
			catch
			{
			}
		}
	}
}