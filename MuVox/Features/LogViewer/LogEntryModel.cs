﻿using System;

namespace TTech.MuVox.Features.LogViewer
{
    public class LogEntryModel : GalaSoft.MvvmLight.ObservableObject
    {
        public LogEntryModel(string message)
        {
            DateTime = DateTime.Now;
            Message = message;
        }

        public DateTime DateTime { get; set; }

        public string Message { get; set; }
    }
}
