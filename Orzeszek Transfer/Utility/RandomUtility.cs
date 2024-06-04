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
using System.Security.Cryptography;
using System.Text;

namespace OrzeszekTransfer
{
	public static class RandomUtility
	{
		private static RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();

		public static string GetRandomString(int length)
		{
			byte[] output = new byte[length];
			byte[] buffer = new byte[length];
			random.GetBytes(buffer);

			for (int i = 0, j = 0; i < length; j++)
			{
				if (j == buffer.Length)
				{
					random.GetBytes(buffer);
					j = 0;
				}

				if (buffer[j] >= 0x30 && buffer[j] <= 0x39 || buffer[j] >= 0x61 && buffer[j] <= 0x7A)
				{
					output[i] = buffer[j];
					i++;
				}
			}

			return Encoding.UTF8.GetString(output);
		}
	}
}