﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace pbXNet
{
	public static class LocalizationManager
	{
		static Locale _locale = new Locale();

		public static CultureInfo CurrentCultureInfo
		{
			get {
				CultureInfo c = _locale.GetCurrentCultureInfo();
				_locale.SetLocale(c);
				return c;
			}
		}

		static CultureInfo _cultureInfo;

		public static CultureInfo CultureInfo
		{
			get {
				if (_cultureInfo == null)
					_cultureInfo = CurrentCultureInfo;
				return _cultureInfo;
			}
			set {
				_cultureInfo = value;
				_locale.SetLocale(_cultureInfo);
			}
		}

		class Resource
		{
			public string BaseName { get; set; }
			public Assembly Assembly { get; set; }

			ResourceManager _ResourceManager;
			public ResourceManager ResourceManager
			{
				get {
					if (object.ReferenceEquals(_ResourceManager, null))
					{
						ResourceManager rm = new ResourceManager(BaseName, Assembly);
						_ResourceManager = rm;
					}
					return _ResourceManager;
				}

			}
		}

		static readonly Lazy<ConcurrentBag<Resource>> _resources = new Lazy<ConcurrentBag<Resource>>(() => new ConcurrentBag<Resource>(), true);
		static volatile int _numberOfInstalledDefaultResources = 0;

		public static void AddResource(string baseName, Assembly assembly)
		{
			Resource resource = new Resource()
			{
				BaseName = baseName,
				Assembly = assembly,
			};

			_resources.Value.Add(resource);
		}

		public static void AddDefaultResources()
		{
			if (_numberOfInstalledDefaultResources == 0)
			{
				AddResource("pbXNet.Exceptions.T", typeof(pbXNet.LocalizationManager).GetTypeInfo().Assembly);
				AddResource("pbXNet.Texts.T", typeof(pbXNet.LocalizationManager).GetTypeInfo().Assembly);
				_numberOfInstalledDefaultResources = 2;
			}
		}

		public static string Localized(string name, params string[] args)
		{
			if (name == null)
				return "";

			AddDefaultResources();

			string value = null;

			if (_resources.Value.Count > 0)
			{
				foreach (var r in _resources.Value)
				{
					try
					{
						value = r.ResourceManager.GetString(name, CultureInfo);
						if (value != null)
						{
							if (args.Length > 0)
								value = string.Format(value, args);
							break;
						}
					}
					catch { }
				}
			}

			if (value == null)
				value = $"!@ {name} @!"; // returns the key, which GETS DISPLAYED TO THE USER

			return value;
		}
	}

	/// <summary>
	/// An auxiliary class used to the intuitive use of localized texts in the code.
	/// <example>
	/// <code>string s = Localized.T("text id");</code>
	/// </example>
	/// </summary>
	public static class Localized
	{
		public static string T(string name, params string[] args)
		{
			return LocalizationManager.Localized(name, args);
		}
	}
}
