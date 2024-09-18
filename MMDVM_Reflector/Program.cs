using NXDN_Reflector;
using P25_Reflector;
using System.Threading;
using YSF_Reflector;

#nullable disable

namespace MMDVM_Reflector
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            P25Reflector p25Reflector = null;
            YSFReflector ysfReflector = null;
            NXDNReflector nxdnReflector = null;

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

            CancellationTokenSource cts = new CancellationTokenSource();

            if (config.Reflectors != null)
            {
                if (config.Reflectors.P25.Enabled)
                {
                    Console.WriteLine("Starting P25Reflector");
                    p25Reflector = new P25Reflector(config.Reflectors.P25);
                    p25Reflector.Run();
                }

                if (config.Reflectors.Ysf.Enabled)
                {
                    Console.WriteLine("Starting YSFReflector");
                    ysfReflector = new YSFReflector(config.Reflectors.Ysf);
                    ysfReflector.Run();
                }

                if (config.Reflectors.Nxdn.Enabled)
                {
                    Console.WriteLine("Starting NXDNReflector");
                    nxdnReflector = new NXDNReflector(config.Reflectors.Nxdn);
                    nxdnReflector.Run();
                }
            }

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    Console.ReadKey(true);
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                Console.WriteLine("Stopping reflectors...");
                if (config.Reflectors.P25.Enabled && p25Reflector != null)
                    p25Reflector.Stop();

                if (config.Reflectors.Ysf.Enabled && ysfReflector != null)
                    ysfReflector.Stop();

                if (config.Reflectors.Nxdn.Enabled && nxdnReflector != null)
                    nxdnReflector.Stop();

                Console.WriteLine("Reflectors stopped.");
            }
        }
    }
}
