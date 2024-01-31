using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace documentprocessor
{
    public class NetworkConnectionException : Exception
    {

        public NetworkConnectionException()
        {
        }

        public NetworkConnectionException(string message)
        : base(message)
        {
        }

        public NetworkConnectionException(string message, Exception inner)
        : base(message, inner)
        {
        }

        public NetworkConnectionException(string errorMessage, SharedFolder folder) : base(NetworkConnectionErrorMessage(errorMessage, folder))
        {
        }

        public static string NetworkConnectionErrorMessage(string errorMessage, SharedFolder folder)
        {
            string fullMessage = string.Format(
                "{0} folder failed to connect with user {1} on path {2}. Error returned was: {3}",
                folder.FolderName,
                folder.UserName,
                folder.UncPath,
                errorMessage);
            return fullMessage;
        }
    }
}
