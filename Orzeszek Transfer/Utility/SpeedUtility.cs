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
using System.Threading;

namespace OrzeszekTransfer
{
	public interface ISpeedTrackable
	{
		long Position { get; }
		double Speed { set; }
	}

	public static class SpeedUtility
	{
		private const int averagePeriod = 15;

		private static Dictionary<ISpeedTrackable, List<long>> objects = new Dictionary<ISpeedTrackable, List<long>>();
		private static Thread thread;

		static SpeedUtility()
		{
			thread = new Thread(new ThreadStart(Process));
			thread.Start();
		}

		public static void Dispose()
		{
			if (thread != null)
			{
				thread.Abort();
				thread = null;
			}
		}

		public static void StartTracking(ISpeedTrackable o)
		{
			lock (objects)
				objects.Add(o, new List<long>());

			o.Speed = 0;
		}

		public static void StopTracking(ISpeedTrackable o)
		{
			lock (objects)
				objects.Remove(o);

			o.Speed = 0;
		}

		private static void Process()
		{
			while (true)
			{
				lock (objects)
					foreach (KeyValuePair<ISpeedTrackable, List<long>> kvp in objects)
					{
						ISpeedTrackable o = kvp.Key;
						List<long> positions = kvp.Value;

						if (positions.Count > averagePeriod)
							positions.RemoveAt(0);

						positions.Add(o.Position);

						if (positions.Count > 1)
							o.Speed = (double)(positions[positions.Count - 1] - positions[0]) / (positions.Count - 1);
					}

				Thread.Sleep(1000);
			}
		}
	}
}