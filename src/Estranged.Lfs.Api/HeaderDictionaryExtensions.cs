using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text;

namespace Estranged.Lfs.Api
{
    public static class HeaderDictionaryExtensions
    {
        public static (string Username, string Password) GetGitCredentials(this IHeaderDictionary headers)
        {
            if (!headers.ContainsKey(LfsConstants.AuthorizationHeader))
            {
                throw new InvalidOperationException("No Authorization header found.");
            }

            string[] authValues = headers[LfsConstants.AuthorizationHeader].ToArray();
            if (authValues.Length != 1)
            {
                throw new InvalidOperationException("More than one Authorization header found.");
            }

            string auth = authValues.Single();
            if (!auth.StartsWith(LfsConstants.BasicPrefix))
            {
                throw new InvalidOperationException("Authorization header is not Basic.");
            }

            auth = auth.Substring(LfsConstants.BasicPrefix.Length).Trim();

            byte[] decoded = Convert.FromBase64String(auth);
            Encoding iso = Encoding.GetEncoding("ISO-8859-1");

            string[] authPair = iso.GetString(decoded).Split(':');

            return (authPair[0], authPair[1]);
        }
    }
}
