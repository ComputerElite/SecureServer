using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using ComputerUtils.Webserver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SecureServer
{
    
    public class SecureServer
    {
        public static HttpServer server = new HttpServer();
        public static string frontendFolder = "frontend" + Path.DirectorySeparatorChar;
        public static string encryptedFolder = frontendFolder + "encrypted" + Path.DirectorySeparatorChar;
        public static string decryptedFolder = frontendFolder + "decrypted" + Path.DirectorySeparatorChar;
        public static string key = "abcdefghabcdefgh";

        public static void PrepareFiles(string directory)
        {
            string currentDir = decryptedFolder + directory + Path.DirectorySeparatorChar;
            string currentOutDir = encryptedFolder + directory + Path.DirectorySeparatorChar;


            string doubleDirSepChar = Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString();

            if (currentDir.EndsWith(doubleDirSepChar)) currentDir = currentDir.Substring(0, currentDir.Length - 1);
            if (currentOutDir.EndsWith(doubleDirSepChar)) currentOutDir = currentOutDir.Substring(0, currentOutDir.Length - 1);

            if (!Directory.Exists(currentDir)) return;
            Logger.Log("Proceeding with directory " + directory, LoggingType.Important);
            FileManager.RecreateDirectoryIfExisting(currentOutDir);
            foreach (string file in Directory.GetFiles(currentDir))
            {
                string f = currentOutDir + Path.GetFileName(file);
                Logger.Log("Encrypting " + f, LoggingType.Important);
                File.WriteAllBytes(f, EncryptFile(File.ReadAllBytes(currentDir + Path.GetFileName(file)), key));
            }
            foreach(string d in Directory.GetDirectories(currentDir))
            {
                string dir = directory + Path.GetFileName(d);
                Logger.Log(dir, LoggingType.Debug);
                PrepareFiles(dir);
            }

        }

        public static void StartServer()
        {
            FileManager.CreateDirectoryIfNotExisting(frontendFolder);
            FileManager.CreateDirectoryIfNotExisting(encryptedFolder);
            FileManager.CreateDirectoryIfNotExisting(decryptedFolder);

            Logger.Log("Encrypting files", LoggingType.Important);
            PrepareFiles("");
            Logger.Log("Encrypted files. Use key '" + key + "' to decrypt", LoggingType.Important);

            server.DefaultCacheValidityInSeconds = 0;

            server.AddRoute("GET", "/encrypt", new Func<ServerRequest, bool>(request =>
            {
                if (File.Exists("encrypted.txt")) File.Delete("encrypted.txt");
                File.WriteAllBytes("encrypted.txt", EncryptFile(File.ReadAllBytes("toEncrypt.txt"), request.queryString.Get("key")));
                request.SendString("Encrypted file");
                return true;
            }));
            server.AddRoute("GET", "/decrypt", new Func<ServerRequest, bool>(request =>
            {
                if (File.Exists("decrypted.txt")) File.Delete("decrypted.txt");
                File.WriteAllBytes("decrypted.txt", DecryptFile(File.ReadAllBytes("encrypted.txt"), request.queryString.Get("key")));
                request.SendString("Decrypted file");
                return true;
            }));
            server.AddRoute("GET", "/encrypted/", new Func<ServerRequest, bool>(request =>
            {
                string file = encryptedFolder + request.pathDiff.Replace('/', Path.DirectorySeparatorChar);
                Logger.Log("decrypting " + file);
                if (!File.Exists(file))
                {
                    request.Send404();
                    return true;
                }
                if(request.cookies["key"] == null)
                {
                    request.Redirect("/");
                    return true;
                }
                string k = request.cookies["key"].Value;
                if(k.Length < 16)
                {
                    request.Redirect("/");
                    return true;
                }
                
                request.SendData(DecryptFile(File.ReadAllBytes(file), k), HttpServer.GetContentTpe(file));
                return true;
            }), true, true, true);
            server.AddRouteFile("/", frontendFolder + "index.html");
            server.AddRouteFile("/style.css", frontendFolder + "style.css");
            server.StartServer(508);
        }

        public static byte[] DecryptFile(byte[] inFile, string key)
        {
            MemoryStream fIn = new MemoryStream(inFile);
            MemoryStream fOut = new MemoryStream();

            //Create variables to help with read and write.
            byte[] bin = new byte[100]; //This is intermediate storage for the encryption.
            long rdlen = 0;              //This is the total number of bytes written.
            long totlen = fIn.Length;    //This is the total length of the input file.
            int len;                     //This is the number of bytes to be written at a time.
            try
            {
                Aes aes = Aes.Create();
                CryptoStream encStream = new CryptoStream(fOut, aes.CreateDecryptor(Encoding.UTF8.GetBytes(key.Substring(0, 16)), new byte[16]), CryptoStreamMode.Write);

                while (rdlen < totlen)
                {
                    len = fIn.Read(bin, 0, 100);
                    encStream.Write(bin, 0, len);
                    rdlen = rdlen + len;
                }

                encStream.Close();
            }
            catch (Exception e)
            {
                Logger.Log("Error while decrypting: " + e.ToString(), LoggingType.Error);
            }

            fOut.Close();
            fIn.Close();
            return fOut.ToArray();
        }

        public static byte[] EncryptFile(byte[] inFile, string key)
        {
            MemoryStream fIn = new MemoryStream(inFile);
            MemoryStream fOut = new MemoryStream();

            //Create variables to help with read and write.
            byte[] bin = new byte[100]; //This is intermediate storage for the encryption.
            long rdlen = 0;              //This is the total number of bytes written.
            long totlen = fIn.Length;    //This is the total length of the input file.
            int len;                     //This is the number of bytes to be written at a time.
            try
            {
                Aes aes = Aes.Create();
                CryptoStream encStream = new CryptoStream(fOut, aes.CreateEncryptor(Encoding.UTF8.GetBytes(key.Substring(0, 16)), new byte[16]), CryptoStreamMode.Write);

                while (rdlen < totlen)
                {
                    len = fIn.Read(bin, 0, 100);
                    encStream.Write(bin, 0, len);
                    rdlen = rdlen + len;
                }

                encStream.Close();
            }
            catch (Exception e)
            {
                Logger.Log("Error while encrypting: " + e.ToString(), LoggingType.Error);
            }

            fOut.Close();
            fIn.Close();
            return fOut.ToArray();
        }
    }
}
