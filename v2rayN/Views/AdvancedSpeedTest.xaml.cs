using System.DirectoryServices.ActiveDirectory;
using NLog;
using ReactiveUI;
using System.IO;
using System.Net.NetworkInformation;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Windows;
using Common.Xui;
using Renci.SshNet;
using v2rayN.Helpers;
using v2rayN.Helpers.ServerConfigHelper;
using v2rayN.Mode;
using v2rayN.ViewModels;
using System.Windows.Controls;
using v2rayN.Helpers.Xui.Model;
using System.Net;
using System.Security.Authentication;
using System.Net.Http;
using System.Reflection;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using v2rayN.Base;
using v2rayN.Helpers.TlsHelpers;
using v2rayN.Helpers.TlsHelpers.Models;
using v2rayN.Helpers.AdvancedSpeedTestHelpers;

namespace v2rayN.Views
{
    public partial class AdvancedSpeedTest
    {
        private readonly MainWindow mainWindow;

        public AdvancedSpeedTest(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            InitializeComponent();
            this.Owner = Application.Current.MainWindow;
            this.Loaded += Window_Loaded;



            //this.WhenActivated(disposables =>
            //{
            //    this.Bind(ViewModel, vm => vm.SelectedSource.remarks, v => v.txtSshIP.Text).DisposeWith(disposables);
            //    this.Bind(ViewModel, vm => vm.SelectedSource.url, v => v.txtSshUsername.Text).DisposeWith(disposables);
            //    this.Bind(ViewModel, vm => vm.SelectedSource.moreUrl, v => v.txtSshPassword.Text).DisposeWith(disposables);
            //    this.Bind(ViewModel, vm => vm.SelectedSource.autoUpdateInterval, v => v.txtDomains.Text).DisposeWith(disposables);

            //    this.BindCommand(ViewModel, vm => vm.SaveCmd, v => v.btnSave).DisposeWith(disposables);

            //});
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtSshIP.Focus();
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            txtReport.Text = "";
            Log("starting...");
            var ip = txtSshIP.Text;
            var username = txtSshUsername.Text;
            var password = txtSshPassword.Text;
            var domains = txtDomains.Text;
            Task.Factory.StartNew(async () =>
            {


                Log("cheking if xui is installed");

                var installed = XuiHelper.IsXuiInstalled(ip, username, password);
                if (!installed)
                {
                    Log("Installing xui");

                    await ServerConfig.ConfigServer(ip, username, password,
                         Log);

                }
                var lst = new List<XuiAddUserModel>();
                var lstDomains = domains.Split("\n").ToList().Where(q => !q.IsNullOrEmpty()).Select(q => q.Trim());
                foreach (var item in lstDomains)
                {
                    Log($"{item} - Tesing TLSv1.3");
                    if (!await TlsHelper.DomainIsTlsOne13(item))
                    {
                        Log($"{item} - Do not have TLSv1.3");

                        continue;
                    }
                    Log($"{item} - Making Reality in XUI");

                    var userModel = new XuiAddUserModel
                    {
                        Remark = Guid.NewGuid().ToString("N"),
                        Port = "443",
                        TestingDomain = item,
                        Email = Guid.NewGuid().ToString("N"),
                        UUID = Guid.NewGuid().ToString(),
                    };
                    lst.Add(userModel);
                    await XuiHelper.DeleteUser(ip, 1);
                    await XuiHelper.AddUser(ip, userModel);

                    var configText = XuiHelper.GetVlessString(ip, userModel);
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Log($"{item} - Connecting to server");

                        mainWindow.ViewModel?.AddServerViaText(configText);
                        var recent = mainWindow.ViewModel?.ProfileItems.First(q => q.remarks == userModel.Remark);
                        mainWindow.ViewModel?.SetDefaultServer(recent.indexId);
                    }));

                    await Task.Delay(2000);
                    Log($"{item} - testing upload");

                    var uploadSpeedTestResult = await AdvancedSpeedTestHelper.UploadTest.Run(Log);
                    Log($"{item} - Upload Result: {uploadSpeedTestResult.MegabitsPerSecond}mbps {uploadSpeedTestResult.MegabytesPerSecond}MBps");
                    Log($"{item} - testing download");
                    var downloadSpeedTestResult = await AdvancedSpeedTestHelper.DownloadTest.Run(Log);
                    Log($"{item} - Download Result: {downloadSpeedTestResult.MegabitsPerSecond}mbps {downloadSpeedTestResult.MegabytesPerSecond}MBps");
                }
            });
        }
        private void Log(string s)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtReport.Text += $"{s}\n";
                txtReport.ScrollToEnd();

            }));
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
