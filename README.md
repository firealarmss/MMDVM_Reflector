# MMDVM Reflector: YSF, P25, and NXDN Reflector based on MMDVM written by Caleb KO4UYJ.

[![License](https://img.shields.io/badge/License-GPLv3-blue?style=for-the-badge)](https://www.gnu.org/licenses/gpl-3.0)

## Debian 12 Basic Install (might work on debian 11, not tested):

The script downloads dotnet for compiling, compiles the app, and creates a daemon file so it can run in the background.

 - `sudo -s`
 - `apt update && apt upgrade`
 - `apt install git`
 - `cd /`
 - `git clone https://github.com/VALER24/MMDVM_Reflector`
 - `cd MMDVM_Reflector`
 - `chmod +x installer.sh`
 - `./installer.sh`

Linux/Debian Manual Compliling:

Follow the intructions to install dotnet on your current debian version here: https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian

1. Change into main directory `cd /`
3. Clone the app `git clone https://github.com/firealarmss/MMDVM_Reflector.git`
4. Change directory into the app`cd MMDVM_Reflector`
5. Compile the app `dotnet compile`
6. Run the app: `./MMDVM_Reflector/MMDVM_Reflector/bin/Debug/net8.0/MMDVM_Reflector --config=/MMDVM_Reflector/MMDVM_Reflector/configs/config.yml`

## Enable/Disable modes and change ports under MMDVM_Reflector/configs/config.yml
