using System.Collections.Generic;
using System.IO;

namespace documentprocessor
{
    public enum FileFormat
    {
        Unrecognised,
        WordDocument,
        WordDocumentX,
        PDF,
        JPEG,
        PNG,
        TIFF,
        Text,
        BMP,
        RTF
    }

    public enum FileFormatFamily
    {
        Unrecognised,
        Word,
        PDF,
        Image
    }

    public static class FileFormats
    {
        public static FileFormat GetFileFormat(string extension)
        {
            string extensionLower = extension.Trim().ToLowerInvariant().Replace(".", "");
            return extensionLower switch
            {
                "doc" => FileFormat.WordDocument,
                "docx" => FileFormat.WordDocumentX,
                "pdf" => FileFormat.PDF,
                "jpg" or "jpeg" => FileFormat.JPEG,
                "bmp" => FileFormat.BMP,
                "tiff" or "tif" => FileFormat.TIFF,
                "png" => FileFormat.PNG,
                "txt" => FileFormat.Text,
                "rtf" => FileFormat.RTF,
                _ => FileFormat.Unrecognised,
            };
        }

        public static FileFormatFamily GetFileFormatFamily(FileFormat fileFormat)
        {
            return fileFormat switch
            {
                FileFormat.WordDocumentX or FileFormat.WordDocument or FileFormat.RTF or FileFormat.Text => FileFormatFamily.Word,
                FileFormat.PDF => FileFormatFamily.PDF,
                FileFormat.JPEG or FileFormat.PNG or FileFormat.TIFF or FileFormat.BMP => FileFormatFamily.Image,
                _ => FileFormatFamily.Unrecognised,
            };
        }

        public static FileFormatFamily GetFileFormatFamily(string extension)
        {
            return GetFileFormatFamily(GetFileFormat(extension));
        }

        public static FileFormatFamily GetPathFormatFamily(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return GetFileFormatFamily(GetFileFormat(extension));
        }

        public static List<FileFormat> GetFileFormats(string formatArg)
        {
            List<FileFormat> result = new List<FileFormat>();
            string[] formats = formatArg.Split(',');
            foreach (string format in formats)
            {
                result.Add(GetFileFormat(format));
            }
            return result;
        }

        public static List<FileFormatFamily> GetFileFormatFamilies(string formatArg)
        {
            List<FileFormatFamily> result = new List<FileFormatFamily>();
            string[] formats = formatArg.Split(',');
            foreach (string format in formats)
            {
                result.Add(GetFileFormatFamily(format));
            }
            return result;
        }

        public static List<FileFormatFamily> GetFileFormatFamilies(List<FileFormat> fileFormatList)
        {
            List<FileFormatFamily> result = new List<FileFormatFamily>();
            foreach (FileFormat fileFormat in fileFormatList)
            {
                result.Add(GetFileFormatFamily(fileFormat));
            }
            return result;
        }

        public static string GetPreferredExtension(FileFormat fileFormat)
        {
            return fileFormat switch
            {
                FileFormat.WordDocumentX or FileFormat.WordDocument => "docx",
                FileFormat.PDF => "pdf",
                FileFormat.JPEG => "jpg",
                FileFormat.PNG => "png",
                FileFormat.TIFF => "tif",
                FileFormat.RTF => "rtf",
                FileFormat.Text => "txt",
                FileFormat.BMP => "bmp",
                _ => null,
            };
        }
    }
}
