using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace Cryptography.Utils
{
    public class XmlCertificate
    {
        private System.Net.WebClient _client = null;
        private List<KeyValuePair<string, Org.BouncyCastle.X509.X509Crl>> _listOfRevoke = new List<KeyValuePair<string, Org.BouncyCastle.X509.X509Crl>>();

        public XmlCertificate()
        {

        }

        public XmlCertificate(System.Net.WebClient client)
        {
            _client = client;
        }

        public string ParseCertAttribute(string certData, string attributeName)
        {
            string result = String.Empty;
            try
            {
                if (certData == null || certData == "") return result;

                attributeName = attributeName + "=";

                if (!certData.Contains(attributeName)) return result;

                int start = certData.IndexOf(attributeName);

                if (start > 0 && !certData.Substring(0, start).EndsWith(" "))
                {
                    attributeName = " " + attributeName;

                    if (!certData.Contains(attributeName)) return result;
                }

                start = certData.IndexOf(attributeName) + attributeName.Length;

                int length = certData.IndexOf('=', start) == -1 ? certData.Length - start : certData.IndexOf(", ", start) - start;

                if (length == 0) return result;
                if (length > 0)
                {
                    result = certData.Substring(start, length);

                }
                else
                {
                    result = certData.Substring(start);
                }
                return result;

            }
            catch (Exception)
            {
                return result;
            }
        }

        public string GetPersonInnFromCertificate(X509Certificate2 certificate)
        {
            var crypt = new WinApi.WinApiCryptWrapper(certificate);
            var ogrnip = crypt.GetValueBySubjectOid("1.2.643.100.5");

            if (string.IsNullOrEmpty(ogrnip))
                return null;

            var inn = ParseCertAttribute(certificate.Subject, "ИНН").TrimStart('0');

            if (string.IsNullOrEmpty(inn))
            {
                crypt.InitializeCertificate(certificate);
                inn = crypt.GetValueBySubjectOid("1.2.643.3.131.1.1");
            }

            return inn;
        }

        public string GetOrgInnFromCertificate(X509Certificate2 certificate)
        {
            var inn = ParseCertAttribute(certificate.Subject, "ИНН").TrimStart('0');

            if (string.IsNullOrEmpty(inn) || inn.Length == 12)
            {
                var crypt = new WinApi.WinApiCryptWrapper(certificate);
                inn = crypt.GetValueBySubjectOid("1.2.643.100.4");
            }

            return inn;
        }

        /// <summary>
        /// Проверка сертификата на валидность, и что он не содержится в списках отзыва
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public bool IsCertificateValid(X509Certificate2 certificate, Action<string> logAction = null)
        {
            logAction?.Invoke($"IsCertificateValid : проверка на валидность сертификата с серийным номером {certificate.SerialNumber}");
            var cert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(certificate);

            if (!cert.IsValidNow)
                return false;

            if (_client == null)
                return true;

            logAction?.Invoke($"Проверка наличия сертификата в списках отзыва");
            var crypto = new WinApi.WinApiCryptWrapper(certificate);

            var references = crypto.GetCrlReferences();
            bool isCertRevoked = false;

            foreach (var reference in references)
            {
                if (string.IsNullOrEmpty(reference))
                    continue;

                if (isCertRevoked)
                    continue;

                Org.BouncyCastle.X509.X509Crl crl = null;

                try
                {
                    if (_listOfRevoke.Exists(l => l.Key == reference))
                    {
                        crl = _listOfRevoke.First(l => l.Key == reference).Value;
                        isCertRevoked = isCertRevoked || crl.IsRevoked(cert);
                    }
                    else
                    {
                        var bytes = _client.DownloadData(reference);
                        isCertRevoked = isCertRevoked || crypto.IsCertRevoked(bytes, out crl);

                        if (crl != null)
                            _listOfRevoke.Add(new KeyValuePair<string, Org.BouncyCastle.X509.X509Crl>(reference, crl));
                    }
                }
                catch
                {
                    continue;
                }
            }

            logAction?.Invoke($"Результат проверки на наличие в списках отзыва - {isCertRevoked.ToString()}");
            return !isCertRevoked;
        }
    }
}
