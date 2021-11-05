# FastGithub
github加速神器，解决github打不开、用户头像无法加载、releases无法上传下载、git-clone、git-pull、git-push失败等问题。

### 1 程序下载
* [github-release](https://github.com/dotnetcore/fastgithub/releases)
* Q群1 [307306673](https://qm.qq.com/cgi-bin/qm/qr?k=cx_MgEIvoo1EMkrKg5tXz8vMdtPap3Rw&jump_from=webapi) [已满]
* Q群2 [742376932](https://qm.qq.com/cgi-bin/qm/qr?k=6BBJ1nrJwe1o1E4-NJfwSOP-C4sMGc4q&jump_from=webapi)
  
### 2 部署方式
#### 2.1 windows-x64
* 双击运行FastGithub.UI.exe

#### 2.2 linux-x64
* 执行`./fastgithub`
* 安装cacert/fastgithub.cer到受信任的根证书颁发机构
* 设置系统自动代理为`http://127.0.0.1:38457`，或手动代理http/https为`127.0.0.1:38457`

#### 2.3 macOS-x64
* 双击运行fastgithub
* 安装cacert/fastgithub.cer并设置信任
* 设置系统自动代理为`http://127.0.0.1:38457`，或手动代理http/https为`127.0.0.1:38457`
* [具体配置详情](https://github.com/dotnetcore/FastGithub/blob/master/MacOSXConfig.md)

### 3 加速原理
#### 3.1 windows
1. 客户端访问`https://github.com`
2. 客户端向dns查询github.com的ip，FastGithub拦截dns数据包并伪造解析结果为127.0.0.1
3. 客户端请求到FastGithub的`https://127.0.0.1:443`
4. FastGithub使用fastgithub.cer颁发服务器证书给客户端
5. FastGithub查询和计算github.com最快的ip
6. FastGithub与github.com进行无sni的tls连接
7. FastGithub将请求反向代理到`https://github.com`

#### 3.2 linux/osx
1. 客户端访问`https://github.com`
2. 客户端使用fagithub的代理端口38457代理请求
3. FastGithub将代理的流量请求到自身的反向代理服务
4. FastGithub使用fastgithub.cer颁发服务器证书给客户端
5. FastGithub查询和计算github.com最快的ip
6. FastGithub与github.com进行无sni的tls连接
7. FastGithub将请求反向代理到`https://github.com`
  
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
