![Total Visitors](https://komarev.com/ghpvc/?username=aron-666dawnMiner&color=green)

[![en](https://img.shields.io/badge/lang-en-red.svg)](https://github.com/aron-666/Aron.DawnMiner/blob/master/README.en.md)
[![中文](https://img.shields.io/badge/lang-中文-blue.svg)](https://github.com/aron-666/Aron.DawnMiner)

# Aron.DawnMiner
Written in .Net 8

## If you find it useful, support me by using my referral code: ly90oppy
[Register Now at dawn](https://chromewebstore.google.com/detail/dawn-validator-chrome-ext/fpdkjdnhkakefebpekbdhillbhonfjjp)



## Execution Screenshots
1. Login
![image](https://github.com/aron-666/Aron.DawnMiner/blob/master/%E6%88%AA%E5%9C%96/%E5%BE%8C%E8%87%BA%E7%99%BB%E5%85%A5%E7%95%AB%E9%9D%A2.png?raw=true)

2. Mining Information
![image](https://github.com/aron-666/Aron.DawnMiner/blob/master/%E6%88%AA%E5%9C%96/%E6%8C%96%E7%A4%A6%E7%95%AB%E9%9D%A2.png?raw=true)

## 1. Docker Installation
1. Install Docker
   - Windows: [Docker Desktop](https://www.docker.com/products/docker-desktop/)
   - Linux: If you're using Linux, you probably know how to do this already.

2. Edit docker-compose.yml (In the docker-install folder of the source code)
   ```
   version: '1'
   services:
      dawn:
         image: aron666/dawn
         container_name: dawn
         environment:
            - DW_TOKEN=token
            - ADMIN_USER=admin
            - ADMIN_PASS=admin
            - PROXY_ENABLE=true # false
            - PROXY_HOST=http(s)://host:port
            - PROXY_USER=user
            - PROXY_PASS=pass
         ports:
            - 5006:50006
   ```

   - Port 5006 will open a port on your computer. Open firewall port 5006 for LAN access.
   - DW_TOKEN: 
   1. Open your browser and go to ```chrome-extension://fpdkjdnhkakefebpekbdhillbhonfjjp/signin.html``` and log in.
   2. Type "allow pasting" then paste the following and press enter:
   ```
   chrome.storage.local.get('token', (data) => {
      console.log(data.token);
   });
   ```

3. Execute
   ```
   //cmd, navigate to the directory first (docker-install)
   docker compose up -d
   or
   docker-compose up -d
   ```
   Then, you can check the backend status using the following URLs:

   - Local: [http://localhost:5006](http://localhost:5006)
   - Other devices: Open cmd and type `ipconfig`/`ifconfig` to find your LAN IP, then access [http://IP:5006](http://IP:5006)
     - The process continues even if the webpage is closed.
     - For Windows auto-start, adjust settings in Docker Desktop.


