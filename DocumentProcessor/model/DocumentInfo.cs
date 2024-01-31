
namespace documentprocessor
{
    public class DocumentInfo
    {
        public readonly string DpItemId;
        public readonly string DocumentId;
        public readonly string Path;
        public readonly string Extension;

        public DocumentInfo(string dpItemId, string documentId, string path, string extension)
        {
            DpItemId = dpItemId;
            DocumentId = documentId;
            Path = path;
            Extension = extension;
        }
    }
}
