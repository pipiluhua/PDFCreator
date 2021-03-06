﻿using System;
using System.Collections.Generic;
using System.Windows.Input;
using SystemInterface.IO;
using pdfforge.Obsidian;
using pdfforge.PDFCreator.Conversion.Settings;
using pdfforge.PDFCreator.Conversion.Settings.Enums;
using pdfforge.PDFCreator.Conversion.Settings.GroupPolicies;
using pdfforge.PDFCreator.Core.Controller;
using pdfforge.PDFCreator.Core.Printing.Printer;
using pdfforge.PDFCreator.Core.Services.Logging;
using pdfforge.PDFCreator.Core.SettingsManagement;
using pdfforge.PDFCreator.UI.Interactions;
using pdfforge.PDFCreator.UI.Interactions.Enums;
using pdfforge.PDFCreator.UI.ViewModels.Assistants;
using pdfforge.PDFCreator.UI.ViewModels.UserControlViewModels.ApplicationSettings.Translations;
using pdfforge.PDFCreator.Utilities.Process;

namespace pdfforge.PDFCreator.UI.ViewModels.UserControlViewModels.ApplicationSettings
{
    public class DebugTabViewModel : ObservableObject
    {
        private readonly IFile _fileWrap;
        private readonly IIniSettingsAssistant _iniSettingsAssistant;
        private readonly IInteractionInvoker _invoker;
        private readonly IPrinterHelper _printerHelper;
        private readonly IProcessStarter _processStarter;
        private readonly ISettingsManager _settingsManager;
        private ISettingsProvider _settingsProvider;
        private readonly ITestPageHelper _testPageHelper;
        private DebugTabTranslation _translation;

        public DebugTabViewModel(ISettingsManager settingsManager, ITestPageHelper testPageHelper, IFile fileWrap, IProcessStarter processStarter, IInteractionInvoker invoker, IPrinterHelper printerHelper, IIniSettingsAssistant iniSettingsAssistant, DebugTabTranslation translation)
        {
            _fileWrap = fileWrap;
            _processStarter = processStarter;
            _invoker = invoker;
            _printerHelper = printerHelper;
            _iniSettingsAssistant = iniSettingsAssistant;
            _translation = translation;

            _settingsManager = settingsManager;
            _settingsProvider = settingsManager.GetSettingsProvider();
            _testPageHelper = testPageHelper;

            ShowLogFileCommand = new DelegateCommand(ExecuteShowLogFile);
            ClearLogFileCommand = new DelegateCommand(ExecuteClearLogFile);
            PrintPdfCreatorTestpageCommand = new DelegateCommand(ExecutePdfCreatorTestpage);
            PrintWindowsTestpageCommand = new DelegateCommand(ExecuteWindowsTestpage);
            LoadIniSettingsCommand = new DelegateCommand(ExecuteLoadIniSettings);
            SaveIniSettingsCommand = new DelegateCommand(ExecuteSaveIniSettings);
            RestoreDefaultSettingsCommand = new DelegateCommand(ExecuteRestoreDefaultSettings);
        }

        public DebugTabTranslation Translation
        {
            get { return _translation; }
            set { _translation = value; RaisePropertyChanged(nameof(Translation)); }
        }


        public ICommand ShowLogFileCommand { get; }
        public ICommand ClearLogFileCommand { get; }
        public ICommand PrintPdfCreatorTestpageCommand { get; }
        public ICommand PrintWindowsTestpageCommand { get; }
        public ICommand LoadIniSettingsCommand { get; }
        public ICommand SaveIniSettingsCommand { get; }
        public ICommand RestoreDefaultSettingsCommand { get; }

        public IGpoSettings GpoSettings { get; private set; }
        public Conversion.Settings.ApplicationSettings ApplicationSettings { get; private set; }

        public bool ProfileManagementIsEnabled
        {
            get
            {
                if (GpoSettings == null)
                    return true;
                return !GpoSettings.DisableProfileManagement;
            }
        }

        public event EventHandler<SettingsEventArgs> SettingsLoaded;

        private void ExecuteLoadIniSettings(object o)
        {
            var success = _iniSettingsAssistant.LoadIniSettings();

            if (!success)
                return;

            ApplySettingsProcedure(_settingsProvider.Settings);
        }

