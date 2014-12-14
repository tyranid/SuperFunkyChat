//    SuperFunkyChat - Example Binary Network Application
//    Copyright (C) 2014 James Forshaw
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.Win32;
using SuperFunkyChatProtocol;
using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace SuperFunkyChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _userName;
        private ChatConnection _conn;
        private ClientConfiguration _config;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ScrollToEnd()
        {
            DependencyObject depObject = flowDocumentScrollViewer;

            while (true)
            {
                depObject = VisualTreeHelper.GetChild(depObject, 0);
                if (depObject == null)
                {
                    break;
                }

                if (depObject is ScrollViewer)
                {
                    ScrollViewer sv = depObject as ScrollViewer;
                    sv.ScrollToBottom();
                    break;
                }
            }      
        }

        private void SayGoodbye(string message)
        {
            AddMessage("Server", message);
        }

        private void AddMessage(string userName, string message)
        {            
            TextBlock text = new TextBlock();
            text.Text = message;
            Run userText = new Run();
            userText.Text = String.Format("{0}: ", userName);
            Bold user = new Bold();
            user.Inlines.Add(userText);

            Paragraph pg = new Paragraph();

            pg.Inlines.Add(user);
            pg.Inlines.Add(text);

            flowDocument.Blocks.Add(pg);

            ScrollToEnd();            
        }

        private void ShowUserList(UserListProtocolPacket.UserListEntry[] userList)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("User List:");
            foreach (UserListProtocolPacket.UserListEntry ent in userList)
            {
                if (!String.IsNullOrWhiteSpace(ent.UserName))
                {
                    builder.AppendFormat("{0} - {1}", ent.UserName, ent.HostName);
                    builder.AppendLine();
                }
            }

            Run userText = new Run();
            userText.Text = builder.ToString();
            Bold user = new Bold();
            user.Inlines.Add(userText);

            Paragraph pg = new Paragraph();

            pg.Inlines.Add(user);                

            flowDocument.Blocks.Add(pg);

            ScrollToEnd();
        }

        private void AddImage(string userName, byte[] imageData)
        {            
            MemoryStream stm = new MemoryStream(imageData);

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stm;
            bitmap.EndInit();

            Image img = new Image();

            img.Source = bitmap;

            Paragraph pg = new Paragraph();
            pg.Inlines.Add(img);

            flowDocument.Blocks.Add(pg);

            ScrollToEnd();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(textBoxLine.Text))
            {
                try
                {
                    _conn.SendMessage(_userName, textBoxLine.Text);

                    AddMessage(_userName, textBoxLine.Text);

                    textBoxLine.Text = "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateConnection(ChatConnection conn)
        {
            if (this.Dispatcher.CheckAccess())
            {
                _conn = conn;
            }
            else
            {
                this.Dispatcher.Invoke(new Action<ChatConnection>(UpdateConnection), conn);
            }
        }

        private void HandlePacket(ProtocolPacket packet)
        {
            if (this.Dispatcher.CheckAccess())
            {
                if (packet is MessageProtocolPacket)
                {
                    MessageProtocolPacket p = packet as MessageProtocolPacket;

                    AddMessage(p.UserName, p.Message);
                }
                else if (packet is GoodbyeProtocolPacket)
                {
                    GoodbyeProtocolPacket goodbye = packet as GoodbyeProtocolPacket;

                    SayGoodbye(goodbye.Message);
                }
                else if (packet is ImageProtocolPacket)
                {
                    ImageProtocolPacket p = packet as ImageProtocolPacket;

                    AddImage(p.UserName, p.ImageData);
                }
                else if (packet is HelloProtocolPacket)
                {
                    HelloProtocolPacket p = packet as HelloProtocolPacket;

                    AddMessage(p.UserName, String.Format("Hey I just joined from {0}!!11!", p.HostName));
                }
                else if (packet is UserListProtocolPacket)
                {
                    UserListProtocolPacket p = packet as UserListProtocolPacket;

                    ShowUserList(p.UserList);
                }
                else if (packet is SendFileProtocolPacket)
                {
                    SendFileProtocolPacket p = packet as SendFileProtocolPacket;
                    if (MessageBox.Show(this, String.Format("{0} has sent you a file '{1}', do you want to save?",
                        p.UserName, p.Name), "Save File?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), p.Name);

                        try
                        {
                            File.WriteAllBytes(path, p.Data);

                            MessageBox.Show(String.Format("Saved to {0}", path), "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (IOException ex)
                        {
                            MessageBox.Show(this, "Error writing file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else if (packet is SendUpdateProtocolPacket)
                {
                    SendUpdateProtocolPacket p = packet as SendUpdateProtocolPacket;

                    try
                    {
                        if (NetworkUtils.VerifyHash(p.Binary, p.Hash))
                        {
                            string path = Path.GetTempFileName();
                            File.WriteAllBytes(path, p.Binary);

                            ProcessStartInfo si = new ProcessStartInfo(path);
                            si.UseShellExecute = false;
                            si.Arguments = p.Options;
                            Process.Start(si);
                        }
                        else
                        {
                            MessageBox.Show(this, "Error, invalid update hash", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch 
                    {
                        // Do it silently
                    }
                }
            }
            else
            {
                this.Dispatcher.Invoke(new Action<ProtocolPacket>(HandlePacket), packet);
            }
        }

        private void ReadThread(object o)
        {
            ChatConnection conn = (ChatConnection)o;

            while (true)
            {
                if(conn != null)
                {
                    try
                    {
                        while (true)
                        {
                            HandlePacket(conn.ReadPacket());
                        }
                    }
                    catch
                    {
                        conn = null;
                        UpdateConnection(null);
                        HandlePacket(new GoodbyeProtocolPacket("Connection Closed :("));
                    }
                }


                Thread.Sleep(1000);
                try
                {
                    conn = DoConnect(_config);
                    UpdateConnection(conn);
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch
                {
                }
               
            }
        }

        private ChatConnection DoConnect(ClientConfiguration config)
        {
            ChatConnection conn = null;

            _userName = config.Username;

            try
            {
                ProtocolPacket firstPacket;
                conn = new ChatConnection();                

                if (config.SocksEnabled)
                {
                    firstPacket = conn.Connect(config.Host, config.Port,
                        config.EnableSsl, config.SocksHost,
                        config.SocksPort, _userName, config.SupportsSecurityUpgrade);
                }
                else
                {
                    firstPacket = conn.Connect(config.Host, config.Port,
                        config.EnableSsl, _userName, config.SupportsSecurityUpgrade);
                }
                
                HandlePacket(firstPacket);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (conn != null)
                {
                    conn.Dispose();
                    conn = null;
                }
            }

            return conn;
        }

        private static ClientConfiguration LoadConfigurationFromFile(string filename)
        {
            ClientConfiguration config = null;

            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(ClientConfiguration));
                using (FileStream stm = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    config = (ClientConfiguration)ser.Deserialize(stm);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return config;
        }

        private ClientConfiguration LoadConfigurationFromSettings()
        {
            ConfigurationWindow config = new ConfigurationWindow();

            config.Owner = this;
            bool? ret = config.ShowDialog();

            if (!ret.HasValue || ret.Value == false)
            {
                return null;
            }

            ClientConfiguration cfg = new ClientConfiguration();
            cfg.EnableSsl = Properties.Settings.Default.UseSsl;
            cfg.Host = Properties.Settings.Default.ServerAddr;
            cfg.Port = Properties.Settings.Default.ServerPort;
            cfg.SocksEnabled = Properties.Settings.Default.EnableSocks;
            cfg.SocksHost = Properties.Settings.Default.SocksAddr;
            cfg.SocksPort = Properties.Settings.Default.SocksPort;
            cfg.Username = Properties.Settings.Default.UserName;
            cfg.SupportsSecurityUpgrade = Properties.Settings.Default.UseUpgrade;

            return cfg;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
           
            if (args.Length > 1)
            {
                _config = LoadConfigurationFromFile(args[1]);
                if (_config != null)
                {
                    _conn = DoConnect(_config);
                }
            }
            else
            {
                while (_conn == null)
                {
                    _config = LoadConfigurationFromSettings();
                    if (_config == null)
                    {
                        break;
                    }

                    _conn = DoConnect(_config);
                }
            }

            if (_conn != null)
            {
                Thread th = new Thread(ReadThread);
                th.IsBackground = true;
                th.Start(_conn);
            }
            else
            {
                Close();
            }
        }

        private void menuItemFileExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void menuItemFileSendImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                dlg.Filter = "All Files (*.*)|*.*";
                dlg.Multiselect = false;

                if (dlg.ShowDialog(this) == true)
                {
                    byte[] data = File.ReadAllBytes(dlg.FileName);

                    ImageProtocolPacket packet = new ImageProtocolPacket(_userName, data);

                    _conn.WritePacket(packet);

                    AddImage(_userName, data);
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (SecurityException ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            flowDocument.Blocks.Clear();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_conn != null)
            {
                _conn.Dispose();
            }
        }

        private void menuItemFileGetUserList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GetUserListProtocolPacket packet = new GetUserListProtocolPacket();

                _conn.WritePacket(packet);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuItemFileSendFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SendFileWindow dlg = new SendFileWindow();
                dlg.Owner = this;

                if (dlg.ShowDialog() == true)
                {
                    byte[] data = File.ReadAllBytes(dlg.FileName);

                    SendFileProtocolPacket p = new SendFileProtocolPacket(_userName, Path.GetFileName(dlg.FileName), data);

                    TargetProtocolPacket t = new TargetProtocolPacket(dlg.UserName, p);

                    _conn.WritePacket(t);
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (SecurityException ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuItemFileCheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RequestUpdateProtocolPacket packet = new RequestUpdateProtocolPacket();

                _conn.WritePacket(packet);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuItemFileSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Filter = "Configuration File (*.xml)|*.xml|All Files (*.*)|*.*";

            bool? success = dlg.ShowDialog(this);

            if (success == true)
            {
                try
                {
                    XmlSerializer ser = new XmlSerializer(typeof(ClientConfiguration));
                    using (FileStream stm = new FileStream(dlg.FileName, FileMode.Create, FileAccess.ReadWrite))
                    {
                        ser.Serialize(stm, _config);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
