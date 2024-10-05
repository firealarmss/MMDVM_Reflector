# MMDVM Reflector: YSF, P25, NXDN, and M17 Reflector based on MMDVM Projects

[![License](https://img.shields.io/badge/License-GPLv3-blue?style=for-the-badge)](https://www.gnu.org/licenses/gpl-3.0)

## Debian Basic Scripted Install:

The script downloads dotnet for compiling, compiles the app, and creates a service file so it can run in the background.

 - `sudo apt update && sudo apt upgrade && sudo apt install git`
 - `cd /opt`
 - `sudo git clone https://github.com/firealarmss/MMDVM_Reflector`
 - `cd MMDVM_Reflector/debian`
 - `sudo chmod +x install.sh`
 - `sudo ./install.sh`

## Linux Manual Compliling:

Follow the intructions to install dotnet on your current debian version here: https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian

1. Change into the default directory `cd /opt`
3. Clone the app `git clone https://github.com/firealarmss/MMDVM_Reflector.git`
4. Change directory into the app`cd MMDVM_Reflector`
5. Compile the app `dotnet compile`
6. Run the app: `./opt/MMDVM_Reflector/MMDVM_Reflector/bin/Debug/net8.0/MMDVM_Reflector --config=/opt/MMDVM_Reflector/MMDVM_Reflector/configs/config.yml`

## Windows Manual Compliling

clone the repo then open in VS22 and compile
