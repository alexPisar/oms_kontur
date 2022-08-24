using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;

namespace Cryptography.WinApi
{
    public class WinApiCryptWrapper : ICrypt
    {
        private X509Certificate2 _certificate;
        private string _pinCode = null;

        private readonly IDictionary<string, string> hashAlgorithmsMap = new Dictionary<string, string>
        {
            { WinApiConstants.OID_GOST_34_11_94_R3410EL, WinApiConstants.OID_GOST_34_11_94 },
            { WinApiConstants.OID_GOST_34_11_12_256_R3410, WinApiConstants.OID_GOST_34_11_12_256 },
            { WinApiConstants.OID_GOST_34_11_12_512_R3410, WinApiConstants.OID_GOST_34_11_12_512 }
        };

        public WinApiCryptWrapper(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public WinApiCryptWrapper()
        {

        }

        public void InitializeCertificate(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public void SetupPinCode(string pinCode)
        {
            if (_certificate == null)
                throw new Exception("Не задан сертификат пользователя.");

            _pinCode = pinCode;
        }

        public string GetCleanThumbprint(string stamp)
        {
            var thumbprintStr = stamp.Replace(" ", "").ToUpper();
            byte[] tBytes = UTF8Encoding.UTF8.GetBytes(thumbprintStr);
            StringBuilder resultThumbprint = new StringBuilder();

            foreach (var byteVal in tBytes)
            {
                if (byteVal >= 48 && byteVal <= 57 || byteVal >= 65 && byteVal <= 90)
                {
                    char ch = UTF8Encoding.UTF8.GetChars(new byte[] { byteVal }).First();
                    resultThumbprint.Append(ch);
                }
            }

            return resultThumbprint.ToString();
        }

        /// <summary>
        /// Получение значения параметра oid из строки субьекта сертификата
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        public string GetValueBySubjectOid(string oid)
        {
            if (_certificate == null)
                throw new Exception("Не определён сертификат пользователя.");

            var cert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(_certificate);
            var oids = cert.SubjectDN.GetOidList();

            var selectedOid = oids?.OfType<object>()?.FirstOrDefault(s => s.ToString() == oid);

            if (selectedOid == null)
                return null;

            var oidIndex = oids.IndexOf(selectedOid);

            if (oidIndex < 0)
                return null;

            var val = cert.SubjectDN.GetValueList()[oidIndex]?.ToString();
            string inn = val.TrimStart('0');
            return inn;
        }

        public bool IsCertRevoked(byte[] crlBytes, out Org.BouncyCastle.X509.X509Crl crl)
        {
            if (_certificate == null)
                throw new Exception("Не определён сертификат пользователя.");

            var cert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(_certificate);

            crl = new Org.BouncyCastle.X509.X509CrlParser().ReadCrl(crlBytes);

            if (crl == null)
                return false;

            return crl.IsRevoked(cert);
        }

        public List<string> GetCrlReferences()
        {
            if (_certificate == null)
                throw new Exception("Не определён сертификат пользователя.");

            List<string> references = new List<string>();

            var cert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(_certificate);

            var objectIdentifier = new Org.BouncyCastle.Asn1.DerObjectIdentifier("2.5.29.31");
            Org.BouncyCastle.Asn1.Asn1OctetString octetString = cert.GetExtensionValue(objectIdentifier);
            var revokedObjects = Org.BouncyCastle.X509.Extension.X509ExtensionUtilities.FromExtensionValue(octetString);

            if (revokedObjects as Org.BouncyCastle.Asn1.Asn1Sequence == null)
                return references;

            foreach (var revokedObject in (Org.BouncyCastle.Asn1.Asn1Sequence)revokedObjects)
            {
                object obj = ((Org.BouncyCastle.Asn1.Asn1Sequence)revokedObject)[0];

                while (obj.GetType() == typeof(Org.BouncyCastle.Asn1.DerTaggedObject))
                    obj = ((Org.BouncyCastle.Asn1.DerTaggedObject)obj).GetObject();

                if (obj as Org.BouncyCastle.Asn1.Asn1OctetString != null)
                {
                    var bytes = ((Org.BouncyCastle.Asn1.Asn1OctetString)obj).GetEncoded();
                    var str = Encoding.ASCII.GetString(bytes);

                    var indx = str.IndexOf("http");

                    if (indx < 0)
                        continue;

                    str = str.Substring(indx);

                    if(!string.IsNullOrEmpty(str))
                        references.Add(str);
                }
            }

            return references;
        }

        /// <summary>
		/// Сравнение двух массивов байт
		/// </summary>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns></returns>
		private int Compare(byte[] d1, byte[] d2)
        {
            if (d1 == d2) return 0;
            if (d1 == null) return -1;
            if (d2 == null) return 1;
            int data1Length = d1.Length;
            int data2Length = d2.Length;
            for (int i = 0; i < Math.Min(data1Length, data2Length); ++i)
            {
                int comparison = d1[i].CompareTo(d2[i]);
                if (comparison != 0) return comparison;
            }
            return data1Length.CompareTo(data2Length);
        }

        public List<X509Certificate2> GetCertificatesWithGostSignAlgorithm(List<X509Certificate2> certificates)
        {
            return certificates?
                .Where(c => hashAlgorithmsMap.ContainsKey(c.SignatureAlgorithm.Value))?
                .ToList() ?? new List<X509Certificate2>();
        }

        public List<X509Certificate2> GetPersonalCertificates(bool onlyWithPrivateKey, bool useLocalSystemStorage = false)
        {
            var s = new X509Store("MY", useLocalSystemStorage ? StoreLocation.LocalMachine : StoreLocation.CurrentUser);
            s.Open(OpenFlags.ReadOnly);
            try
            {
                return s.Certificates.Cast<X509Certificate2>().Where(c => !onlyWithPrivateKey || c.HasPrivateKey).ToList();
            }
            finally
            {
                s.Close();
            }
        }

        public List<X509Certificate2> GetAllGostPersonalCertificates()
        {
            var certificates = GetPersonalCertificates(true, true);

            certificates.AddRange(GetPersonalCertificates(true, false));

            certificates = GetCertificatesWithGostSignAlgorithm(certificates);

            return certificates;
        }

        public X509Certificate2 GetCertificateWithPrivateKey(string thumbprint, bool useLocalSystemStorage = false)
        {
            var cert = GetPersonalCertificates(true, useLocalSystemStorage)
                .FirstOrDefault(c => c.Thumbprint != null && c.Thumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase));
            if (cert == null)
                throw new Exception("Не найден сертификат с закрытым ключом и отпечатком " + thumbprint);
            return cert;
        }

        /// <summary>
		/// Подписывание данных
		/// </summary>
		/// <param name="content">Содержимое</param>
		/// <param name="certificateContent">Содержимое сертификата</param>
		/// <returns>Подпись</returns>
		public byte[] Sign(byte[] content, bool isDetached)
        {
            if (_certificate == null)
                throw new Exception("Не инициализирован сертификат для подписания");

            int cryptoProvider = (int)GostTypeEnum.Gost2001;

            switch (_certificate.SignatureAlgorithm.Value)
            {
                case WinApiConstants.OID_GOST_34_11_94_R3410EL:
                    cryptoProvider = (int)GostTypeEnum.Gost2001;
                    break;
                case WinApiConstants.OID_GOST_34_11_12_256_R3410:
                    cryptoProvider = (int)GostTypeEnum.Gost2012256;
                    break;
                case WinApiConstants.OID_GOST_34_11_12_512_R3410:
                    cryptoProvider = (int)GostTypeEnum.Gost2012512;
                    break;
            }

            IntPtr Provider = IntPtr.Zero;
            var certificate = IntPtr.Zero;

            var containerName = GetContainerNameByCertificate(ref certificate, _certificate.RawData, cryptoProvider);

            if (string.IsNullOrEmpty(containerName))
                throw new Exception("Не найден контейнер с закрытым ключом.");

            var certificatesHandle = GCHandle.Alloc(new[] { certificate }, GCHandleType.Pinned);

            if (WinApiCrypt.CryptAcquireContext(ref Provider, containerName, "", cryptoProvider, 0))
            {
                try
                {
                    if (!string.IsNullOrEmpty(_pinCode))
                    {
                        WinApiCrypt.CryptSetProvParam(Provider,
                            WinApiConstants.PP_KEYEXCHANGE_PIN,
                            Encoding.UTF8.GetBytes(_pinCode), 0);
                    }

                    return Sign(content, certificate, certificatesHandle, isDetached);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    certificatesHandle.Free();
                    WinApiCrypt.CertFreeCertificateContext(certificate);
                    WinApiCrypt.CryptReleaseContext(Provider, 0);
                }
            }
            else
            {
                throw new Exception("Не удалось инициализировать контекст провайдера.");
            }
        }

        protected byte[] Sign(byte[] content, IntPtr certificate, GCHandle certificatesHandle, bool isDetachedSignature)
        {
            var contentHandle = GCHandle.Alloc(content, GCHandleType.Pinned);

            try
            {
                var signParameters = new CRYPT_SIGN_MESSAGE_PARA();
                signParameters.size = Marshal.SizeOf(signParameters);
                signParameters.encoding = WinApiConstants.ENCODING;
                signParameters.signerCertificate = certificate;
                signParameters.hashAlgorithm.pszObjId = GetHashAlgorithm(certificate);
                signParameters.certificatesCount = 1;
                signParameters.certificates = certificatesHandle.AddrOfPinnedObject();

                var localSignParameters = signParameters;
                var signatureSize = 0;

                if (!WinApiCrypt.CryptSignMessage(ref localSignParameters, isDetachedSignature, 1, new[] { contentHandle.AddrOfPinnedObject() }, new[] { content.Length }, IntPtr.Zero, ref signatureSize))
                    throw new Win32Exception();

                var bufferLength = signatureSize + 1024;

                while (true)
                {
                    var buffer = new byte[bufferLength];
                    var bytesWritten = bufferLength;
                    var signatureHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                    try
                    {
                        if (!WinApiCrypt.CryptSignMessage(ref signParameters, isDetachedSignature, 1, new[] { contentHandle.AddrOfPinnedObject() }, new[] { content.Length }, signatureHandle.AddrOfPinnedObject(), ref bytesWritten))
                            throw new Win32Exception();

                        Array.Resize(ref buffer, bytesWritten);
                        return buffer;
                    }
                    catch (Exception exception)
                    {
                        var win32Exception = exception.InnerException as Win32Exception;
                        if (win32Exception != null && win32Exception.NativeErrorCode == 234)
                            bufferLength *= 2;
                        else
                            throw;
                    }
                    finally
                    {
                        signatureHandle.Free();
                    }
                }
            }
            finally
            {
                contentHandle.Free();
            }
        }

        /// <summary>
		/// Получение имени ключевого контейнера для сертификата с закрытым ключом
		/// </summary>
		/// <param name="certificate"></param>
		/// <returns></returns>
        private string GetContainerNameByCertificate(ref IntPtr certificate, byte[] certificateContent, int cryptoProvider)
        {
            int bufferLength = 0;
            certificate = WinApiCrypt.CertCreateCertificateContext(WinApiConstants.ENCODING, certificateContent, certificateContent.Length);

            if (!WinApiCrypt.CertGetCertificateContextProperty(certificate, WinApiConstants.CERT_KEY_PROV_INFO_PROP_ID, IntPtr.Zero, ref bufferLength))
            {
                certificate = FindCertificateWithPrivateKey(certificate, out bufferLength);

                if (certificate == IntPtr.Zero || bufferLength <= 0)
                    throw new Exception("Не удалось найти контейнер закрытого ключа для сертификата.");
            }

            var buffer = new byte[bufferLength];
            GCHandle bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            WinApiCrypt.CertGetCertificateContextProperty(certificate, WinApiConstants.CERT_KEY_PROV_INFO_PROP_ID, bufferHandle.AddrOfPinnedObject(), ref bufferLength);

            var certKeyInfo = Marshal.PtrToStructure<CRYPT_KEY_PROV_INFO>(bufferHandle.AddrOfPinnedObject());

            return certKeyInfo.pwszContainerName;
        }

        /// <summary>
		/// Возвращает список сертификатов из хранилища
		/// </summary>
		/// <param name="storeHandle"></param>
		/// <returns></returns>
		private List<IntPtr> GetCertificatesFromStore(IntPtr storeHandle)
        {
            var certificates = new List<IntPtr>();
            IntPtr certificate = IntPtr.Zero;
            try
            {
                while (true)
                {
                    certificate = WinApiCrypt.CertEnumCertificatesInStore(storeHandle, certificate);
                    if (certificate == IntPtr.Zero)
                    {
                        Int32 errorCode = Marshal.GetLastWin32Error();
                        if (errorCode == WinApiConstants.CRYPT_E_NOT_FOUND || errorCode == WinApiConstants.ERROR_NO_MORE_FILES)
                            break;
                        throw new Win32Exception(errorCode);
                    }
                    IntPtr certificateContext = WinApiCrypt.CertDuplicateCertificateContext(certificate);
                    certificates.Add(certificateContext);
                }
            }
            catch
            {
                if (certificate != IntPtr.Zero)
                    WinApiCrypt.CertFreeCertificateContext(certificate);
                throw;
            }
            return certificates;
        }

        /// <summary>
		/// Поиск сертификата, совпрадающего с исходным в известных хранилищах
		/// </summary>
		/// <param name="initialCertificate">Исходный сертификат</param>
		/// <returns>Сертификат с закрытым ключом. IntPtr.Zero, если не найден</returns>
		private IntPtr FindCertificateWithPrivateKey(IntPtr initialCertificate, out int bufferLength)
        {
            List<IntPtr> stores = GetDefaultStores();
            bufferLength = 0;
            try
            {
                foreach (IntPtr store in stores)
                {
                    foreach (IntPtr storedCertificate in GetCertificatesFromStore(store))
                    {
                        if (Compare(GetHash(storedCertificate), GetHash(initialCertificate)) == 0)
                        {
                            if(WinApiCrypt.CertGetCertificateContextProperty(storedCertificate, WinApiConstants.CERT_KEY_PROV_INFO_PROP_ID, IntPtr.Zero, ref bufferLength))
                                return storedCertificate;
                            else
                            {
                                int errorCode = Marshal.GetLastWin32Error();

                                if (errorCode != WinApiConstants.CRYPT_E_NOT_FOUND)
                                    throw new Win32Exception(errorCode);
                            }
                        }

                        WinApiCrypt.CertFreeCertificateContext(storedCertificate);
                    }
                }
            }
            finally
            {
                foreach (IntPtr storeHandle in stores)
                    WinApiCrypt.CertCloseStore(storeHandle, 0);
            }
            return IntPtr.Zero;
        }

        /// <summary>
		/// Хэш для сертификата
		/// </summary>
		/// <param name="certificate"></param>
		/// <returns></returns>
		private byte[] GetHash(IntPtr certificate)
        {
            int bufferLength = 0;
            if (!WinApiCrypt.CertGetCertificateContextProperty(certificate, WinApiConstants.CERT_HASH_PROP_ID, IntPtr.Zero, ref bufferLength)) throw new Win32Exception();
            var buffer = new byte[bufferLength];
            GCHandle bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                if (!WinApiCrypt.CertGetCertificateContextProperty(certificate, WinApiConstants.CERT_HASH_PROP_ID, bufferHandle.AddrOfPinnedObject(), ref bufferLength)) throw new Win32Exception();
            }
            finally
            {
                bufferHandle.Free();
            }
            return buffer;
        }

