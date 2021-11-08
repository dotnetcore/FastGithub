#! /bin/bash
cd ./publish
 
# win-x64
zip -r fastgithub_win-x64.zip fastgithub_win-x64

# linux-x64
chmod 777 ./fastgithub_linux-x64/fastgithub
chmod 777 ./fastgithub_linux-x64/dnscrypt-proxy/dnscrypt-proxy
zip -r fastgithub_linux-x64.zip fastgithub_linux-x64

# linux-arm64
chmod 777 ./fastgithub_linux-arm64/fastgithub
chmod 777 ./fastgithub_linux-arm64/dnscrypt-proxy/dnscrypt-proxy
zip -r fastgithub_linux-arm64.zip fastgithub_linux-arm64

# osx-x64
chmod 777 ./fastgithub_osx-x64/fastgithub
chmod 777 ./fastgithub_osx-x64/dnscrypt-proxy/dnscrypt-proxy
zip -r fastgithub_osx-x64.zip fastgithub_osx-x64
