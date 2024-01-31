using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace documentprocessor
{
    public class SharedFolder
    {
        private string uncPath = string.Empty;

        private bool isUnc(string path)
        {
            Uri uri = null;
            if (!Uri.TryCreate(path, UriKind.Absolute, out uri))
            {
                return false;
            }

            return uri.IsUnc;
        }

        public SharedFolder(string name)
        {
            this.FolderName = name;
        }

        public string FolderName
        {
            get;
            set;
        }

        public string UncPath
        {
            get
            {
                return uncPath;
            }
            set
            {
                if (!isUnc(value))
                {
                    throw new ArgumentException("Path provided is not a valid UNC path.", nameof(value));
                }

                uncPath = value;
            }
        }

        public string UserName
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }

        public bool IsValid
        {
            get
            {
                bool valid = !string.IsNullOrWhiteSpace(this.Password)
                    && !string.IsNullOrWhiteSpace(this.UserName)
                    && !string.IsNullOrWhiteSpace(this.UncPath);
                return valid;
            }
        }
    }
}