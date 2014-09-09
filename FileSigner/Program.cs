using System;
using System.IO;
using System.Security.Cryptography;

namespace FileSigner
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    Console.WriteLine("Usage: FileSigner -genkey basename|-sign private.key file signature.bin|-verify public.key file signature.bin");
                }
                else
                {
                    if (args[0] == "-genkey")
                    {
                        if (args.Length > 1)
                        {
                            RSA key = new RSACryptoServiceProvider(1024);

                            File.WriteAllText(Path.ChangeExtension(args[1], "pub"), key.ToXmlString(false));
                            File.WriteAllText(Path.ChangeExtension(args[1], "key"), key.ToXmlString(true));
                        }
                        else
                        {
                            Console.WriteLine("Must provide a base name for the keys");
                        }
                    }
                    else if (args[0] == "-sign")
                    {
                        if (args.Length > 3)
                        {
                            RSA key = new RSACryptoServiceProvider();

                            key.FromXmlString(File.ReadAllText(args[1]));

                            byte[] data = File.ReadAllBytes(args[2]);

                            SHA1 sha1 = new SHA1CryptoServiceProvider();

                            sha1.ComputeHash(data);

                            RSAPKCS1SignatureFormatter formatter = new RSAPKCS1SignatureFormatter(key);

                            File.WriteAllBytes(args[3], formatter.CreateSignature(sha1));
                        }
                        else
                        {
                            Console.WriteLine("Must provide private key, file to sign and output signature file");
                        }
                    }
                    else if (args[0] == "-verify")
                    {
                        if (args.Length > 3)
                        {
                            RSA key = new RSACryptoServiceProvider();

                            key.FromXmlString(File.ReadAllText(args[1]));

                            byte[] data = File.ReadAllBytes(args[2]);
                            RSAPKCS1SignatureDeformatter deformatter = new RSAPKCS1SignatureDeformatter(key);

                            SHA1 sha1 = new SHA1CryptoServiceProvider();

                            sha1.ComputeHash(data);

                            if (deformatter.VerifySignature(sha1, File.ReadAllBytes(args[3])))
                            {
                                Console.WriteLine("Signature matches");
                            }
                            else
                            {
                                Console.WriteLine("Signature does NOT match");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Must provide private key, file to verify and input signature file");
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
