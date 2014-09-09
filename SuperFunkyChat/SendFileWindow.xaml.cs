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

using System;
using System.Windows;

namespace SuperFunkyChat
{
    /// <summary>
    /// Interaction logic for SendFileWindow.xaml
    /// </summary>
    public partial class SendFileWindow : Window
    {
        public string UserName { get; set; }
        public string FileName { get; set; }

        public SendFileWindow()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Filter = "All Files (*.*)|*.*";
            dlg.Multiselect = false;

            if (dlg.ShowDialog(this) == true)
            {
                textBoxFileName.Text = dlg.FileName;
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(textBoxUserName.Text))
            {
                MessageBox.Show(this, "Must provide a username", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (String.IsNullOrWhiteSpace(textBoxFileName.Text))
            {
                MessageBox.Show(this, "Must provide a filename", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                FileName = textBoxFileName.Text;
                UserName = textBoxUserName.Text;
                DialogResult = true;
                Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(UserName))
            {
                textBoxUserName.Text = UserName;
            }

            if (!String.IsNullOrWhiteSpace(FileName))
            {
                textBoxFileName.Text = FileName;
            }
        }
    }
}
