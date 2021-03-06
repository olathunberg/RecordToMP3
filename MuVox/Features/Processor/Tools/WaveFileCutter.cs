﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using TTech.MuVox.Core;

namespace TTech.MuVox.Features.Processor.Tools
{
    public class WaveFileCutter
    {
        private Settings.Settings Settings => Features.Settings.Settings.Current;

        public Task<List<string>> CutWavFileFromMarkersFile(string baseFilename, Action<string> addLogMessage, IProgress<long> progressMaximum, IProgress<long> progress)
        {
            return Task.Run<List<string>>(() => DoCutWavFileFromMarkersFile(baseFilename, addLogMessage, progressMaximum, progress));
        }

        public void CutWavFileToEnd(string inPath, string outPath, TimeSpan cutFrom, IProgress<long> progress)
        {
            Debug.Assert(progress != null);
            if (progress == null)
                return;

            EnsureOutputDirectory(outPath);

            using (var reader = new WaveFileReader(inPath))
            using (var writer = new WaveFileWriter(outPath, reader.WaveFormat))
            {
                int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                int startPos = (int)cutFrom.TotalMilliseconds * bytesPerMillisecond;
                startPos -= startPos % reader.WaveFormat.BlockAlign;

                var endPos = (int)reader.Length;

                CutWavFile(reader, writer, startPos, endPos, progress);
            }
        }

        public void CutWavFile(string inPath, string outPath, TimeSpan cutFrom, TimeSpan cutTo, IProgress<long> progress)
        {
            Debug.Assert(progress != null);
            if (progress == null)
                return;

            EnsureOutputDirectory(outPath);

            using (var reader = new WaveFileReader(inPath))
            using (var writer = new WaveFileWriter(outPath, reader.WaveFormat))
            {
                int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                int startPos = (int)cutFrom.TotalMilliseconds * bytesPerMillisecond;
                startPos -= startPos % reader.WaveFormat.BlockAlign;

                int endPos = (int)cutTo.TotalMilliseconds * bytesPerMillisecond;
                endPos -= endPos % reader.WaveFormat.BlockAlign;

                CutWavFile(reader, writer, startPos, endPos, progress);
            }
        }

        private static void EnsureOutputDirectory(string outPath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(outPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            }
        }

        private string GetTargetFileName(string baseFilename, int fileIndex)
        {
            if (!string.IsNullOrEmpty(Settings.Processor_OutputPath))
                baseFilename = Path.Combine(Settings.Processor_OutputPath, Path.GetFileName(baseFilename));

            return Path.ChangeExtension(baseFilename, "." + fileIndex + ".wav");
        }

        private List<string> DoCutWavFileFromMarkersFile(string baseFilename, Action<string> addLogMessage, IProgress<long> progressMaximum, IProgress<long> progress)
        {
            if (MarkersHelper.HasMarkerFile(baseFilename))
            {
                using (var reader = new WaveFileReader(baseFilename))
                    progressMaximum.Report(reader.Length);

                var markers = MarkersHelper
                    .GetMarkersFromFile(baseFilename);

                var newFiles = new ConcurrentBag<string>();

                addLogMessage("Creating segments");
                var fileIndex = 0;
                Parallel.For(0, markers.Count + 1, i =>
                {
                    fileIndex++;
                    if (i == markers.Count)
                    {
                        if (markers[i - 1].Type == Core.Marker.MarkerType.RemoveAfter)
                            return;

                        var start2 = new TimeSpan(0, 0, 0, 0, markers[i - 1].Time * 100);

                        var lastFilename = GetTargetFileName(baseFilename, fileIndex);
                        CutWavFileToEnd(baseFilename, lastFilename, start2, progress);
                        newFiles.Add(lastFilename);
                    }
                    else
                    {
                        if (markers[i].Type == Core.Marker.MarkerType.RemoveBefore || (i > 0 && markers[i - 1].Type == Core.Marker.MarkerType.RemoveAfter))
                            return;

                        var marker = i == 0 ? 0 : markers[i - 1].Time;
                        var start = new TimeSpan(0, 0, 0, 0, marker * 100);
                        var end = new TimeSpan(0, 0, 0, 0, markers[i].Time * 100);

                        var newFilename = GetTargetFileName(baseFilename, fileIndex);
                        CutWavFile(baseFilename, newFilename, start, end, progress);
                        newFiles.Add(newFilename);
                    }
                });

                newFiles = EnsureSingleFileFilename(newFiles);

                return newFiles.ToList();
            }

            using (var reader = new WaveFileReader(baseFilename))
                progress.Report(reader.Length);

            return new List<string> { baseFilename };
        }

        private static ConcurrentBag<string> EnsureSingleFileFilename(ConcurrentBag<string> newFiles)
        {
            if (newFiles.Count() == 1)
            {
                var splitFilename = newFiles.First().Split('.');
                var firstPart = splitFilename.Take(splitFilename.Count() - 2).Select(x => x).ToList();
                firstPart.Add(splitFilename.Last());
                var newFilename = string.Join(".", firstPart.ToArray());

                if (File.Exists(newFilename))
                {
                    File.Delete(newFilename);
                }
                File.Move(newFiles.First(), newFilename);
                newFiles = new ConcurrentBag<string>() { newFilename };
            }

            return newFiles;
        }

        private void CutWavFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos, IProgress<long> progress)
        {
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            while (reader.Position < endPos)
            {
                var bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }

                    progress.Report(bytesRead);
                }
            }
        }
    }
}