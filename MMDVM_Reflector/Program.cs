using P25_Reflector;
using YSF_Reflector;

#nullable disable

namespace MMDVM_Reflector
{
    internal class Program
    {
        static void Main(string[] args)
        {
            P25Reflector p25Reflector = null;
            YSFReflector ysfReflector = null;
            
            Console.WriteLine("MMDVM Reflector Suite");

            string configFilePath = "config.yml";
            var configArg = args.FirstOrDefault(arg => arg.StartsWith("--config="));
            if (configArg != null)
            {
                configFilePath = configArg.Split('=')[1];
            }

            GlobalConfig config;
            try
            {
                config = GlobalConfig.Load(configFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config file: {ex.Message}");
                return;
            }

            if (config.Reflectors != null)
            {
                if (config.Reflectors.P25.Enabled)
                {
                    p25Reflector = new P25Reflector(config.Reflectors.P25);
                    p25Reflector.Run();
                }

                if (config.Reflectors.Ysf.Enabled)
                {
                    ysfReflector = new YSFReflector(config.Reflectors.Ysf);
                    ysfReflector.Run();
                }
            }

            Console.ReadLine();

            if (config.Reflectors.P25.Enabled && p25Reflector != null)
                p25Reflector.Stop();

            if (config.Reflectors.Ysf.Enabled && ysfReflector != null)
                ysfReflector.Stop();
        }
    }
}
