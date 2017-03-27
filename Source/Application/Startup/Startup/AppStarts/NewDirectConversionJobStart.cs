﻿using NLog;
using pdfforge.PDFCreator.Conversion.Jobs.JobInfo;
using pdfforge.PDFCreator.Core.DirectConversion;
using pdfforge.PDFCreator.Core.Workflow;

namespace pdfforge.PDFCreator.Core.Startup.AppStarts
{
    public abstract class NewDirectConversionJobStart : MaybePipedStart
    {
        private readonly IJobInfoManager _jobInfoManager;
        private readonly IJobInfoQueue _jobInfoQueue;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private string _newInfFile;

        protected NewDirectConversionJobStart(IJobInfoQueue jobInfoQueue,
            IMaybePipedApplicationStarter maybePipedApplicationStarter, IJobInfoManager jobInfoManager)
            : base(maybePipedApplicationStarter)
        {
            _jobInfoQueue = jobInfoQueue;
            _jobInfoManager = jobInfoManager;
        }

        protected abstract DirectConversionBase DirectConversionBase { get; }

        public string NewDirectConversionFile { get; internal set; }
        public string PrinterName { get; internal set; }

        private string NewInfFile
        {
            get
            {
                if (!string.IsNullOrEmpty(_newInfFile))
                    return _newInfFile;

                _newInfFile = DirectConversionBase.TransformToInfFile(NewDirectConversionFile, PrinterName);

                if (string.IsNullOrEmpty(_newInfFile))
                    _newInfFile = "";

                return _newInfFile;
            }
        }

        protected override string ComposePipeMessage()
        {
            return "NewJob|" + NewInfFile;
        }

        protected override bool StartApplication()
        {
            if (string.IsNullOrEmpty(NewInfFile))
                return false;

            _logger.Debug("Adding new job.");
            var jobInfo = _jobInfoManager.ReadFromInfFile(NewInfFile);
            _jobInfoQueue.Add(jobInfo);

            return true;
        }
    }
}