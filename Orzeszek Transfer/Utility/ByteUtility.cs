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
	public static class ByteUtility
	{
		private static string[] sizeFormats = { OrzeszekTransfer.Resources.SizeB, OrzeszekTransfer.Resources.SizeKB, OrzeszekTransfer.Resources.SizeMB, OrzeszekTransfer.Resources.SizeGB, OrzeszekTransfer.Resources.SizeTB, OrzeszekTransfer.Resources.SizePB, OrzeszekTransfer.Resources.SizeEB, OrzeszekTransfer.Resources.SizeZB, OrzeszekTransfer.Resources.SizeYB };
		private static string[] speedFormats = { OrzeszekTransfer.Resources.SpeedB, OrzeszekTransfer.Resources.SpeedKB, OrzeszekTransfer.Resources.SpeedMB, OrzeszekTransfer.Resources.SpeedGB, OrzeszekTransfer.Resources.SpeedTB, OrzeszekTransfer.Resources.SpeedPB, OrzeszekTransfer.Resources.SpeedEB, OrzeszekTransfer.Resources.SpeedZB, OrzeszekTransfer.Resources.SpeedYB };

		public static string ToSizeString(this long size)
		{
			int i = 0;
			double s = size;

			while (i < sizeFormats.Length && s > 1024)
			{
				i++;
				s /= 1024;
			}

			return string.Format(sizeFormats[i], s);
		}

		public static string ToSizeString(this double size)
		{
			int i = 0;

			while (i < sizeFormats.Length && size > 1024)
			{
				i++;
				size /= 1024;
			}

			return string.Format(sizeFormats[i], size);
		}

		public static string ToSpeedString(this long speed)
		{
			int i = 0;
			double s = speed;

			while (i < speedFormats.Length && s > 1024)
			{
				i++;
				s /= 1024;
			}

			return string.Format(speedFormats[i], s);
		}

		public static string ToSpeedString(this double speed)
		{
			int i = 0;

			while (i < speedFormats.Length && speed > 1024)
			{
				i++;
				speed /= 1024;
			}

			return string.Format(speedFormats[i], speed);
		}
	}
}