        private void ExecuteSaveIniSettings(object o)
        {
            _iniSettingsAssistant.SaveIniSettings(ApplicationSettings);
        }

        private void ExecutePdfCreatorTestpage(object o)
        {
            if (!QuerySaveModifiedSettings())
                return;

            _testPageHelper.CreateTestPage();
        }

        private void ExecuteWindowsTestpage(object o)
        {
            if (!QuerySaveModifiedSettings())
                return;

            _printerHelper.PrintWindowsTestPage(ApplicationSettings.PrimaryPrinter);
        }

        private void ExecuteClearLogFile(object o)
        {
            if (_fileWrap.Exists(LoggingHelper.LogFile))
            {
                _fileWrap.WriteAllText(LoggingHelper.LogFile, "");
            }
        }

        private void ExecuteShowLogFile(object o)
        {
            if (_fileWrap.Exists(LoggingHelper.LogFile))
            {
                _processStarter.Start(LoggingHelper.LogFile);
            }
            else
            {
                var caption = Translation.NoLogFile;
                var message = Translation.NoLogFileAvailable;

                var interaction = new MessageInteraction(message, caption, MessageOptions.OK, MessageIcon.Warning);
                _invoker.Invoke(interaction);
            }
        }

        public IEnumerable<LoggingLevel> LoggingValues => Enum.GetValues(typeof(LoggingLevel)) as LoggingLevel[];
        private void ExecuteRestoreDefaultSettings(object obj)
        {
            var title = Translation.RestoreDefaultSettingsTitle;
            var message = Translation.RestoreDefaultSettingsMessage;
            var messageInteraction = new MessageInteraction(message, title, MessageOptions.YesNo, MessageIcon.Question);
            _invoker.Invoke(messageInteraction);
            if (messageInteraction.Response == MessageResponse.Yes)
            {
                var profileBuilder = new DefaultProfileBuilder();
                var defaultSettings = profileBuilder.CreateDefaultSettings(_settingsProvider.Settings);
                ApplySettingsProcedure(defaultSettings);
            }
        }

        private void ApplySettingsProcedure(PdfCreatorSettings settings)
        {
            _settingsManager.ApplyAndSaveSettings(settings);
            _settingsManager.LoadPdfCreatorSettings(); //Load settings to ensure default profile
            _settingsManager.SaveCurrentSettings(); //Save settings again to synch registry with current settings
            SettingsLoaded?.Invoke(this, new SettingsEventArgs(_settingsProvider.Settings));
        }

        private bool AppSettingsAreModified()
        {
            return !ApplicationSettings.Equals(_settingsProvider.Settings.ApplicationSettings);
        }

        private void SaveAppSettings()
        {
            _settingsProvider.Settings.ApplicationSettings = ApplicationSettings.Copy();
            _settingsManager.ApplyAndSaveSettings(_settingsProvider.Settings); // call apply to trigger LanguageChanged event
        }

        private bool QuerySaveModifiedSettings()
        {
            if (!AppSettingsAreModified())
                return true; //No changes -> proceed

            var message = Translation.AskSaveModifiedSettings;
            var caption = Translation.AppSettings;

            var interaction = new MessageInteraction(message, caption, MessageOptions.YesNo, MessageIcon.Question);

            _invoker.Invoke(interaction);

            var response = interaction.Response;

            if (response == MessageResponse.Yes) //Proceed with saved settings
            {
                SaveAppSettings();
                return true;
            }
            if (response == MessageResponse.No) //Proceed with old settings
            {
                return true;
            }
            return false; //Cancel Testprinting
        }

        public void SetSettingsAndRaiseNotifications(Conversion.Settings.ApplicationSettings applicationSettings, IGpoSettings gpoSettings)
        {
            ApplicationSettings = applicationSettings;
            GpoSettings = gpoSettings;

            OnSettingsChanged();
        }

        private void OnSettingsChanged()
        {
            RaisePropertyChanged(nameof(ApplicationSettings));
            RaisePropertyChanged(nameof(GpoSettings));
            RaisePropertyChanged(nameof(ProfileManagementIsEnabled));
        }
    }

    public class SettingsEventArgs : EventArgs
    {
        public SettingsEventArgs(PdfCreatorSettings settings)
        {
            Settings = settings;
        }

        public PdfCreatorSettings Settings { get; private set; }
    }
}