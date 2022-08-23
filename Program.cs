using ComputerUtils.Logging;

namespace SecureServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.displayLogInConsole = true;
            SecureServer.StartServer();
        }
    }
}