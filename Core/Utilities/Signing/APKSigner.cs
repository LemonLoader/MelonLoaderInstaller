using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.Cms;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MelonLoaderInstaller.Core.Utilities.Signing
{
    public class APKSigner
    {
        private X509Certificate _xCert;
        private AsymmetricKeyParameter _privateKey;
        private SHA256 _sha;

        private UTF8Encoding _encoding;
        private IPatchLogger _logger;

        public APKSigner(string pemData, IPatchLogger patchLogger)
        {
            LoadCerts(pemData);
            _sha = SHA256.Create();

            _encoding = new UTF8Encoding(false);
            _logger = patchLogger;
        }

        private void LoadCerts(string pemData)
        {
            _logger.Log("Reading certificates");

            using (var reader = new StringReader(pemData))
            {
                // Iterate through the PEM objects until we find the public or private key
                var pemReader = new PemReader(reader);
                object pemObject;
                while ((pemObject = pemReader.ReadObject()) != null)
                {
                    _xCert ??= pemObject as X509Certificate;
                    _privateKey ??= (pemObject as AsymmetricCipherKeyPair)?.Private;
                }
            }

            if (_xCert == null)
                throw new System.Security.SecurityException("Certificate could not be loaded from PEM data.");

            if (_privateKey == null)
                throw new System.Security.SecurityException("Private Key could not be loaded from PEM data.");
        }

        public void Sign(string apkPath)
        {
            _logger.Log("Signing an APK, this can take a few minutes.");

            SignV1(apkPath);
            _logger.Log("V1 signing complete");

            SignV2(apkPath);
            _logger.Log("V2 signing complete");
        }

        #region V1

        private void SignV1(string apkPath)
        {
            using FileStream apkStream = new FileStream(apkPath, FileMode.Open);
            using ZipArchive apkArchive = new ZipArchive(apkStream, ZipArchiveMode.Update);

            #region Create MANIFEST.MF

            using MemoryStream manifestStream = new MemoryStream();
            using StreamWriter manifestWriter = AsStreamWriter(manifestStream);

            using MemoryStream sigHolderStream = new MemoryStream();
            using StreamWriter sigHolderWriter = AsStreamWriter(sigHolderStream);

            manifestWriter.WriteLine("Manifest-Version: 1.0");
            manifestWriter.WriteLine("Created-By: LemonLoader");
            manifestWriter.WriteLine();

            manifestWriter.Close();

            foreach (ZipArchiveEntry entry in apkArchive.Entries)
            {
                if (entry.FullName.StartsWith("META-INF"))
                    continue;

                WriteDigests(entry, manifestStream, sigHolderWriter);
            }

            sigHolderWriter.Close();

            #endregion

            #region Create LEMON.SF

            using MemoryStream sigStream = new MemoryStream();
            using StreamWriter sigWriter = AsStreamWriter(sigStream);

            manifestStream.Seek(0, SeekOrigin.Begin);
            byte[] manifestSha = _sha.ComputeHash(manifestStream);

            sigWriter.WriteLine("Signature-Version: 1.0");
            sigWriter.WriteLine("Created-By: LemonLoader");
            sigWriter.WriteLine($"SHA-256-Digest-Manifest: {Convert.ToBase64String(manifestSha)}");
            sigWriter.WriteLine();

            sigWriter.Close();

            sigHolderStream.Seek(0, SeekOrigin.Begin);
            sigHolderStream.CopyTo(sigStream);

            #endregion

            #region Add to APK

            // Remove old META-INF files
            for (int i = apkArchive.Entries.Count - 1; i >= 0; i--)
            {
                ZipArchiveEntry file = apkArchive.Entries[i];
                if (file.FullName.StartsWith("META-INF"))
                    file.Delete();
            }

            // Add the new
            manifestStream.Seek(0, SeekOrigin.Begin);
            sigStream.Seek(0, SeekOrigin.Begin);

            ZipArchiveEntry manifestEntry = apkArchive.CreateEntry("META-INF/MANIFEST.MF");
            ZipArchiveEntry sigEntry = apkArchive.CreateEntry("META-INF/LEMON.SF");
            ZipArchiveEntry rsaEntry = apkArchive.CreateEntry("META-INF/LEMON.RSA");

            using (Stream stream = manifestEntry.Open())
                manifestStream.CopyTo(stream);
            using (Stream stream = sigEntry.Open())
                sigStream.CopyTo(stream);

            sigStream.Seek(0, SeekOrigin.Begin);

            byte[] signedSig = GetSignatureFileSig(sigStream.ToArray());
            using (Stream stream = rsaEntry.Open())
                stream.Write(signedSig);

            #endregion
        }

        private void WriteDigests(ZipArchiveEntry entry, Stream manifestStream, StreamWriter sigHolderWriter)
        {
            using Stream entryStream = entry.Open();
            string entryDigest = Convert.ToBase64String(_sha.ComputeHash(entryStream));

            using MemoryStream proxyStream = new MemoryStream();
            using StreamWriter proxyWriter = AsStreamWriter(proxyStream);

            proxyWriter.WriteLine($"Name: {entry.FullName}");
            proxyWriter.WriteLine($"SHA-256-Digest: {entryDigest}");
            proxyWriter.WriteLine();

            proxyWriter.Close();

            proxyStream.Seek(0, SeekOrigin.Begin);

            sigHolderWriter.WriteLine($"Name: {entry.FullName}");
            sigHolderWriter.WriteLine($"SHA-256-Digest: {Convert.ToBase64String(_sha.ComputeHash(proxyStream))}");
            sigHolderWriter.WriteLine();
            proxyStream.Seek(0, SeekOrigin.Begin);
            proxyStream.CopyTo(manifestStream);
        }

        private byte[] GetSignatureFileSig(byte[] sfBytes)
        {
            var certStore = X509StoreFactory.Create("Certificate/Collection", new X509CollectionStoreParameters(new List<X509Certificate> { _xCert }));
            CmsSignedDataGenerator dataGen = new CmsSignedDataGenerator();
            dataGen.AddCertificates(certStore);
            dataGen.AddSigner(_privateKey, _xCert, CmsSignedGenerator.EncryptionRsa, CmsSignedGenerator.DigestSha256);

            // Content is detached - i.e. not included in the signature block itself
            CmsProcessableByteArray detachedContent = new CmsProcessableByteArray(sfBytes);
            var signedContent = dataGen.Generate(detachedContent, false);

            // Get the signature in the proper ASN.1 structure for java to parse it properly.  Lots of trial and error
            var signerInfos = signedContent.GetSignerInfos();
            var signer = signerInfos.GetSigners().Cast<SignerInformation>().First();
            SignerInfo signerInfo = signer.ToSignerInfo();
            Asn1EncodableVector digestAlgorithmsVector = new Asn1EncodableVector
            {
                new AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1"), DerNull.Instance)
            };
            ContentInfo encapContentInfo = new ContentInfo(new DerObjectIdentifier("1.2.840.113549.1.7.1"), null);
            Asn1EncodableVector asnVector = new Asn1EncodableVector()
            {
                X509CertificateStructure.GetInstance(Asn1Object.FromByteArray(_xCert.GetEncoded()))
            };
            Asn1EncodableVector signersVector = new Asn1EncodableVector() { signerInfo.ToAsn1Object() };
            SignedData signedData = new SignedData(new DerSet(digestAlgorithmsVector), encapContentInfo, new BerSet(asnVector), null, new DerSet(signersVector));
            ContentInfo contentInfo = new ContentInfo(new DerObjectIdentifier("1.2.840.113549.1.7.2"), signedData);
            return contentInfo.GetDerEncoded();
        }

        private StreamWriter AsStreamWriter(MemoryStream ms) => new StreamWriter(ms, _encoding, 1024, true);

        #endregion

        #region V2

        private void SignV2(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open);
            using FileMemory memory = new FileMemory(fs);
            using MemoryStream ms = new MemoryStream();
            using FileMemory outMemory = new FileMemory(ms);
            memory.Position = memory.Length() - 22;
            while (memory.ReadInt() != EndOfCentralDirectory.SIGNATURE)
            {
                memory.Position -= 4 + 1;
            }
            memory.Position -= 4;
            var eocdPosition = memory.Position;
            EndOfCentralDirectory eocd = new EndOfCentralDirectory(memory);
            if (eocd == null)
                return;
            var cd = eocd.OffsetOfCD;
            memory.Position = cd - 16 - 8;
            var d = memory.ReadULong();
            var d2 = memory.ReadString(16);
            var section1 = GetSectionDigests(fs, 0, cd);
            var section3 = GetSectionDigests(fs, cd, eocdPosition);
            var section4 = GetSectionDigests(fs, eocdPosition, fs.Length);

            var digestChunks = section1.Concat(section3).Concat(section4).ToList();

            byte[] bytes = new byte[1 + 4];
            bytes[0] = 0x5a;
            byte[] sizeBytes = BitConverter.GetBytes((uint)digestChunks.Count);
            bytes[1] = sizeBytes[0];
            bytes[2] = sizeBytes[1];
            bytes[3] = sizeBytes[2];
            bytes[4] = sizeBytes[3];
            var digest = _sha.ComputeHash(bytes.Concat(digestChunks.Aggregate((a, b) => a.Concat(b).ToArray())).ToArray());

            uint algorithm = 0x0103;

            APKSignatureSchemeV2 block = new APKSignatureSchemeV2();
            var signer = new APKSignatureSchemeV2.Signer();

            using MemoryStream signedDataMs = new MemoryStream();
            using FileMemory memorySignedData = new FileMemory(signedDataMs);
            var signedData = new APKSignatureSchemeV2.Signer.BlockSignedData();
            signedData.Digests.Add(new APKSignatureSchemeV2.Signer.BlockSignedData.Digest(algorithm, digest));

            signedData.Certificates.Add(_xCert.GetEncoded());

            signedData.Write(memorySignedData);
            signer.SignedData = signedDataMs.ToArray();

            ISigner signerType = SignerUtilities.GetSigner("SHA256WithRSA");
            signerType.Init(true, _privateKey);
            signerType.BlockUpdate(signer.SignedData, 0, signer.SignedData.Length);

            signer.Signatures.Add(new APKSignatureSchemeV2.Signer.BlockSignature(algorithm, signerType.GenerateSignature()));
            signer.PublicKey = _xCert.CertificateStructure.SubjectPublicKeyInfo.GetDerEncoded();
            block.Signers.Add(signer);

            APKSigningBlock signingBlock = new APKSigningBlock();
            signingBlock.Values.Add(block.ToIDValuePair());

            fs.Position = 0;
            outMemory.WriteBytes(memory.ReadBytes(cd));
            signingBlock.Write(outMemory);
            eocd.OffsetOfCD = (int)ms.Position;
            outMemory.WriteBytes(memory.ReadBytes((int)(eocdPosition - cd)));
            eocd.Write(outMemory);

            fs.SetLength(0);
            ms.Position = 0;
            ms.CopyTo(fs);
            fs.Close();
        }

        private List<byte[]> GetSectionDigests(FileStream fs, long startOffset, long endOffset)
        {
            var digests = new List<byte[]>();
            int chunkSize = 1024 * 1024;
            for (long i = startOffset; i < endOffset; i += chunkSize)
            {
                fs.Position = i;
                var size = Math.Min(endOffset - i, chunkSize);
                byte[] bytes = new byte[1 + 4 + size];
                bytes[0] = 0xa5;
                byte[] sizeBytes = BitConverter.GetBytes((uint)size);
                bytes[1] = sizeBytes[0];
                bytes[2] = sizeBytes[1];
                bytes[3] = sizeBytes[2];
                bytes[4] = sizeBytes[3];
                fs.Read(bytes, 5, (int)size);
                digests.Add(_sha.ComputeHash(bytes));
            }
            return digests;
        }

        #endregion
    }
}
