# FastGithub
github加速神器，解决github打不开、用户头像无法加载、releases无法上传下载、git-clone、git-pull、git-push失败等问题。

### 1 程序下载
* [github-release](https://github.com/dotnetcore/fastgithub/releases)
* Q群1 [307306673](https://qm.qq.com/cgi-bin/qm/qr?k=cx_MgEIvoo1EMkrKg5tXz8vMdtPap3Rw&jump_from=webapi) [已满]
* Q群2 [742376932](https://qm.qq.com/cgi-bin/qm/qr?k=6BBJ1nrJwe1o1E4-NJfwSOP-C4sMGc4q&jump_from=webapi)
  
### 2 部署方式
#### 2.1 windows-x64桌面
* 双击运行FastGithub.UI.exe 

#### 2.2 windows-x64服务 
* `fastgithub.exe start` // 以windows服务安装并启动
* `fastgithub.exe stop` // 以windows服务卸载并删除

#### 2.3 linux-x64终端
* `sudo ./fastgithub`
* 设置系统自动代理为`http://127.0.0.1:38457`，或手动代理http/https为`127.0.0.1:38457`
  
#### 2.4 linux-x64服务
* `sudo ./fastgithub start` // 以systemd服务安装并启动
* `sudo ./fastgithub stop` // 以systemd服务卸载并删除
* 设置系统自动代理为`http://127.0.0.1:38457`，或手动代理http/https为`127.0.0.1:38457`

#### 2.5 macOS-x64
* 双击运行fastgithub
* 安装cacert/fastgithub.cer并设置信任
* 设置系统自动代理为`http://127.0.0.1:38457`，或手动代理http/https为`127.0.0.1:38457`
* [具体配置详情](https://github.com/dotnetcore/FastGithub/blob/master/MacOSXConfig.md)
 
#### 2.6 docker-compose一键部署
* 准备好docker 18.09, docker-compose.
* 在源码目录下，有一个docker-compose.yaml 文件，专用于在实际项目中，临时使用github.com源码，而做的demo配置。
* 根据自己的需要更新docker-compose.yaml中的sample和build镜像即可完成拉github.com源码加速，并基于源码做后续的操作。
 
### 3 软件功能 
* 提供域名的纯净IP解析；
* 提供IP测速并选择最快的IP；
* 提供域名的tls连接自定义配置；
* google的CDN资源替换，解决大量国外网站无法加载js和css的问题；
  
### 4 证书验证
#### 4.1 git
git操作提示`SSL certificate problem`</br>
需要关闭git的证书验证：`git config --global http.sslverify false`

#### 4.2 firefox
firefox提示`连接有潜在的安全问题`</br>
设置->隐私与安全->证书->查看证书->证书颁发机构，导入cacert/fastgithub.cer，勾选“信任由此证书颁发机构来标识网站”
  

### 5 安全性说明
FastGithub为每台不同的主机生成自颁发CA证书，保存在cacert文件夹下。客户端设备需要安装和无条件信任自颁发的CA证书，请不要将证书私钥泄露给他人，以免造成损失。

### 6 合法性说明
《国际联网暂行规定》第六条规定：“计算机信息网络直接进行国际联网，必须使用邮电部国家公用电信网提供的国际出入口信道。任何单位和个人不得自行建立或者使用其他信道进行国际联网。”
FastGithub本地代理使用的都是“公用电信网提供的国际出入口信道”，从国外Github服务器到国内用户电脑上FastGithub程序的流量，使用的是正常流量通道，其间未对流量进行任何额外加密（仅有网页原有的TLS加密，区别于VPN的流量加密），而FastGithub获取到网页数据之后发生的整个代理过程完全在国内，不再适用国际互联网相关之规定。
