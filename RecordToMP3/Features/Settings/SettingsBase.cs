﻿using System;
using System.IO;
using Newtonsoft.Json;

namespace RecordToMP3.Features.Settings
{
    /// <summary>
    /// Provides a singleton with lazy loading of settings
    ///
    /// Persists to JSON
    ///
    /// Observes PropertyChange on T and saves entire object
    /// </summary>
    /// <typeparam name="T">Class containing settings</typeparam>
    public class SettingsBase<T> where T : GalaSoft.MvvmLight.ObservableObject, ISettings, new()
    {
        private static T current;

        public static T Current
        {
            get
            {
                if (current == null)
                    current = LoadCurrent();
                return current;
            }
        }

        public static void Save(T current)
        {
            var serializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            };
            File.WriteAllText(current.FILE_PATH, JsonConvert.SerializeObject(current, serializerSettings));
        }

        private static T LoadCurrent()
        {
            var newSettings = JsonConvert.DeserializeObject<T>(File.ReadAllText(new T().FILE_PATH));

            newSettings.PropertyChanged += (s, e) => Save(newSettings);

            return newSettings;
        }
    }
}