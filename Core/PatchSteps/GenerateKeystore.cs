using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.X509.Extension;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.OpenSsl;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class GenerateKeystore : IPatchStep
    {
        private const string FALLBACK_CERT = @"-----BEGIN CERTIFICATE-----
MIICNTCCAZ6gAwIBAgIUeXP9Gyg714ZW2GVMXKbZzAKZIhEwDQYJKoZIhvcNAQEL
BQAwFjEUMBIGA1UEAwwLbGVtb25fbWVsb24wIBcNMjMwNjA2MDU1NjI2WhgPMzAy
MTEwMDcwNTU2MjZaMBYxFDASBgNVBAMMC2xlbW9uX21lbG9uMIGfMA0GCSqGSIb3
DQEBAQUAA4GNADCBiQKBgQDTxm5a2ooyAwzeBBdPkkMYadD2zsxFCcIRqteEjC9p
3R0J5rYSXJjGTrY17O4BEsngJ1ffDhHVWfz9CTByWiDhLqrZWsRMPqyP/bx4Ar1d
y4TwcyD3sIEkPTDhKzGtWi9+OENesL6zYCzpzWOZKZjOgi0a7hTGxe3QHaOnFUWL
twIDAQABo34wfDAdBgNVHQ4EFgQUPIiLPMKTkZAxh5D4kiIKZ+yx1pAwHwYDVR0j
BBgwFoAUPIiLPMKTkZAxh5D4kiIKZ+yx1pAwCwYDVR0PBAQDAgKEMBMGA1UdJQQM
MAoGCCsGAQUFBwMDMBgGA1UdEQQRMA+CDW1lbG9ud2lraS54eXowDQYJKoZIhvcN
AQELBQADgYEAw/SPOp/f2ssuS+Vh+CL9+UGRDaIMBfNaG8KNxsEuq+Ctw2/8M33j
UxSr/+a1ho1LxlS1chz35w+oI93eEObY3WwBp5TjcGgaH7WjrxHNvt5S6A1Hb7/v
R7N2eEM//D9Cl70NQN/837HJxUm45tjhRVVPAKdXg7pxZP/2HF8FW84=
-----END CERTIFICATE-----
-----BEGIN RSA PRIVATE KEY-----
MIICXgIBAAKBgQDTxm5a2ooyAwzeBBdPkkMYadD2zsxFCcIRqteEjC9p3R0J5rYS
XJjGTrY17O4BEsngJ1ffDhHVWfz9CTByWiDhLqrZWsRMPqyP/bx4Ar1dy4TwcyD3
sIEkPTDhKzGtWi9+OENesL6zYCzpzWOZKZjOgi0a7hTGxe3QHaOnFUWLtwIDAQAB
AoGBAKmXuBpT9uW0IaLOPejAJbEwVGLCGz2SYfMKEIuaRAIQS8f5FYfA1avBrxOi
SLtdU4OJnkoHl2p3JS1yJXT+DmMxqHRH4FkNxGkMfz9l+XsHnTCSiynRo5FzQo4j
fg8v3o8GdODxS2LUQLT89KUCXp+jDwSrNi0rIufeWD0sLOcBAkEA7AnKAOaC0D/3
k+xu7Ii2fKKjW5Rw18wMVw8sG8FNonJXH0ddB/i73P+Il1BllNpILbUjgTckRGko
c9eDJqMjNwJBAOWvWOV9TM3DSLOGKETs8MgSNG5onqVn77T7tR5dLTLynRS9v+L2
p/jty/y9FapkwRRRX/qojZQqYIyN/rt2G4ECQQCjJiUFOE+FCCHlkhAd2GVigrwt
Sc4xqu2Ao5EWYid6OFQ134rTPr8Dg3DzPfPoznQDe+fdobKkwpbec0FIzIxDAkEA
q8GkSHiapoQSKa15D5HfvL1gV/AEMsy2hDB2EG69Dgw/SvNaOu8YTR4GHMmJGhKe
EAOKMnc46EOIT5Mfmi+IAQJAMyPfohx7AkrhalBIiTDV6pl332pdfVTBHdPUf4XV
IAE6kTSMMHC6bVbrbS/CC8hRW8m7yD3LUa1EjFJmRWXsCQ==
-----END RSA PRIVATE KEY-----";

        public bool Run(Patcher patcher)
        {
            RsaKeyPairGenerator kpg = new RsaKeyPairGenerator();
            kpg.Init(new KeyGenerationParameters(SecureRandom.GetInstance("SHA256"), 1024));

            AsymmetricCipherKeyPair keyPair = kpg.GenerateKeyPair();

            var certificateGenerator = new X509V3CertificateGenerator();
            BigInteger serialNumber = BigInteger.ProbablePrime(120, new Random());
            X509Name issuerDN = new X509Name("lemon");
            X509Name subjectDN = new X509Name("lemon");
            certificateGenerator.SetSerialNumber(serialNumber);
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(999));
            certificateGenerator.SetPublicKey(keyPair.Public);

            var subjectKeyIdentifierExtension = new SubjectKeyIdentifierStructure(keyPair.Public);
            certificateGenerator.AddExtension(X509Extensions.SubjectKeyIdentifier.Id, false, subjectKeyIdentifierExtension);
            certificateGenerator.AddExtension(X509Extensions.BasicConstraints.Id, true, new BasicConstraints(false));

            Asn1SignatureFactory signatureFactory = new Asn1SignatureFactory("SHA256WITHRSA", keyPair.Private);
            X509Certificate certificate = certificateGenerator.Generate(signatureFactory);

            if (certificate == null)
            {
                patcher._info.PemData = FALLBACK_CERT;
                return true;
            }

            using StringWriter stringWriter = new StringWriter();
            PemWriter pemWriter = new PemWriter(stringWriter);

            pemWriter.WriteObject(new Org.BouncyCastle.Utilities.IO.Pem.PemObject("CERTIFICATE", certificate.GetEncoded()));
            pemWriter.WriteObject(keyPair.Private);
            patcher._info.PemData = pemWriter.ToString();

            return true;
        }
    }
}
