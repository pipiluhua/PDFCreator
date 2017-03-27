﻿using System;
using NLog;
using Optional;
using pdfforge.LicenseValidator.Interface;
using pdfforge.LicenseValidator.Interface.Data;
using pdfforge.Obsidian;
using pdfforge.PDFCreator.Core.Controller;
using pdfforge.PDFCreator.Core.SettingsManagement;
using pdfforge.PDFCreator.Core.Startup.Translations;
using pdfforge.PDFCreator.Core.StartupInterface;
using pdfforge.PDFCreator.UI.Interactions;
using pdfforge.PDFCreator.UI.Interactions.Enums;
using pdfforge.PDFCreator.Utilities;

namespace pdfforge.PDFCreator.Core.Startup.StartConditions
{
    public class LicenseCondition : IStartupCondition
    {
        private readonly IInteractionInvoker _interactionInvoker;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ISettingsManager _settingsManager;
        private readonly ProgramTranslation _translation;
        private readonly ILicenseChecker _licenseChecker;
        private readonly IVersionHelper _versionHelper;
        private readonly ApplicationNameProvider _applicationNameProvider;

        public LicenseCondition(ISettingsManager settingsManager, ProgramTranslation translation, ILicenseChecker licenseChecker, IInteractionInvoker interactionInvoker, IVersionHelper versionHelper, ApplicationNameProvider applicationNameProvider)
        {
            _settingsManager = settingsManager;
            _translation = translation;
            _licenseChecker = licenseChecker;
            _interactionInvoker = interactionInvoker;
            _versionHelper = versionHelper;
            _applicationNameProvider = applicationNameProvider;
        }

        public StartupConditionResult Check()
        {
            var activation = RenewActivation();

            if (activation.Exists(a => a.IsActivationStillValid()))
                return StartupConditionResult.BuildSuccess();

            _logger.Error("Invalid or expired license.");

            var editionWithVersionNumber = _applicationNameProvider.ApplicationName + " " + _versionHelper.FormatWithThreeDigits();

            _settingsManager.LoadAllSettings();
            var settingsProvider = _settingsManager.GetSettingsProvider();

            if (settingsProvider.GpoSettings.HideLicenseTab)
            {
                var errorMessage = _translation.GetFormattedLicenseInvalidGpoHideLicenseTab(editionWithVersionNumber);
                return StartupConditionResult.BuildErrorWithMessage((int)ExitCode.LicenseInvalidAndHiddenWithGpo, errorMessage);
            }

            var caption = _applicationNameProvider.ApplicationName;
            var message = _translation.GetFormattedLicenseInvalidTranslation(editionWithVersionNumber);
            var result = ShowMessage(message, caption, MessageOptions.YesNo, MessageIcon.Exclamation);
            if (result != MessageResponse.Yes)
                return StartupConditionResult.BuildErrorWithMessage((int)ExitCode.LicenseInvalidAndNotReactivated, "The license is invalid!", showMessage:false);

            var interaction = new LicenseInteraction();
            _interactionInvoker.Invoke(interaction);

            //Check latest edition for validity
            activation = _licenseChecker.GetSavedActivation();

            if (activation.Exists(a => a.IsActivationStillValid()))
                return StartupConditionResult.BuildSuccess();

            return StartupConditionResult.BuildErrorWithMessage((int)ExitCode.LicenseInvalidAfterReactivation, _translation.GetFormattedLicenseInvalidAfterReactivationTranslation(_applicationNameProvider.ApplicationName));
        }

        private Option<Activation, LicenseError> RenewActivation()
        {
            var activation = _licenseChecker.GetSavedActivation();

            if (activation.Exists(a => a.ActivationMethod == ActivationMethod.Offline))
                return activation;

            if (activation.Exists(IsActivationPeriodStillValid))
                return activation;

            var licenseKey = activation.Match(
                some: a => a.Key.Some<string, LicenseError>(),
                none: e => _licenseChecker.GetSavedLicenseKey());

            return licenseKey.Match(
                some: key => _licenseChecker.ActivateWithKey(key),
                none: e => Option.None<Activation, LicenseError>(LicenseError.NoLicenseKey));
        }

        private bool IsActivationPeriodStillValid(Activation activation)
        {
            var remainingActivationTime = activation.ActivatedTill - DateTime.Now;
            return remainingActivationTime >= TimeSpan.FromDays(4);
        }

        private MessageResponse ShowMessage(string message, string title, MessageOptions options, MessageIcon icon)
        {
            var interaction = new MessageInteraction(message, title, options, icon);
            _interactionInvoker.Invoke(interaction);
            return interaction.Response;
        }
    }
}