        /// <summary>
		/// Получение списка хранилищ по-умлочанию
		/// </summary>
		/// <returns></returns>
		private List<IntPtr> GetDefaultStores()
        {
            var stores = new List<IntPtr>();
            try
            {
                stores.Add(GetStoreHandle(WinApiConstants.CERT_SYSTEM_STORE_CURRENT_USER, "my"));
                stores.Add(GetStoreHandle(WinApiConstants.CERT_SYSTEM_STORE_CURRENT_USER, "root"));
                stores.Add(GetStoreHandle(WinApiConstants.CERT_SYSTEM_STORE_CURRENT_USER, "ca"));
                stores.Add(GetStoreHandle(WinApiConstants.CERT_SYSTEM_STORE_CURRENT_USER, "addressbook"));
                stores.Add(GetStoreHandle(WinApiConstants.CERT_SYSTEM_STORE_LOCAL_MACHINE, "my"));
                stores.Add(GetStoreHandle(WinApiConstants.CERT_SYSTEM_STORE_LOCAL_MACHINE, "root"));
                stores.Add(GetStoreHandle(WinApiConstants.CERT_SYSTEM_STORE_LOCAL_MACHINE, "ca"));
                stores.Add(GetStoreHandle(WinApiConstants.CERT_SYSTEM_STORE_LOCAL_MACHINE, "addressbook"));
                return stores;
            }
            catch
            {
                foreach (IntPtr store in stores)
                    WinApiCrypt.CertCloseStore(store, 0);
                throw;
            }
        }

        /// <summary>
		/// Получение указателя на хранилище сертификатов
		/// </summary>
		/// <param name="store"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		private IntPtr GetStoreHandle(int store, String name)
        {
            int flags = store | WinApiConstants.CERT_STORE_READONLY_FLAG;
            IntPtr storeHandle = WinApiCrypt.CertOpenStore(new IntPtr(WinApiConstants.CERT_STORE_PROV_SYSTEM), 0, IntPtr.Zero, flags, Encoding.Unicode.GetBytes(name));
            if (storeHandle == IntPtr.Zero)
                throw new Win32Exception();
            return storeHandle;
        }

        private string GetHashAlgorithm(IntPtr certificatePtr)
        {
            var certificate2 = new X509Certificate2(certificatePtr);
            var signatureAlgorithm = certificate2.SignatureAlgorithm.Value;

            return hashAlgorithmsMap.TryGetValue(signatureAlgorithm, out var hashAlgorithm)
                ? hashAlgorithm : WinApiConstants.OID_GOST_34_11_94;
        }
    }
}
