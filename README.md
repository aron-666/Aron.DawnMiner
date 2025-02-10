![Total Visitors](https://komarev.com/ghpvc/?username=aron-666dawnMiner&color=green)

[![en](https://img.shields.io/badge/lang-en-red.svg)](https://github.com/aron-666/Aron.DawnMiner/blob/master/Readme.en.md)
[![中文](https://img.shields.io/badge/lang-中文-blue.svg)](https://github.com/aron-666/Aron.DawnMiner)

# Aron.DawnMiner 
使用.Net 8撰寫


## 好用請支持，使用我的推薦碼註冊: ly90oppy
[立即註冊 dawn](https://chromewebstore.google.com/detail/dawn-validator-chrome-ext/fpdkjdnhkakefebpekbdhillbhonfjjp)


## 執行畫面
1. 登入
![image](https://github.com/aron-666/Aron.DawnMiner/blob/main/%E6%88%AA%E5%9C%96/%E5%BE%8C%E8%87%BA%E7%99%BB%E5%85%A5%E7%95%AB%E9%9D%A2.png?raw=true)

2. 挖礦資訊
![image](https://github.com/aron-666/Aron.DawnMiner/blob/main/%E6%88%AA%E5%9C%96/%E6%8C%96%E7%A4%A6%E7%95%AB%E9%9D%A2.png?raw=true)

## 1. Docker 安裝
1. 安裝 Docker
   - Windows: [Docker Desktop](https://www.docker.com/products/docker-desktop/)
   - Linux: 你都會用Linux了還要我教？


2. 編輯 docker-compose.yml 
   ```
   version: '1'
   services:
      dawn:
         image: aron666/dawn
         container_name: dawn
         environment:
           - DW_USER=user
           - DW_PASS=password
           - ADMIN_USER=admin
           - ADMIN_PASS=admin
           - PROXY_ENABLE=false # true
           - PROXY_HOST=http(s)://host:port
           - PROXY_USER=user
           - PROXY_PASS=pass
         volumes:
           - ./data:/app/data
         ports:
           - 5006:50006
   ```

   - Port 5006 會在你電腦上開一個 Port，要讓區網連請開防火牆 Port 5006
   

3. 執行
   ```
   //cmd請先 cd 到資料夾目錄(docker-install)
   docker compose up -d
   或
   docker-compose up -d
   ```
   再來就可以用網址看後臺狀態了，第一次登入需要輸入```驗證碼！```

   - 本機: [http://localhost:5006](http://localhost:5006)
   - 其他設備: 先開 cmd 打 `ipconfig`/`ifconfig` 找到你的區網 IP [http://IP:5006](http://IP:5006)
     - 關掉網頁還會繼續執行
     - Windows 要開機自動執行要去Docker Desktop設定改

   ![image](https://github.com/aron-666/Aron.DawnMiner/blob/main/%E6%88%AA%E5%9C%96/%E9%A9%97%E8%AD%89%E7%A2%BC.png?raw=true)


# 支持此項目

如果您覺得這個自動挖礦機器人對您有所幫助，並希望支持我繼續開發，歡迎您：

☕ **請我喝杯咖啡！** ☕

您的支持就像一杯香醇的咖啡，讓我充滿能量繼續努力寫程式！

### 咖啡地址
- [!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/Aron666)
- **BEP20（USDT/BNB 等）：** `0xd14ee77edb0a052eb955db6fcd2a1cdcafeef53e`
- **TRC20（USDT 等）：** `THrEH2tKHxCUiSiuFpGhU99Y4QdChW8dub`

感謝您的慷慨支持！ 🙌
