using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;
using System.Net;
using System.Drawing;
using Newtonsoft.Json;
using System.Text;
using Aron.DawnMiner.Models;

namespace Aron.DawnMiner.Services
{
    public class MinerService : IMinerService
    {
        public ChromeDriver driver { get; set; }
        private readonly AppConfig _appConfig;
        private readonly MinerRecord _minerRecord;
        private readonly string extensionPath = "./Dawn.crx";
        private readonly string extensionId = "fpdkjdnhkakefebpekbdhillbhonfjjp";
        private bool Enabled { get; set; } = true;

        private Thread? thread;

        private DateTime BeforeRefresh = DateTime.MinValue;
        public MinerService(AppConfig appConfig, MinerRecord minerRecord)
        {
            _appConfig = appConfig;
            _minerRecord = minerRecord;
            // call https://ifconfig.me to get the public IP address
            try
            {
                _minerRecord.PublicIp = new WebClient().DownloadString("https://ifconfig.me");
            }
            catch (Exception ex)
            {
                _minerRecord.PublicIp = "Error to get your public ip.";
            }

            thread = new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (Enabled)
                        {
                            await Run();
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _minerRecord.Exception = ex.ToString();
                        _minerRecord.ExceptionTime = DateTime.Now;
                        _minerRecord.Status = MinerStatus.Error;
                    }
                    finally
                    {
                        await Task.Delay(10000);
                    }
                }

            })
            { IsBackground = true };

            thread.Start();
        }

        public void Stop()
        {
            Enabled = false;
        }

        public void Start()
        {

            Enabled = true;

        }

        private async Task Run()
        {
            try
            {
                driver?.Close();
                driver?.Quit();
                driver?.Dispose();
                driver = null;
                _minerRecord.Status = MinerStatus.AppStart;
                _minerRecord.IsConnected = false;
                _minerRecord.LoginUserName = null;
                _minerRecord.ReconnectSeconds = 0;
                _minerRecord.ReconnectCounts = 0;
                _minerRecord.Exception = null;
                _minerRecord.ExceptionTime = null;
                _minerRecord.Points = "0";

                // get assembly version
                _minerRecord.AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();


                // 設定 Chrome 擴充功能路徑
                string chromedriverPath = "/usr/bin/chromedriver";

                // 建立 Chrome 選項
                ChromeOptions options = new ChromeOptions();
                if (!_appConfig.ShowChrome)
                    options.AddArgument("--headless=new");
                options.AddArgument("--no-sandbox");
                //options.AddArgument("--enable-javascript");
                options.AddArgument("--auto-close-quit-quit");
                options.AddArgument("disable-infobars");
                options.AddArgument("--window-size=1024,768");
                if ((_appConfig.ProxyEnable ?? "").ToLower() == "true"
                    && !string.IsNullOrEmpty(_appConfig.ProxyHost))
                {
                    options.AddArgument("--proxy-server=" + _appConfig.ProxyHost);
                    if (!string.IsNullOrEmpty(_appConfig.ProxyUser) && !string.IsNullOrEmpty(_appConfig.ProxyPass))
                    {
                        options.AddArgument($"--proxy-auth={_appConfig.ProxyUser}:{_appConfig.ProxyPass}");
                    }
                }
                options.AddExcludedArgument("enable-automation");
                options.AddUserProfilePreference("credentials_enable_service", false);
                options.AddUserProfilePreference("profile.password_manager_enabled", false);
                options.AddArgument("--disable-gpu"); // 禁用 GPU 加速，减少资源占用
                options.AddArgument("--disable-software-rasterizer"); // 禁用软件光栅化器
                options.AddArgument("--disable-dev-shm-usage"); // 禁用 /dev/shm 临时文件系统
                options.AddArgument("--disable-notifications");
                options.AddArgument("--disable-popup-blocking");
                options.AddArgument("--disable-infobars");
                options.AddArgument("--renderer-process-limit=3");
                //options.AddArgument("--force-dark-mode");
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36 Edg/121.0.0.0");

                options.AddExtension(extensionPath);


                // 建立 Chrome 瀏覽器
                if (!File.Exists(chromedriverPath))
                {
                    chromedriverPath = "./chromedriver";
                    options.AddArgument("--chromedriver=" + chromedriverPath);
                    driver = new ChromeDriver(options);

                }
                else
                    driver = new ChromeDriver(chromedriverPath, options);
                try
                {

                    _minerRecord.Status = MinerStatus.LoginPage;
                    await Login();



                }
                catch (Exception ex)
                {
                    _minerRecord.Status = MinerStatus.LoginError;
                    _minerRecord.Exception = ex.ToString();
                    _minerRecord.ExceptionTime = DateTime.Now;
                    Console.WriteLine(ex);
                    return;
                }




                driver.Manage().Window.Size = new Size(500, 768);

                _minerRecord.Status = MinerStatus.Disconnected;
                while (Enabled)
                {
                    try
                    {
                        // 找到 video class="new-img" 移除它
                        // 幹 就是它 給我吃一堆CPU
                        try
                        {
                            driver.ExecuteScript("document.querySelectorAll('video.new-img').forEach(e => e.remove());");
                        }
                        catch
                        {

                        }

                        if (!driver.PageSource.Contains("Connected"))
                        {

                            _minerRecord.Status = MinerStatus.Disconnected;
                            _minerRecord.IsConnected = false;
                            _minerRecord.ReconnectCounts++;
                        }
                        else
                        {
                            _minerRecord.Status = MinerStatus.Connected;
                            // 取得span[id='netwokquality_']
                            IWebElement? networkQualityElement = driver.FindElement(By.Id("netwokquality_"));
                            _minerRecord.NetworkQuality = networkQualityElement.Text;
                            // 取得span[id='dawnbalance']
                            IWebElement? pointsElement = driver.FindElement(By.Id("dawnbalance"));
                            _minerRecord.Points = pointsElement.Text;

                            //IWebElement? userNameElement = driver.FindElement(By.CssSelector("span[title='Username']"));
                            _minerRecord.IsConnected = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        _minerRecord.Status = MinerStatus.Connected;
                    }
                    finally
                    {
                        int countdownSeconds = 30;

                        // 倒數計時
                        while (countdownSeconds > 0)
                        {
                            _minerRecord.ReconnectSeconds = countdownSeconds;

                            SpinWait.SpinUntil(() => false, 3000); // 等待 3 秒
                            if (driver.PageSource.Contains("Connected"))
                                break;
                            countdownSeconds -= 3;
                            if (!Enabled)
                            {
                                break;
                            }
                        }
                        // 20-35 分鐘後重新整理
                        if (Enabled && BeforeRefresh.AddMinutes(15 + new Random().Next(5, 20)) <= DateTime.Now)
                        {
                            BeforeRefresh = DateTime.Now;
                            //refresh
                            driver.Navigate().GoToUrl($"chrome-extension://{extensionId}/pages/dashboard.html");

                            try
                            {
                                // 等待video class="new-img" 出現
                                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                                driver.ExecuteScript("document.querySelectorAll('video.new-img').forEach(e => e.remove());");
                            }
                            catch
                            {

                            }
                            SpinWait.SpinUntil(() => !Enabled, 15000);
                        }
                        await Task.Delay(5000);
                    }
                }
                _minerRecord.Status = MinerStatus.Stop;
            }
            catch (Exception ex)
            {
                _minerRecord.Exception = ex.ToString();
                _minerRecord.ExceptionTime = DateTime.Now;
                _minerRecord.Status = MinerStatus.Error;
                Console.WriteLine(ex);
            }
            finally
            {
                driver?.Close();
                driver?.Quit();
                driver?.Dispose();
                driver = null;
            }
        }

        private async Task Login()
        {
            driver.Navigate().GoToUrl($"chrome-extension://{extensionId}/pages/signin.html");

            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));

            Console.WriteLine("Go to app: " + driver.Url);

            _ = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//h5[text()='Login']")));
            LoginConfig? loginConfig = null;
            // 讀取 data/loginConfig.json
            if (System.IO.File.Exists("data/loginConfig.json"))
                loginConfig = JsonConvert.DeserializeObject<LoginConfig>(System.IO.File.ReadAllText("data/loginConfig.json"));
            string base64Username = Convert.ToBase64String(Encoding.UTF8.GetBytes(_appConfig.UserName));
            if (loginConfig == null || loginConfig.username == _appConfig.UserName || loginConfig.username != base64Username)
            {
                // 未登入，輸入帳號密碼
                IWebElement? usernameElement = driver.FindElement(By.Id("email"));
                usernameElement.SendKeys(_appConfig.UserName);
                IWebElement? passwordElement = driver.FindElement(By.Id("password"));
                passwordElement.SendKeys(_appConfig.Password);


                // 等待驗證碼圖片src有值
                wait.Until(d =>
                {
                    try
                    {
                        string? base64 = (string)driver.ExecuteScript("return document.getElementById('puzzleImage').src;");
                        if (!string.IsNullOrEmpty(base64) && base64.StartsWith("data:image"))
                        {
                            _minerRecord.CaptchaBase64Image = base64;
                            return true;
                        }
                        return false;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });

                // 等待 url 包含 dashboard

                while (true)
                {
                    try
                    {
                        if (driver.Url.Contains("dashboard"))
                        {
                            break;
                        }
                    }
                    catch (Exception)
                    {
                    }


                    await Task.Delay(3000);
                }
                // 等待包含Connection 字段
                wait.Until(d =>
                {
                    try
                    {
                        return driver.PageSource.Contains("Connection");
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });

                // 取得 token

                loginConfig = GetLoginConfig();

                if (loginConfig == null)
                {
                    throw new Exception("Failed to get token");

                }
                else
                {
                    // 寫入 data/loginConfig.json
                    System.IO.File.WriteAllText("data/loginConfig.json", JsonConvert.SerializeObject(loginConfig));

                    // 清除驗證碼
                    _minerRecord.CaptchaBase64Image = null;
                }
            }
            else
            {
                AddToLocalStorage(driver, loginConfig);

                // go to dashboard
                driver.Navigate().GoToUrl($"chrome-extension://{extensionId}/pages/dashboard.html");
                Console.WriteLine("Go to dashboard: " + driver.Url);
            }

        }

        private LoginConfig GetLoginConfig()
        {
            var script = @"
                return new Promise((resolve, reject) => {
                    chrome.storage.local.get(null, (result) => {
                        if (chrome.runtime.lastError) {
                            reject(chrome.runtime.lastError);
                        } else {
                            resolve(JSON.stringify(result));
                        }
                    });
                });
            ";

            var result = driver.ExecuteScript(script);
            return JsonConvert.DeserializeObject<LoginConfig>(result.ToString());
        }


        static void AddToLocalStorage(ChromeDriver driver, LoginConfig loginConfig)
        {
            string json = JsonConvert.SerializeObject(loginConfig);
            json = json.Replace("{token}", loginConfig.token);
            driver.ExecuteScript($"const data = {json}; " +
                $"chrome.storage.local.set(data, () => {{\r\n  // 檢查是否有錯誤發生\r\n  if (chrome.runtime.lastError) {{\r\n    console.error(\"儲存資料時發生錯誤：\", chrome.runtime.lastError);\r\n  }} else {{\r\n    console.log(\"資料已成功儲存到 chrome.storage.local\");\r\n  }}\r\n}});");
            // 等待chrome.storage.local 取得token有值
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            driver.ExecuteScript(@"
                tokenMatchResult = false; // 全局變數儲存比對結果
                chrome.storage.local.get('token', (data) => { 
                    tokenMatchResult = data.token === '" + loginConfig.token + @"'; 
                });
            ");
            bool ret = wait.Until(d =>
            {
                try
                {
                    return (bool)driver.ExecuteScript("return tokenMatchResult;");
                }
                catch (Exception)
                {
                    return false;
                }
            });
            if (!ret)
            {
                throw new Exception("Failed to set token to chrome.storage.local");
            }
        }


        public async Task ApplyCaptcha(string captcha)
        {
            if (driver == null)
                return;
            try
            {

                IWebElement? captchaElement = driver.FindElement(By.Id("puzzelAns"));
                captchaElement.SendKeys(captcha);

                IWebElement? loginButton = driver.FindElement(By.Id("loginButton"));

                // 清除驗證碼輸入
                try
                {

                    // 清除驗證碼圖片src
                    driver.ExecuteScript("document.getElementById('puzzleImage').src = '';");
                    loginButton.Click();
                    await Task.Delay(2000);

                    captchaElement.Clear();

                    // 等待驗證碼圖片src有值
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                    wait.Until(d =>
                    {
                        try
                        {
                            string? base64 = (string)driver.ExecuteScript("return document.getElementById('puzzleImage').src;");
                            if (!string.IsNullOrEmpty(base64) && base64.StartsWith("data:image"))
                            {
                                _minerRecord.CaptchaBase64Image = base64;
                                return true;
                            }
                            return false;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    });
                }
                catch
                {

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void RefreshCaptcha()
        {
            if (driver == null)
                return;
            try
            {
                // 清除驗證碼圖片src
                driver.ExecuteScript("document.getElementById('puzzleImage').src = '';");


                IWebElement? refreshButton = driver.FindElement(By.Id("captcha-Refresh"));
                refreshButton.Click();

                // 等待驗證碼圖片src有值
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                wait.Until(d =>
                {
                    try
                    {
                        string? base64 = (string)driver.ExecuteScript("return document.getElementById('puzzleImage').src;");
                        if (!string.IsNullOrEmpty(base64) && base64.StartsWith("data:image"))
                        {
                            _minerRecord.CaptchaBase64Image = base64;
                            return true;
                        }
                        return false;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
