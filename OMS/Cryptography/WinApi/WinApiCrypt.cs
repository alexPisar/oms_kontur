using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Cryptography.WinApi
{
    public class WinApiCrypt
    {
        [DllImport("Crypt32.dll", SetLastError = true)]
        public static extern bool CryptDecryptMessage(ref CRYPT_DECRYPT_MESSAGE_PARA parameters, IntPtr encryptedData, Int32 encryptedDataSize, IntPtr decryptedDataBuffer, ref int bufferLength, IntPtr certificate);

        [DllImport("Crypt32.dll", SetLastError = true)]
        public static extern bool CryptSignMessage(ref CRYPT_SIGN_MESSAGE_PARA parameters, Boolean detachedSignature, Int32 contentsCount, IntPtr[] contents, Int32[] contentsSizes, IntPtr buffer, ref Int32 signatureSize);

        [DllImport("Crypt32.dll", SetLastError = true)]
        public static extern bool CryptVerifyDetachedMessageSignature(ref CRYPT_VERIFY_MESSAGE_PARA parameters, Int32 signerIndex, Byte[] signature, Int32 signatureSize, Int32 contentsCount, IntPtr[] contents, Int32[] contentsSizes, [Out] out IntPtr certificate);

        [DllImport("Crypt32.dll", SetLastError = true)]
        public static extern Boolean CertCloseStore(IntPtr store, Int32 flags);

        [DllImport("Crypt32.dll", SetLastError = true)]
        public static extern IntPtr CertCreateCertificateContext(Int32 encoding, Byte[] certificateData, Int32 certificateDataSize);

        [DllImport("Crypt32.dll")]
        public static extern IntPtr CertDuplicateCertificateContext(IntPtr certificate);

        [DllImport("Crypt32.dll", SetLastError = true)]
        public static extern IntPtr CertEnumCertificatesInStore(IntPtr store, IntPtr previousCertificate);

        [DllImport("Crypt32.dll")]
        public static extern Boolean CertFreeCertificateContext(IntPtr certificate);

        [DllImport("Crypt32.dll", SetLastError = true)]
        public static extern bool CertGetCertificateContextProperty(IntPtr certificate, int propertyId, IntPtr buffer, ref int bufferSize);

        [DllImport("Crypt32.dll", SetLastError = true)]
        public static extern IntPtr CertOpenStore(IntPtr storeProvider, Int32 encoding, IntPtr cryptoProvider, Int32 flags, Byte[] parameters);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptAcquireContext(ref IntPtr cryptoProvider, [MarshalAs(UnmanagedType.LPStr)]string containerName, [MarshalAs(UnmanagedType.LPStr)]string providerName, int providerType, uint flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptReleaseContext(IntPtr cryptoProvider, int flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptGetProvParam(IntPtr cryptoProvider, uint dwParam, byte[] pbData, ref int pdwDataLen, int flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptSetProvParam(IntPtr cryptoProvider, uint dwParam, byte[] pbData, int flags);
    }
}
