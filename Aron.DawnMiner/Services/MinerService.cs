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

                string token = _appConfig.DwToken;

                // 設定 Chrome 擴充功能路徑
                string chromedriverPath = "./chromedriver";

                // 建立 Chrome 選項
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--chromedriver=" + chromedriverPath);
                if (!_appConfig.ShowChrome)
                    options.AddArgument("--headless=new");
                options.AddArgument("--no-sandbox");
                //options.AddArgument("--enable-javascript");
                options.AddArgument("--auto-close-quit-quit");
                options.AddArgument("disable-infobars");
                options.AddArgument("--window-size=500,768");
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
                //options.AddArgument("--force-dark-mode");
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36 Edg/121.0.0.0");

                options.AddExtension(extensionPath);


                // 建立 Chrome 瀏覽器
                driver = new ChromeDriver(options);
                try
                {
                    
                    driver.Navigate().GoToUrl($"chrome-extension://{extensionId}/signin.html");
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(50));

                    Console.WriteLine("Go to app: " + driver.Url);

                   _ = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//h5[text()='Login']")));
                    AddToLocalStorage(driver, token);

                    // go to dashboard
                    driver.Navigate().GoToUrl($"chrome-extension://{extensionId}/dashboard.html");
                    Console.WriteLine("Go to dashboard: " + driver.Url);

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

                            SpinWait.SpinUntil(() => false, 1000); // 等待 1 秒
                            if (driver.PageSource.Contains("Connected"))
                                break;
                            countdownSeconds--;
                            if (!Enabled)
                            {
                                break;
                            }
                        }
                        if (Enabled && BeforeRefresh.AddSeconds(60) <= DateTime.Now)
                        {
                            BeforeRefresh = DateTime.Now;
                            //refresh
                            driver.Navigate().GoToUrl($"chrome-extension://{extensionId}/dashboard.html");
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

        static void SetLocalStorageItem(IWebDriver driver, string key, string value)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript($"window.localStorage.setItem('{key}', '{value}');");
        }

        static void SetCookie(IWebDriver driver, string key, string value)
        {
            driver.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie(key, value, "/", DateTime.UtcNow.AddYears(1)));
        }

        static string GetLocalStorageItem(IWebDriver driver, string key)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            return (string)js.ExecuteScript($"return window.localStorage.getItem('{key}');");
        }


        static string SetLocalStorageItem2(ChromeDriver driver, string key, string value)
        {
            driver.ExecuteScript($"localStorage.setItem('{key}', '{value}');");
            var result = driver.ExecuteScript($"return localStorage.getItem('{key}');") as string;
            return result;
        }

        static void AddToLocalStorage(ChromeDriver driver, string token)
        {
            string json = """
                                {
                  "discordid": "discordid",
                  "discordid_points": 5000,
                  "firstname": "",
                  "lastUpdatedTime": "",
                  "password": "", 
                  "referralCode": "abcdefg",
                  "refrral_points": 0,
                  "telegramid": "telegramid",
                  "telegramid_points": 5000,
                  "token": "{token}", 
                  "total_points": 81000,
                  "twitter_x_id": "twitter_x_id",
                  "twitter_x_id_points": 5000,
                  "username": "",
                  "wallet": ""
                }
                """;
            json = json.Replace("{token}", token);
            driver.ExecuteScript($"const data = {json}; " +
                $"chrome.storage.local.set(data, () => {{\r\n  // 檢查是否有錯誤發生\r\n  if (chrome.runtime.lastError) {{\r\n    console.error(\"儲存資料時發生錯誤：\", chrome.runtime.lastError);\r\n  }} else {{\r\n    console.log(\"資料已成功儲存到 chrome.storage.local\");\r\n  }}\r\n}});");
            // 等待chrome.storage.local 取得token有值
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            driver.ExecuteScript(@"
                tokenMatchResult = false; // 全局變數儲存比對結果
                chrome.storage.local.get('token', (data) => { 
                    tokenMatchResult = data.token === '" + token + @"'; 
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

        static bool WaitForElementExists(ChromeDriver driver, By by, int timeout = 10)
        {
            try
            {
                new WebDriverWait(driver, TimeSpan.FromSeconds(timeout)).Until(ExpectedConditions.ElementExists(by));
                return true;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        static IWebElement WaitForElement(IWebDriver driver, By by, int timeout = 10)
        {
            try
            {
                var element = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout)).Until(ExpectedConditions.ElementExists(by));
                return element;
            }
            catch (WebDriverTimeoutException e)
            {
                throw;
            }
        }

    }
}
