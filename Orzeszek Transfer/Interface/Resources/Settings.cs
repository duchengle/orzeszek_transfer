﻿//
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
using System.Configuration;

namespace OrzeszekTransfer
{
	internal sealed partial class Settings
	{
		public Settings()
		{
			// If we have an Orzeszek Transfer.xml, use the PortableSettingsProvider instead of the default
			if (PortableSettingsProvider.IsPortable)
			{
				// Cache a PortableSettingsProvider if it doesn't exist
				if (Providers[typeof(PortableSettingsProvider).FullName] == null)
					Providers.Add(new PortableSettingsProvider());

				// Set the settings provider on all properties
				SettingsProvider provider = Providers[typeof(PortableSettingsProvider).FullName];
				foreach (SettingsProperty property in Properties)
				{
					if (property.PropertyType.GetCustomAttributes(typeof(SettingsProviderAttribute), false /* inherit */).Length == 0)
					{
						property.Provider = provider;
					}
				}
			}
		}
	}
}