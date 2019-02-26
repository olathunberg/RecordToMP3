﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using TTech.Muvox.Features.Messages;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;

namespace TTech.Muvox
{
    public class MainWindowModel : ViewModelBase
    {
        private readonly ViewModelLocator viewModelLocator = (ViewModelLocator)Application.Current.Resources["ViewModelLocator"];
        private ViewModelBase _currentViewModel;

        public ViewModelBase CurrentViewModel
        {
            get { return _currentViewModel; }
            set
            {
                if (_currentViewModel == value)
                    return;
                Set(() => CurrentViewModel, ref _currentViewModel, value);
            }
        }

        public MainWindowModel()
        {
            Helpers.HotKeyManager.RegisterHotKey(System.Windows.Forms.Keys.F3, Helpers.KeyModifiers.Alt);
            Helpers.HotKeyManager.HotKeyPressed += HotKeyManager_HotKeyPressed;

            CurrentViewModel = viewModelLocator.Recorder;

            Messenger.Default.Register<GotoPageMessage>(
                this, (action) =>
                {
                    if (action.GotoPage == Pages.Recorder)
                        CurrentViewModel = viewModelLocator.Recorder;
                    if (action.GotoPage == Pages.Processor)
                        CurrentViewModel = viewModelLocator.Processor;
                    if (action.GotoPage == Pages.Marker)
                        CurrentViewModel = viewModelLocator.Marker;
                    if (action.GotoPage == Pages.Settings)
                        CurrentViewModel = viewModelLocator.Settings;
                });
        }

        private RelayCommand showSettingsCommand;
        public ICommand ShowSettingsCommand
        {
            get
            {
                return showSettingsCommand ?? (showSettingsCommand = new RelayCommand(
                    () =>
                    {
                        Messenger.Default.Send(new GotoPageMessage(Pages.Settings));
                    },
                    () => true));
            }
        }

        private void HotKeyManager_HotKeyPressed(object sender, Helpers.HotKeyEventArgs e)
        {
            Messenger.Default.Send<SetMarkerMessage>(new SetMarkerMessage());
        }

        public override void Cleanup()
        {
            base.Cleanup();

            viewModelLocator.Recorder.Cleanup();
            viewModelLocator.Processor.Cleanup();
            viewModelLocator.Marker.Cleanup();
        }
    }
}