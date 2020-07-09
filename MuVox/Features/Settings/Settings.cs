﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace TTech.MuVox.Features.Settings
{
    public class Settings : GalaSoft.MvvmLight.ObservableObject, ISettings
    {
        private const string PROCESSOR = "Processor";
        private const string RECORDER = "Recorder";
        private const string UX = "UX";

        public static Settings Current => SettingsBase<Settings>.Current;

        public static Action Save => () => SettingsBase<Settings>.Save();

        public string FILE_PATH => "Settings.json";

        public bool AutoSave => false;

        public IEnumerable<string> Verify()
        {
            if (Add_Jingle != JingleAdding.None)
            {
                if (string.IsNullOrEmpty(Jingle_Path) || !File.Exists(Jingle_Path))
                    yield return $"'{nameof(Jingle_Path)}' must be a valid file when '{nameof(Add_Jingle)}' is not '{nameof(JingleAdding.None)}'";
            }
        }

        [Category(PROCESSOR)]
        [DisplayName("Output path")]
        public string Processor_OutputPath { get; set; } = string.Empty;

        [Category(PROCESSOR)]
        [DisplayName("MP3 Quality")]
        public int Processor_Mp3Quality { get; set; } = 160;

        [Category(PROCESSOR)]
        [DisplayName("Jingle Path")]
        public string Jingle_Path { get; set; } = string.Empty;

        [Category(PROCESSOR)]
        [DisplayName("Jingle Adding")]
        public JingleAdding Add_Jingle { get; set; } = JingleAdding.None;

        [Category(RECORDER)]
        [DisplayName("Minutes on pregressbar")]
        public uint Recorder_MinutesOnProgressbar { get; set; } = 200;

        [Category(RECORDER)]
        [DisplayName("Filename")]
        public string Recorder_FileName { get; set; } = "MuVox {0:yyyy-MM-dd HHmmss}";

        [Category(RECORDER)]
        [DisplayName("Output path")]
        public string Recorder_OutputPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MuVox");

        [Category(UX)]
        [DisplayName("Volumemeter, Peekmark fallbackspeed")]
        public int UX_VolumeMeter_PeakMarkFallBackSpeed { get; set; } = 2;

        [Category(UX)]
        [DisplayName("Volumemeter, Peekmark holdtime (ms)")]
        public int UX_VolumeMeter_PeakMarkHoldTime { get; set; } = 200;

        [Category(UX)]
        [DisplayName("Volumemeter, MinDb")]
        public float UX_VolumeMeter_MinDb { get; set; } = -24;

        [Category(UX)]
        [DisplayName("Volumemeter, MaxDb")]
        public float UX_VolumeMeter_MaxDb { get; set; } = 2;

        [Category(UX)]
        [DisplayName("Volumemeter, Num of samples")]
        public byte UX_VolumeMeter_NoSamples { get; set; } = 8;

        [Browsable(false)]
        public string Recorder_LastFile { get; set; } = string.Empty;
    }
}
