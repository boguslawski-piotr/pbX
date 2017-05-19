﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;

//using Xamarin.Forms;
//using Xamarin.Forms.Xaml;

namespace pbXNet
{
    public static class LocalizationManager
    {
        // TODO: umozliwic dodanie dwolnej liczby zasobow i wtedy wyszukiwanie tekstow po kolei, w kolejnosci dodawania

        static string _BaseName { get; set; }

        static Assembly _Assembly { get; set; }

        public static void AddResources(string BaseName, Assembly Assembly)
        {
            _BaseName = BaseName;
            _Assembly = Assembly;
        }

        static Locale _Locale = new Locale();

        public static CultureInfo CurrentCultureInfo
        {
            get {
                CultureInfo c = _Locale.GetCurrentCultureInfo();
                _Locale.SetLocale(c);
                return c;
            }
        }

        static CultureInfo _Culture;

        public static CultureInfo Culture
        {
            get {
                if (_Culture == null)
                    _Culture = CurrentCultureInfo;
                return _Culture;
            }
            set {
                _Culture = value;
            }
        }

        static ResourceManager _ResourceManager;

		public static ResourceManager ResourceManager
        {
            get {
                if (object.ReferenceEquals(_ResourceManager, null))
                {
                    ResourceManager temp = new ResourceManager(_BaseName, _Assembly);
                    _ResourceManager = temp;
                }
                return _ResourceManager;
            }
        }

        public static string GetText(string name)
        {
            if (name == null)
                return "";

            string value = null;

            if (ResourceManager != null)
                try
                {
                    value = ResourceManager.GetString(name, Culture);
                }
                catch { }

            if (value == null)
                value = $"!@ {name} @!"; // returns the key, which GETS DISPLAYED TO THE USER

            return value;
        }
    }

    /// <summary>
    /// An auxiliary class used to the intuitive use of localized text in the code.
    /// Example: string s = T.Localized("text id");
    /// </summary>
    public static class T
    {
        public static string Localized(string name)
        {
            return LocalizationManager.GetText(name);
        }
    }
}