﻿#region Copyright
// // -----------------------------------------------------------------------
// // <copyright company="Chinchilla Software Limited">
// // 	Copyright Chinchilla Software Limited. All rights reserved.
// // </copyright>
// // -----------------------------------------------------------------------
#endregion

using System;

namespace Cqrs.Configuration
{
	/// <summary>
	/// Provides access to configuration settings from the app settings of an app.config or web.config... i.e. <see cref="System.Configuration.ConfigurationManager.AppSettings"/>
	/// </summary>
	public class ConfigurationManager : IConfigurationManager
	{
		#region Implementation of IConfigurationManager

		/// <summary>
		/// Read the setting named <paramref name="key"/>.
		/// </summary>
		/// <param name="key">The key (or name) of the setting to read.</param>
		public virtual string GetSetting(string key)
		{
			return System.Configuration.ConfigurationManager.AppSettings[key];
		}

		/// <summary>
		/// Read the setting named <paramref name="key"/>.
		/// </summary>
		/// <param name="key">The key (or name) of the setting to read.</param>
		/// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns>true if the an element with the specified key exists; otherwise, false.</returns>
		public virtual bool TryGetSetting(string key, out string value)
		{
			try
			{
				value = GetSetting(key);
				return true;
			}
			catch (Exception)
			{
				value = null;
				return false;
			}
		}

		/// <summary>
		/// Read the setting named <paramref name="key"/>.
		/// </summary>
		/// <param name="key">The key (or name) of the setting to read.</param>
		/// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns>true if the an element with the specified key exists; otherwise, false.</returns>
		public virtual bool TryGetSetting(string key, out bool value)
		{
			string rawValue;
			if (TryGetSetting(key, out rawValue))
			{
				if (bool.TryParse(rawValue, out value))
				{
					return true;
				}
			}
			value = false;
			return false;
		}

		#endregion
	}
}