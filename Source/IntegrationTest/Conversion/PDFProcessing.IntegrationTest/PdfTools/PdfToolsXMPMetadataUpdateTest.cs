﻿using SystemWrapper.IO;
using NUnit.Framework;
using pdfforge.PDFCreator.Conversion.Processing.PdfProcessingInterface;
using pdfforge.PDFCreator.Conversion.Processing.PdfToolsProcessing;
using pdfforge.PDFCreator.IntegrationTest.Conversion.PDFProcessing.Base;

namespace pdfforge.PDFCreator.IntegrationTest.Conversion.PDFProcessing.PdfTools
{
    class PdfToolsXMPMetadataUpdateTest : XMPMetadataUpdateTestBase
    {
        protected override IPdfProcessor BuildPdfProcessor()
        {
            var pdfToolsLicensing = new PdfToolsTestLicensing();
            Assert.IsTrue(pdfToolsLicensing.Apply(), "Could not apply pdf-tools licensing.");

            return new PdfToolsPdfProcessor(new FileWrap(), new DefaultProcessingPasswordsProvider());
        }
    }
}
