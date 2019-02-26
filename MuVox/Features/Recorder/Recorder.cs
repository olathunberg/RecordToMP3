﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using NAudio.Lame;
using NAudio.Wave;

namespace TTech.Muvox.Features.Recorder
{
    public class Recorder : GalaSoft.MvvmLight.ObservableObject, ICleanup, IDisposable
    {
        #region Fields
        private WaveIn waveIn;
        private WaveFileWriter writer;
        private RecordingState recordingState;
        private string outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MuVox");
        private string outputFilenameBase;
        private Settings.Settings Settings { get { return Features.Settings.SettingsBase<Settings.Settings>.Current; } }
        #endregion

        #region Constructors
        public Recorder()
        {
            recordingState = RecordingState.Monitoring;

            waveIn = new WaveIn();
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveIn.RecordingStopped += waveIn_RecordingStopped;
            waveIn.BufferMilliseconds = 15;
            waveIn.WaveFormat = new WaveFormat(44100, 16, 2);

            if (!(bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue))
                waveIn.StartRecording();
        }
        #endregion

        #region Events
        private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (recordingState == RecordingState.Recording)
            {
                Task.Run(async () =>
                    {
                        await writer.WriteAsync(e.Buffer, 0, e.BytesRecorded);
                    });
            }
            else if (recordingState == RecordingState.RequestedStop)
            {
                recordingState = RecordingState.Monitoring;
                if (writer != null)
                {
                    writer.Dispose();
                    writer = null;
                }
            }

            float maxL = 0;
            float minL = 0;
            float maxR = 0;
            float minR = 0;

            for (int index = 0; index < e.BytesRecorded; index += 4)
            {
                var sample = (short)((e.Buffer[index + 1] << 8) | (e.Buffer[index]));
                var sample32 = sample / 32768f;

                maxL = Math.Max(sample32, maxL);
                minL = Math.Min(sample32, minL);

                sample = (short)((e.Buffer[index + 3] << 8) | (e.Buffer[index + 2]));
                sample32 = sample / 32768f;

                maxR = Math.Max(sample32, maxR);
                minR = Math.Min(sample32, minR);
            }

            if (NewSample != null)
                NewSample(minL, maxL, minR, maxR);

            RaisePropertyChanged(() => TenthOfSecondsRecorded);
        }

        private void waveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }

            RaisePropertyChanged(() => TenthOfSecondsRecorded);

            if (e.Exception != null)
            {
                //MessageBox.Show(String.Format("A problem was encountered during recording {0}",
                //                              e.Exception.Message));
            }

        }
        #endregion

        #region Private methods
        private void ConvertWavStreamToMp3File(ref MemoryStream ms, string savetofilename)
        {
            ms.Seek(0, SeekOrigin.Begin);

            using (var rdr = new WaveFileReader(ms))
            using (var wtr = new LameMP3FileWriter(savetofilename, rdr.WaveFormat, LAMEPreset.VBR_90))
            {
                rdr.CopyTo(wtr);
            }
        }

        private int GetTenthOfSecondsRecorded()
        {
            if (writer != null)
                return (int)(writer.Length / (writer.WaveFormat.AverageBytesPerSecond / 10));
            else
                return 0;
        }
        #endregion

        #region Properties
        public Action<float, float, float, float> NewSample { get; set; }

        public RecordingState RecordingState { get { return recordingState; } }

        public ObservableCollection<int> Markers { get; set; }

        public int TenthOfSecondsRecorded
        {
            get { return GetTenthOfSecondsRecorded(); }
        }
        #endregion

        #region Public methods
        public void SetMarker()
        {
            Markers.Add(GetTenthOfSecondsRecorded());

            var fileName = Path.Combine(outputFolder, outputFilenameBase) + ".markers";
            new Marker.Marker().AddMarkerToFile(fileName, Markers.Last());

            RaisePropertyChanged(() => Markers);
        }

        public void StartRecording()
        {
            if (writer == null)
            {
                outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MuVox");
                Directory.CreateDirectory(outputFolder);
                outputFilenameBase = String.Format(Settings.Recorder_FileName, DateTime.Now);
                writer = new WaveFileWriter(Path.Combine(outputFolder, outputFilenameBase) + ".wav", waveIn.WaveFormat);

                MuVox.Properties.Settings.Default.RECORDER_LastFile = writer.Filename;
                MuVox.Properties.Settings.Default.Save();

                if (Markers == null)
                    Markers = new ObservableCollection<int>();

                Markers.Clear();
            }

            switch (recordingState)
            {
                case RecordingState.Monitoring:
                    recordingState = RecordingState.Recording;
                    break;
                case RecordingState.Recording:
                    recordingState = RecordingState.Paused;
                    break;
                case RecordingState.Paused:
                    recordingState = RecordingState.Recording;
                    break;
            }
        }

        public void StopRecording()
        {
            recordingState = RecordingState.RequestedStop;
            if (Markers != null)
                Markers.Clear();
        }

        public void Cleanup()
        {
            waveIn.StopRecording();
            waveIn.Dispose();
            waveIn = null;
        }

        #region IDisposable Support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (waveIn != null)
                        waveIn.Dispose();
                    if (writer != null)
                        writer.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
        #endregion
    }
}