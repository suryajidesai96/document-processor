using System;
using log4net;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

namespace documentprocessor
{
    public class Factory
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public readonly Config Config;
        public readonly Database Database;
        public readonly Model Model;
        public readonly Processor Processor;
        public readonly Utility Utility;

        private Settings settings;

        private GenericProcessor initialProcessor;
        private GenericProcessor converterProcessor;
        private GenericProcessor imageToPdfProcessor;
        private GenericProcessor pdfToImageProcessor;
        private GenericProcessor finalProcessor;
        private GenericProcessor imageToImageProcessor;
        private GenericProcessor wordPrintProcessor;


        private static SharedFolderManager shareManager = null;

        public Factory()
        {
            Utility = new Utility(this);
            Config = new Config();
            Database = new Database(Config);
            Model = new Model(this);
            ReTool();
            InitShareManager(this);
            Processor = new Processor(this);

            if (!string.IsNullOrWhiteSpace(Config.GemboxDocumentLicence))
            {
                GemBox.Document.ComponentInfo.SetLicense(Config.GemboxDocumentLicence);
            }
            if (!string.IsNullOrWhiteSpace(Config.GemboxPdfLicence))
            {
                GemBox.Pdf.ComponentInfo.SetLicense(Config.GemboxPdfLicence);
            }
        }

        private static void InitShareManager(Factory fac)
        {
            if (shareManager == null)
            {
                shareManager = new SharedFolderManager(fac);
                shareManager.Setup();
            }
        }

        public Settings Settings
        {
            get { return settings; }
        } 

        public GenericProcessor InitialProcessor
        {
            get { return initialProcessor; }
        }

        public GenericProcessor ConverterProcessor
        {
            get { return converterProcessor; }
        }

        public GenericProcessor PdfToImageProcessor
        {
            get { return pdfToImageProcessor; }
        }

        public GenericProcessor FinalProcessor
        {
            get { return finalProcessor; }
        }

        public GenericProcessor ImageToImageProcessor
        {
            get { return imageToImageProcessor; }
        }

        public GenericProcessor WordToPdfProcessor
        {
            get { return wordPrintProcessor; }
        }

        public GenericProcessor ImageToPdfProcessor
        {
            get { return imageToPdfProcessor; }
        }

        public void ReTool()
        {
            settings = new Settings(this);
            initialProcessor = new InitialProcessor(this);
            converterProcessor = new ConvertorProcessor(this);
            pdfToImageProcessor = new DocnetPdfToImage(this);
            finalProcessor = new FinalProcessor(this);
            imageToImageProcessor = new ImageToImageProcessor(this);
            wordPrintProcessor = new GemWordToPdfProcessor(this);
            imageToPdfProcessor = new GemImageToPdfProcessor(this);
        }

        public void CreateTempDirIfMissing()
        {
            System.IO.Directory.CreateDirectory(Config.TempDirectory);
        }

        public void StandDown()
        {
            if (pdfToImageProcessor is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }        
    }
}
