#!/bin/bash

# Elevate to root
sudo -s <<EOF

# Install git
apt update && apt install git -y

# Download Microsoft package repository configuration
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb

# Install the package
dpkg -i packages-microsoft-prod.deb

# Clean up
rm packages-microsoft-prod.deb

# Update package list and install .NET SDK
apt-get update && \
  apt-get install -y dotnet-sdk-8.0

# Clone the repository
cd /
git clone https://github.com/VALER24/MMDVM_Reflector

# Change to the project directory
cd /mmdvm_reflector

# Compile the .NET project
dotnet build

# Copy the systemd service file
cp mmdvm_reflector.service /etc/systemd/system/mmdvm_reflector.service

# Enable and reload the systemd service
systemctl enable mmdvm_reflector.service
systemctl daemon-reload

# Inform the user of completion
echo "Done. Config file is /MMDVM_Reflector/MMDVM_Reflector/configs/config.yml, please enable the modes you want to use and what ports you want to set."
echo "To start the reflector run: systemctl start mmdvm_reflector"

EOF

