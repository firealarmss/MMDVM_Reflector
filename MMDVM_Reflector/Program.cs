/*
* MMDVM_Reflector
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
* 
* Copyright (C) 2024 Caleb, KO4UYJ
* 
*/

using Common.Api;
using NXDN_Reflector;
using P25_Reflector;
using Serilog;
using System.Threading;
using YSF_Reflector;

#nullable disable

namespace MMDVM_Reflector
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Reporter reporter = new Reporter();

            P25Reflector p25Reflector = null;
            YSFReflector ysfReflector = null;
            NXDNReflector nxdnReflector = null;

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

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File($"{config.Logger.Path}mmdvm_reflector.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            CancellationTokenSource cts = new CancellationTokenSource();

            Log.Logger.Information("MMDVM Reflector Suite" +
                "\n             This software is intended for ham radio use only" +
                "\n             Copyright 2024 Caleb, KO4UYJ");

            if (config.Reporter != null)
                reporter = new Reporter(config.Reporter.Ip, config.Reporter.Port, config.Reporter.Enabled, Log.Logger);

            if (config.Reflectors != null)
            {
                if (config.Reflectors.P25.Enabled)
                {
                    p25Reflector = new P25Reflector(config.Reflectors.P25, reporter, Log.Logger);
                    p25Reflector.Run();
                }

                if (config.Reflectors.Ysf.Enabled)
                {
                    ysfReflector = new YSFReflector(config.Reflectors.Ysf, reporter, Log.Logger);
                    ysfReflector.Run();
                }

                if (config.Reflectors.Nxdn.Enabled)
                {
                    nxdnReflector = new NXDNReflector(config.Reflectors.Nxdn, reporter, Log.Logger);
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
