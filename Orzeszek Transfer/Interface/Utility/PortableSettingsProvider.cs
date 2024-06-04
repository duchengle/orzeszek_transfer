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
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace OrzeszekTransfer
{
	public class PortableSettingsProvider : SettingsProvider
	{
		private static readonly object SyncRoot = new object();

		public override string ApplicationName
		{
			get { return Assembly.GetExecutingAssembly().GetName().Name; }
			set { /* Do nothing */ }
		}

		public override string Name
		{
			get { return GetType().FullName; }
		}

		public static bool IsPortable
		{
			get
			{
#if PORTABLE
				return true;
#else
				return false;
#endif
			}
		}

		public static string SettingsPath
		{
			get
			{
				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				FileInfo fi = new FileInfo(executingAssembly.Location);
				return Path.Combine(fi.DirectoryName, Path.GetFileNameWithoutExtension(fi.Name) + ".xml");
			}
		}

		public override void Initialize(string name, NameValueCollection config)
		{
			base.Initialize(ApplicationName, config);
		}

		public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
		{
			lock (SyncRoot)
			{
				// Load the XML
				XmlDocument xml = new XmlDocument();
				try
				{
					xml.Load(SettingsPath);
				}
				catch
				{
					xml = null;
				}

				// Populate the settings collection
				SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
				foreach (SettingsProperty property in collection)
				{
					SettingsPropertyValue value = new SettingsPropertyValue(property);
					value.SerializedValue = property.DefaultValue;
					if (xml != null)
					{
						string xPathQuery = string.Format("//setting[@name='{0}']/value", property.Name);
						XmlNode node = xml.SelectSingleNode(xPathQuery);
						if (node != null)
						{
							value.SerializedValue = node.InnerXml;
						}
					}
					values.Add(value);
				}
				return values;
			}
		}

		public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
		{
			lock (SyncRoot)
			{
				// Sort and filter the settings
				List<SettingsPropertyValue> sortedAndFilteredCollection = new List<SettingsPropertyValue>();
				foreach (SettingsPropertyValue propertyValue in collection)
				{
					if (propertyValue.Property.SerializeAs == SettingsSerializeAs.String || propertyValue.Property.SerializeAs == SettingsSerializeAs.Xml)
					{
						sortedAndFilteredCollection.Add(propertyValue);
					}
				}
				sortedAndFilteredCollection.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

				// Write the XML
				using (XmlTextWriter writer = new XmlTextWriter(SettingsPath, Encoding.UTF8))
				{
					writer.Formatting = Formatting.Indented;
					writer.IndentChar = ' ';
					writer.Indentation = 4;

					// <?xml version="1.0" encoding="utf-8"?>
					writer.WriteStartDocument();
					// <configuration>
					writer.WriteStartElement("configuration");
					// <userSettings>
					writer.WriteStartElement("userSettings");
					// <OrzeszekTransfer.Settings>
					writer.WriteStartElement("OrzeszekTransfer.Settings");
					foreach (SettingsPropertyValue propertyValue in sortedAndFilteredCollection)
					{
						// <setting name="..." serializeAs="...">
						writer.WriteStartElement("setting");
						writer.WriteAttributeString("name", propertyValue.Name);
						writer.WriteAttributeString("serializeAs", propertyValue.Property.SerializeAs.ToString());
						// <value>
						writer.WriteStartElement("value");
						if (propertyValue.Property.SerializeAs == SettingsSerializeAs.String)
						{
							writer.WriteValue(propertyValue.SerializedValue.ToString());
						}
						else
						{
							XmlSerializer serializer = new XmlSerializer(propertyValue.Property.PropertyType);
							serializer.Serialize(writer, propertyValue.PropertyValue);
						}
						// </value>
						writer.WriteEndElement();
						// </setting>
						writer.WriteEndElement();
					}
					// </OrzeszekTransfer.Settings>
					writer.WriteEndElement();
					// </userSettings>
					writer.WriteEndElement();
					// </configuration>
					writer.WriteEndElement();
				}
			}
		}
	}
}