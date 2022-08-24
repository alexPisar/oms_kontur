using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Cryptography.WinApi
{
    internal class WinApiConstants
    {
        public const int ResultOk = 0;
        public const int KP_ALGID = 7;
        public const int KP_IV = 1;
        public const int CRYPT_MODE_CBC = 1;
        public const int KP_MODE = 4;
        public const int AT_KEYEXCHANGE = 1;
        public const int PP_KEYEXCHANGE_PIN = 0x20;        public const int CERT_KEY_PROV_INFO_PROP_ID = 2;
        public const int CERT_HASH_PROP_ID = 3;
        public const int CERT_STORE_PROV_SYSTEM = 10;
        public const int CERT_STORE_READONLY_FLAG = 0x00008000;
        public const int CERT_SYSTEM_STORE_CURRENT_USER = 0x00010000;
        public const int CERT_SYSTEM_STORE_LOCAL_MACHINE = 0x00020000;
        public const int CRYPT_E_NO_SIGNER = -2146885618;
        public const int CRYPT_E_NOT_FOUND = -2146885628;
        public const uint CRYPT_VERIFYCONTEXT = 0xF0000000;
        public const int CRYPT_FIRST = 1;
        public const int CRYPT_NEXT = 2;
        public const int CRYPT_FQCN = 0x10;
        public const uint PP_ENUMCONTAINERS = 2;
        public const int ERROR_NO_MORE_FILES = 18;
        public const int PKCS_7_ASN_ENCODING = 0x00010000;
        public const int X509_ASN_ENCODING = 0x00000001;
        public const int ENCODING = X509_ASN_ENCODING | PKCS_7_ASN_ENCODING;
        public const int NTE_BAD_SIGNATURE = unchecked((int)0x80090006);
        public const string OID_GOST_34_11_94 = "1.2.643.2.2.9"; // Функция хэширования ГОСТ Р 34.11-94
        public const string OID_GOST_34_11_12_256 = "1.2.643.7.1.1.2.2"; // Функция хэширования ГОСТ Р 34.11-2012, длина выхода 256 бит
        public const string OID_GOST_34_11_12_512 = "1.2.643.7.1.1.2.3"; // Функция хэширования ГОСТ Р 34.11-2012, длина выхода 512 бит
        public const string OID_GOST_34_11_94_R3410EL = "1.2.643.2.2.3"; // Алгоритм цифровой подписи ГОСТ Р 34.10-2001
        public const string OID_GOST_34_11_12_256_R3410 = "1.2.643.7.1.1.3.2"; // Алгоритм цифровой подписи ГОСТ Р 34.10-2012 для ключей длины 256 бит
        public const string OID_GOST_34_11_12_512_R3410 = "1.2.643.7.1.1.3.3"; // Алгоритм цифровой подписи ГОСТ Р 34.10-2012 для ключей длины 512 бит
    }

    public enum GostTypeEnum
    {
        Gost2001 = 75,
        Gost2012256 = 80,
        Gost2012512 = 81
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPT_KEY_PROV_INFO
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        internal String pwszContainerName;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal String pwszProvName;
        internal Int32 dwProvType;
        internal Int32 dwFlags;
        internal Int32 cProvParam;
        internal CRYPT_KEY_PROV_PARAM rgProvParam;
        internal Int32 dwKeySpec;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPT_KEY_PROV_PARAM
    {
        internal Int32 dwParam;
        internal IntPtr pbData;
        internal Int32 cbData;
        internal Int32 dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPT_SIGN_MESSAGE_PARA
    {
        internal Int32 size;
        internal Int32 encoding;
        internal IntPtr signerCertificate;
        internal CRYPT_ALGORITHM_IDENTIFIER hashAlgorithm;
        internal IntPtr hashAlgorithmAdditionalParameters;
        internal Int32 certificatesCount;
        internal IntPtr certificates;
        internal Int32 revocationListsCount;
        internal IntPtr revocationLists;
        internal Int32 authenticatedAttributesCount;
        internal IntPtr authenticatedAttributes;
        internal Int32 unauthenticatedAttributesCount;
        internal IntPtr unauthenticatedAttributes;
        internal Int32 flags;
        internal Int32 innerContentType;
        internal CRYPT_ALGORITHM_IDENTIFIER hashEncryptionAlgorithm;
        internal IntPtr hashEncryptionAlgorithmAdditionalParameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPT_DECRYPT_MESSAGE_PARA
    {
        internal Int32 size;
        internal Int32 encoding;
        internal Int32 storesCount;
        internal IntPtr stores;
        internal Int32 flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPT_VERIFY_MESSAGE_PARA
    {
        internal Int32 size;
        internal Int32 encoding;
        internal IntPtr cryptoProvider;
        internal IntPtr getSignerCertificateCallback;
        internal IntPtr callbackCookie;
    }

    internal enum BlobType : uint
    {
        SIMPLEBLOB = 0x1,
        PUBLICKEYBLOB = 0x6,
        PRIVATEKEYBLOB = 0x7,
        PLAINTEXTKEYBLOB = 0x8
    }

    internal enum AlgoritmId
    {
        CALG_DH_GR3410_12_256_EPHEM = 0xaa47,
        CALG_DH_GR3410_12_512_EPHEM = 0xaa43,
        CALG_DH_EL_EPHEM = 0xaa25,
        CALG_G28147 = 0x661e,
        CALG_PRO_EXPORT = 0x661f
    }

    internal enum KeyFlags
    {
        none = 0x0000,
        CRYPT_EXPORTABLE = 0x0001
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_CONTEXT
    {
        public uint dwCertEncodingType;
        public IntPtr pbCertEncoded;
        public uint cbCertEncoded;
        public IntPtr pCertInfo;
        public IntPtr hCertStore;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_INFO
    {
        public uint dwVersion; //4
        public CRYPTOAPI_BLOB SerialNumber; //8 - 16
        public CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm; //12 - 16
        public CRYPTOAPI_BLOB Issuer; //8 - 16
        public System.Runtime.InteropServices.ComTypes.FILETIME NotBefore; //8
        public System.Runtime.InteropServices.ComTypes.FILETIME NotAfter; //8
        public CRYPTOAPI_BLOB Subject; //8 - 16
        public CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;//24 - 40
        public CRYPT_BIT_BLOB IssuerUniqueId;//12 - 24
        public CRYPT_BIT_BLOB SubjectUniqueId;//12 - 24
        public uint cExtension;//4
        public IntPtr rgExtension;//4 - 8
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CRYPTOAPI_BLOB //x86 - 8, x64 - 16
    {
        public Int32 cbData;
        public IntPtr pbData;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CRYPT_ALGORITHM_IDENTIFIER //12 - 16
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public String pszObjId; //4
        public CRYPTOAPI_BLOB Parameters;//8
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CRYPT_BIT_BLOB //12-24
    {
        public Int32 cbData; //4
        public IntPtr pbData;//4-8
        public Int32 cUnusedBits;//4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_PUBLIC_KEY_INFO
    {
        public CRYPT_ALGORITHM_IDENTIFIER Algorithm;
        public CRYPT_BIT_BLOB PublicKey;
    }
}
