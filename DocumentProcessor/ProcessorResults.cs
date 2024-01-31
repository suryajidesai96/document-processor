using System.IO;

namespace documentprocessor
{
    public class ProcessorResults
    {
        public readonly DocumentInfo DocumentInfo;
        public readonly string PathInput;
        public readonly string Processor;
        public readonly string PathOutput;
        public readonly bool Success;
        public readonly string Message;
        public readonly string PathOutputExtension;
        public readonly int? Page;

        public ProcessorResults(DocumentInfo documentInfo, string pathInput, string processor, string pathOutput, bool success)
        {
            DocumentInfo = documentInfo;
            PathInput = pathInput;
            PathOutput = pathOutput;
            Processor = processor;
            Success = success;
            Message = null;
            PathOutputExtension = Path.GetExtension(pathOutput).Replace(".", "");
            Page = null;
        }

        public ProcessorResults(DocumentInfo documentInfo, string pathInput, string processor, string pathOutput, bool success, string message)
        {
            DocumentInfo = documentInfo;
            PathInput = pathInput;
            PathOutput = pathOutput;
            Processor = processor;
            Success = success;
            Message = message;
            PathOutputExtension = Path.GetExtension(pathOutput).Replace(".", "");
            Page = null;
        }

        public ProcessorResults(DocumentInfo documentInfo, string pathInput, string processor, string pathOutput, bool success, string message, int? page)
        {
            DocumentInfo = documentInfo;
            PathInput = pathInput;
            PathOutput = pathOutput;
            Processor = processor;
            Success = success;
            Message = message;
            PathOutputExtension = Path.GetExtension(pathOutput).Replace(".", "");
            Page = page;
        }
    }
}
