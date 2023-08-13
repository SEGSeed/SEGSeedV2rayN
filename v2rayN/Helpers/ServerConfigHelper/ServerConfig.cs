using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Threading;
using Common.Xui;
using Renci.SshNet;
using v2rayN.Helpers.Xui.Model.InsertModels;

namespace v2rayN.Helpers.ServerConfigHelper;

internal class ServerConfig
{
    public static async Task ConfigServer(string sshIp, string sshUsername, string sshPassword, Action<string> callback)
    {
        try
        {
            SaveGroupStatus(sshIp, "testing ssh connection", callback);
            using (var client = new SshClient(sshIp, sshUsername, sshPassword))
            {
                client.Connect();
                client.Disconnect();
            }

            var nginxConfFileName = "nginx.conf";
            var nginxConfPath = Utils.GetSSHFilesPath(nginxConfFileName);
            GenerateNginxConfFile(nginxConfPath);

            var updateFileName = "update.sh";
            var updatePath = Utils.GetSSHFilesPath(updateFileName);
            GenerateUpdateFile(updatePath);

            var xuiInstallFileName = "xui-install.sh";
            var xuiInstallPath = Utils.GetSSHFilesPath(xuiInstallFileName);
            GenerateInstallXuiFile(xuiInstallPath);

            var bbrFileName = "bbr.sh";
            var bbrPath = Utils.GetSSHFilesPath(bbrFileName);
            GenerateBbrFile(bbrPath);

            using (var client = new SftpClient(sshIp, sshUsername, sshPassword))
            {
                SaveGroupStatus(sshIp, "connecting to sftp", callback);

                client.Connect();
                SaveGroupStatus(sshIp, "uploading update", callback);

                using (var fileStream = new FileStream(updatePath, FileMode.Open))
                {

                    client.BufferSize = 4 * 1024;
                    client.UploadFile(fileStream, Path.GetFileName(updatePath));
                }

                SaveGroupStatus(sshIp, "uploading nginx.conf", callback);

                using (var fileStream = new FileStream(nginxConfPath, FileMode.Open))
                {

                    client.BufferSize = 4 * 1024;
                    client.UploadFile(fileStream, Path.GetFileName(nginxConfPath));
                }
                SaveGroupStatus(sshIp, "uploading xui-install", callback);

                using (var fileStream = new FileStream(xuiInstallPath, FileMode.Open))
                {

                    client.BufferSize = 4 * 1024;
                    client.UploadFile(fileStream, Path.GetFileName(xuiInstallPath));
                }
                SaveGroupStatus(sshIp, "uploading bbr", callback);

                using (var fileStream = new FileStream(bbrPath, FileMode.Open))
                {

                    client.BufferSize = 4 * 1024;
                    client.UploadFile(fileStream, Path.GetFileName(bbrPath));
                }
                client.Disconnect();
            }
            SaveGroupStatus(sshIp, "connecting to ssh", callback);

            using (var client = new SshClient(sshIp, sshUsername, sshPassword))
            {

                client.Connect();

                SaveGroupStatus(sshIp, "executing update", callback);
                client.RunCommand("ufw disable");
                client.RunCommand($"chmod u+x {updateFileName}");
                var s = client.RunCommand($"./{updateFileName}");
                //client.RunCommand("/opt/vpnserver/vpncmd localhost /server /CMD ConfigSet " + configFileName);

                SaveGroupStatus(sshIp, "executing xui-install", callback);

                client.RunCommand($"chmod u+x {xuiInstallFileName}");
                client.RunCommand($"./{xuiInstallFileName}");
                client.RunCommand($@"/usr/local/x-ui/x-ui setting -username 'admin' -password 'admin'");
                client.RunCommand($"/usr/local/x-ui/x-ui setting -port '54321'");

                //SaveGroupStatus(groupId, "updating xui panel to latest");
                //VmessHelper.UpdateXuiPanelToLatest(model.SshIp);    

                SaveGroupStatus(sshIp, "installing nginx", callback);

                client.RunCommand($"apt install nginx -y");

                client.RunCommand($"mv -v {nginxConfFileName} /etc/nginx/");

                client.RunCommand($"systemctl restart nginx");


                SaveGroupStatus(sshIp, "executing bbr", callback);

                client.RunCommand($"chmod u+x {bbrFileName}");
                client.RunCommand($"./{bbrFileName}");

                SaveGroupStatus(sshIp, "installing lets encrypt", callback);
                client.RunCommand($"apt install letsencrypt -y");


                client.Disconnect();
            }

            using (var client = new SshClient(sshIp, sshUsername, sshPassword))
            {

                client.Connect();
                var s = client.RunCommand("reboot");
                client.Disconnect();

            }

            var numberOfTries = 0;
            SaveGroupStatus(sshIp, "server rebooted, waiting for it to come online. waiting 10 seconds, number of tries: " + numberOfTries, callback);

            Task.Delay(10000).Wait();
            while (true)
            {
                numberOfTries++;
                try
                {
                    SaveGroupStatus(sshIp, "server rebooted, waiting for it to come online. waiting 5 seconds, number of tries: " + numberOfTries, callback);

                    var pinger = new Ping();
                    PingReply reply = pinger.Send(sshIp);
                    var pingable = reply.Status == IPStatus.Success;
                    if (pingable)
                        break;
                }
                catch (Exception e)
                {
                }

                Task.Delay(5000).Wait();
            }

            //SaveGroupStatus(groupId, "updating xui, setting access.log");
            //XuiHelper.UpdateSettings(group.SshIp);

            SaveGroupStatus(sshIp, "restarting xui panel", callback);
            await XuiHelper.RestartPanel(sshIp).ConfigureAwait(false);
            //using (var client = new SshClient(sshIp, sshUsername, sshPassword))
            //{
            //    client.Connect();
            //    SaveGroupStatus(groupId, "setting vpncmd " + configFileName);

            //    client.RunCommand("/opt/vpnserver/vpncmd localhost /server /CMD ConfigSet " + configFileName);
            //    client.Disconnect();
            //}
            //SaveGroupStatus(groupId, "setting dns and certbot");
            //await SetupCloudflare(group.GroupID, group.SshIp);
            //SetupCertbot(group.GroupID, group.SshIp, group.SshUsername, group.SshPassword);

            SaveGroupStatus(sshIp, "done", callback);
        }
        catch (Exception exception)
        {
            SaveGroupStatus(sshIp, "Error: " + exception.Message, callback);

        }

    }
    private static void SaveGroupStatus(string sship, string message, Action<string> callback)
    {
        callback.Invoke($"{sship} - {message}");
    }

    private static void GenerateBbrFile(string path)
    {
        File.WriteAllText(path, @"#!/usr/bin/env bash
#
# Auto install latest kernel for TCP BBR
#
# System Required:  CentOS 6+, Debian8+, Ubuntu16+
#
# Copyright (C) 2016-2021 Teddysun <i@teddysun.com>
#
# URL: https://teddysun.com/489.html
#

cur_dir=""$(cd -P -- ""$(dirname -- ""$0"")"" && pwd -P)""

_red() {
    printf '\033[1;31;31m%b\033[0m' ""$1""
}

_green() {
    printf '\033[1;31;32m%b\033[0m' ""$1""
}

_yellow() {
    printf '\033[1;31;33m%b\033[0m' ""$1""
}

_info() {
    _green ""[Info] ""
    printf -- ""%s"" ""$1""
    printf ""\n""
}

_warn() {
    _yellow ""[Warning] ""
    printf -- ""%s"" ""$1""
    printf ""\n""
}

_error() {
    _red ""[Error] ""
    printf -- ""%s"" ""$1""
    printf ""\n""
    exit 1
}

_exists() {
    local cmd=""$1""
    if eval type type > /dev/null 2>&1; then
        eval type ""$cmd"" > /dev/null 2>&1
    elif command > /dev/null 2>&1; then
        command -v ""$cmd"" > /dev/null 2>&1
    else
        which ""$cmd"" > /dev/null 2>&1
    fi
    local rt=$?
    return ${rt}
}

_os() {
    local os=""""
    [ -f ""/etc/debian_version"" ] && source /etc/os-release && os=""${ID}"" && printf -- ""%s"" ""${os}"" && return
    [ -f ""/etc/redhat-release"" ] && os=""centos"" && printf -- ""%s"" ""${os}"" && return
}

_os_full() {
    [ -f /etc/redhat-release ] && awk '{print ($1,$3~/^[0-9]/?$3:$4)}' /etc/redhat-release && return
    [ -f /etc/os-release ] && awk -F'[= ""]' '/PRETTY_NAME/{print $3,$4,$5}' /etc/os-release && return
    [ -f /etc/lsb-release ] && awk -F'[=""]+' '/DESCRIPTION/{print $2}' /etc/lsb-release && return
}

_os_ver() {
    local main_ver=""$( echo $(_os_full) | grep -oE  ""[0-9.]+"")""
    printf -- ""%s"" ""${main_ver%%.*}""
}

_error_detect() {
    local cmd=""$1""
    _info ""${cmd}""
    eval ${cmd}
    if [ $? -ne 0 ]; then
        _error ""Execution command (${cmd}) failed, please check it and try again.""
    fi
}

_is_digit(){
    local input=${1}
    if [[ ""$input"" =~ ^[0-9]+$ ]]; then
        return 0
    else
        return 1
    fi
}

_is_64bit(){
    if [ $(getconf WORD_BIT) = '32' ] && [ $(getconf LONG_BIT) = '64' ]; then
        return 0
    else
        return 1
    fi
}

_version_ge(){
    test ""$(echo ""$@"" | tr "" "" ""\n"" | sort -rV | head -n 1)"" == ""$1""
}

get_valid_valname(){
    local val=${1}
    local new_val=$(eval echo $val | sed 's/[-.]/_/g')
    echo ${new_val}
}

get_hint(){
    local val=${1}
    local new_val=$(get_valid_valname $val)
    eval echo ""\$hint_${new_val}""
}

#Display Memu
display_menu(){
    local soft=${1}
    local default=${2}
    eval local arr=(\${${soft}_arr[@]})
    local default_prompt
    if [[ ""$default"" != """" ]]; then
        if [[ ""$default"" == ""last"" ]]; then
            default=${#arr[@]}
        fi
        default_prompt=""(default ${arr[$default-1]})""
    fi
    local pick
    local hint
    local vname
    local prompt=""which ${soft} you'd select ${default_prompt}: ""

    while :
    do
        echo -e ""\n------------ ${soft} setting ------------\n""
        for ((i=1;i<=${#arr[@]};i++ )); do
            vname=""$(get_valid_valname ${arr[$i-1]})""
            hint=""$(get_hint $vname)""
            [[ ""$hint"" == """" ]] && hint=""${arr[$i-1]}""
            echo -e ""${green}${i}${plain}) $hint""
        done
        echo

        if ! _is_digit ""$pick""; then
            prompt=""Input error, please input a number""
            continue
        fi

        if [[ ""$pick"" -lt 1 || ""$pick"" -gt ${#arr[@]} ]]; then
            prompt=""Input error, please input a number between 1 and ${#arr[@]}: ""
            continue
        fi

        break
    done

    eval ${soft}=${arr[$pick-1]}
    vname=""$(get_valid_valname ${arr[$pick-1]})""
    hint=""$(get_hint $vname)""
    [[ ""$hint"" == """" ]] && hint=""${arr[$pick-1]}""
    echo -e ""\nyour selection: $hint\n""
}

get_latest_version() {
    latest_version=($(wget -qO- https://kernel.ubuntu.com/~kernel-ppa/mainline/ | awk -F'\""v' '/v[4-9]./{print $2}' | cut -d/ -f1 | grep -v - | sort -V))
    [ ${#latest_version[@]} -eq 0 ] && _error ""Get latest kernel version failed.""
    kernel_arr=()
    for i in ${latest_version[@]}; do
        if _version_ge $i 5.15; then
            kernel_arr+=($i);
        fi
    done
    display_menu kernel last
    if _is_64bit; then
        deb_name=$(wget -qO- https://kernel.ubuntu.com/~kernel-ppa/mainline/v${kernel}/ | grep ""linux-image"" | grep ""generic"" | awk -F'\"">' '/amd64.deb/{print $2}' | cut -d'<' -f1 | head -1)
        deb_kernel_url=""https://kernel.ubuntu.com/~kernel-ppa/mainline/v${kernel}/${deb_name}""
        deb_kernel_name=""linux-image-${kernel}-amd64.deb""
        modules_deb_name=$(wget -qO- https://kernel.ubuntu.com/~kernel-ppa/mainline/v${kernel}/ | grep ""linux-modules"" | grep ""generic"" | awk -F'\"">' '/amd64.deb/{print $2}' | cut -d'<' -f1 | head -1)
        deb_kernel_modules_url=""https://kernel.ubuntu.com/~kernel-ppa/mainline/v${kernel}/${modules_deb_name}""
        deb_kernel_modules_name=""linux-modules-${kernel}-amd64.deb""
    else
        deb_name=$(wget -qO- https://kernel.ubuntu.com/~kernel-ppa/mainline/v${kernel}/ | grep ""linux-image"" | grep ""generic"" | awk -F'\"">' '/i386.deb/{print $2}' | cut -d'<' -f1 | head -1)
        deb_kernel_url=""https://kernel.ubuntu.com/~kernel-ppa/mainline/v${kernel}/${deb_name}""
        deb_kernel_name=""linux-image-${kernel}-i386.deb""
        modules_deb_name=$(wget -qO- https://kernel.ubuntu.com/~kernel-ppa/mainline/v${kernel}/ | grep ""linux-modules"" | grep ""generic"" | awk -F'\"">' '/i386.deb/{print $2}' | cut -d'<' -f1 | head -1)
        deb_kernel_modules_url=""https://kernel.ubuntu.com/~kernel-ppa/mainline/v${kernel}/${modules_deb_name}""
        deb_kernel_modules_name=""linux-modules-${kernel}-i386.deb""
    fi
    [ -z ""${deb_name}"" ] && _error ""Getting Linux kernel binary package name failed, maybe kernel build failed. Please choose other one and try again.""
}

get_char() {
    SAVEDSTTY=`stty -g`
    stty -echo
    stty cbreak
    dd if=/dev/tty bs=1 count=1 2> /dev/null
    stty -raw
    stty echo
    stty $SAVEDSTTY
}

check_bbr_status() {
    local param=$(sysctl net.ipv4.tcp_congestion_control | awk '{print $3}')
    if [[ x""${param}"" == x""bbr"" ]]; then
        return 0
    else
        return 1
    fi
}

check_kernel_version() {
    local kernel_version=$(uname -r | cut -d- -f1)
    if _version_ge ${kernel_version} 4.9; then
        return 0
    else
        return 1
    fi
}

# Check OS version
check_os() {
    if _exists ""virt-what""; then
        virt=""$(virt-what)""
    elif _exists ""systemd-detect-virt""; then
        virt=""$(systemd-detect-virt)""
    fi
    if [ -n ""${virt}"" -a ""${virt}"" = ""lxc"" ]; then
        _error ""Virtualization method is LXC, which is not supported.""
    fi
    if [ -n ""${virt}"" -a ""${virt}"" = ""openvz"" ] || [ -d ""/proc/vz"" ]; then
        _error ""Virtualization method is OpenVZ, which is not supported.""
    fi
    [ -z ""$(_os)"" ] && _error ""Not supported OS""
    case ""$(_os)"" in
        ubuntu)
            [ -n ""$(_os_ver)"" -a ""$(_os_ver)"" -lt 16 ] && _error ""Not supported OS, please change to Ubuntu 16+ and try again.""
            ;;
        debian)
            [ -n ""$(_os_ver)"" -a ""$(_os_ver)"" -lt 8 ] &&  _error ""Not supported OS, please change to Debian 8+ and try again.""
            ;;
        centos)
            [ -n ""$(_os_ver)"" -a ""$(_os_ver)"" -lt 6 ] &&  _error ""Not supported OS, please change to CentOS 6+ and try again.""
            ;;
        *)
            _error ""Not supported OS""
            ;;
    esac
}

sysctl_config() {
    sed -i '/net.core.default_qdisc/d' /etc/sysctl.conf
    sed -i '/net.ipv4.tcp_congestion_control/d' /etc/sysctl.conf
    echo ""net.core.default_qdisc = fq"" >> /etc/sysctl.conf
    echo ""net.ipv4.tcp_congestion_control = bbr"" >> /etc/sysctl.conf
    sysctl -p >/dev/null 2>&1
}

install_kernel() {
    case ""$(_os)"" in
        centos)
            if [ -n ""$(_os_ver)"" ]; then
                if ! _exists ""perl""; then
                    _error_detect ""yum install -y perl""
                fi
                if [ ""$(_os_ver)"" -eq 6 ]; then
                    _error_detect ""rpm --import https://www.elrepo.org/RPM-GPG-KEY-elrepo.org""
                    rpm_kernel_url=""https://dl.lamp.sh/files/""
                    if _is_64bit; then
                        rpm_kernel_name=""kernel-ml-4.18.20-1.el6.elrepo.x86_64.rpm""
                        rpm_kernel_devel_name=""kernel-ml-devel-4.18.20-1.el6.elrepo.x86_64.rpm""
                    else
                        rpm_kernel_name=""kernel-ml-4.18.20-1.el6.elrepo.i686.rpm""
                        rpm_kernel_devel_name=""kernel-ml-devel-4.18.20-1.el6.elrepo.i686.rpm""
                    fi
                    _error_detect ""wget -c -t3 -T60 -O ${rpm_kernel_name} ${rpm_kernel_url}${rpm_kernel_name}""
                    _error_detect ""wget -c -t3 -T60 -O ${rpm_kernel_devel_name} ${rpm_kernel_url}${rpm_kernel_devel_name}""
                    [ -s ""${rpm_kernel_name}"" ] && _error_detect ""rpm -ivh ${rpm_kernel_name}"" || _error ""Download ${rpm_kernel_name} failed, please check it.""
                    [ -s ""${rpm_kernel_devel_name}"" ] && _error_detect ""rpm -ivh ${rpm_kernel_devel_name}"" || _error ""Download ${rpm_kernel_devel_name} failed, please check it.""
                    rm -f ${rpm_kernel_name} ${rpm_kernel_devel_name}
                    [ ! -f ""/boot/grub/grub.conf"" ] && _error ""/boot/grub/grub.conf not found, please check it.""
                    sed -i 's/^default=.*/default=0/g' /boot/grub/grub.conf
                elif [ ""$(_os_ver)"" -eq 7 ]; then
                    rpm_kernel_url=""https://dl.lamp.sh/kernel/el7/""
                    if _is_64bit; then
                        rpm_kernel_name=""kernel-ml-5.15.60-1.el7.x86_64.rpm""
                        rpm_kernel_devel_name=""kernel-ml-devel-5.15.60-1.el7.x86_64.rpm""
                    else
                        _error ""Not supported architecture, please change to 64-bit architecture.""
                    fi
                    _error_detect ""wget -c -t3 -T60 -O ${rpm_kernel_name} ${rpm_kernel_url}${rpm_kernel_name}""
                    _error_detect ""wget -c -t3 -T60 -O ${rpm_kernel_devel_name} ${rpm_kernel_url}${rpm_kernel_devel_name}""
                    [ -s ""${rpm_kernel_name}"" ] && _error_detect ""rpm -ivh ${rpm_kernel_name}"" || _error ""Download ${rpm_kernel_name} failed, please check it.""
                    [ -s ""${rpm_kernel_devel_name}"" ] && _error_detect ""rpm -ivh ${rpm_kernel_devel_name}"" || _error ""Download ${rpm_kernel_devel_name} failed, please check it.""
                    rm -f ${rpm_kernel_name} ${rpm_kernel_devel_name}
                    /usr/sbin/grub2-set-default 0
                fi
            fi
            ;;
        ubuntu|debian)
            _info ""Getting latest kernel version...""
            get_latest_version
            if [ -n ""${modules_deb_name}"" ]; then
                _error_detect ""wget -c -t3 -T60 -O ${deb_kernel_modules_name} ${deb_kernel_modules_url}""
            fi
            _error_detect ""wget -c -t3 -T60 -O ${deb_kernel_name} ${deb_kernel_url}""
            _error_detect ""dpkg -i ${deb_kernel_modules_name} ${deb_kernel_name}""
            rm -f ${deb_kernel_modules_name} ${deb_kernel_name}
            _error_detect ""/usr/sbin/update-grub""
            ;;
        *)
            ;; # do nothing
    esac
}

reboot_os() {
    echo
    _info ""The system needs to reboot.""
}

install_bbr() {
    if check_bbr_status; then
        echo
        _info ""TCP BBR has already been enabled. nothing to do...""
        exit 0
    fi
    if check_kernel_version; then
        echo
        _info ""The kernel version is greater than 4.9, directly setting TCP BBR...""
        sysctl_config
        _info ""Setting TCP BBR completed...""
        exit 0
    fi
    check_os
    install_kernel
    sysctl_config
    reboot_os
}

[[ $EUID -ne 0 ]] && _error ""This script must be run as root""
opsy=$( _os_full )
arch=$( uname -m )
lbit=$( getconf LONG_BIT )
kern=$( uname -r )

clear
echo ""---------- System Information ----------""
echo "" OS      : $opsy""
echo "" Arch    : $arch ($lbit Bit)""
echo "" Kernel  : $kern""
echo ""----------------------------------------""
echo "" Automatically enable TCP BBR script""
echo
echo "" URL: https://teddysun.com/489.html""
echo ""----------------------------------------""
echo

install_bbr 2>&1 | tee ${cur_dir}/install_bbr.log
");
    }

    private static void GenerateInstallXuiFile(string path)
    {
        File.WriteAllText(path, @"#!/bin/bash
red='\033[0;31m'
green='\033[0;32m'
yellow='\033[0;33m'
plain='\033[0m'
cur_dir=$(pwd)
# check root
[[ $EUID -ne 0 ]] && echo -e ""${red}Fatal error：${plain} Please run this script with root privilege \n "" && exit 1

# Check OS and set release variable
if [[ -f /etc/os-release ]]; then
    source /etc/os-release
    release=$ID
elif [[ -f /usr/lib/os-release ]]; then
    source /usr/lib/os-release
    release=$ID
else
    echo ""Failed to check the system OS, please contact the author!"" >&2
    exit 1
fi
echo ""The OS release is: $release""
arch=$(arch)
if [[ $arch == ""x86_64"" || $arch == ""x64"" || $arch == ""amd64"" ]]; then
    arch=""amd64""
elif [[ $arch == ""aarch64"" || $arch == ""arm64"" ]]; then
    arch=""arm64""
elif [[ $arch == ""s390x"" ]]; then
    arch=""s390x""
else
    arch=""amd64""
    echo -e ""${red} Failed to check system arch, will use default arch: ${arch}${plain}""
fi

echo ""arch: ${arch}""

if [ $(getconf WORD_BIT) != '32' ] && [ $(getconf LONG_BIT) != '64' ]; then
    echo ""x-ui dosen't support 32-bit(x86) system, please use 64 bit operating system(x86_64) instead, if there is something wrong, please get in touch with me!""
    exit -1
fi

os_version=""""
os_version=$(grep -i version_id /etc/os-release | cut -d \"" -f2 | cut -d . -f1)

if [[ ""${release}"" == ""centos"" ]]; then
    if [[ ${os_version} -lt 7 ]]; then
        echo -e ""${red} Please use CentOS 7 or higher ${plain}\n"" && exit 1
    fi
elif [[ ""${release}"" ==  ""ubuntu"" ]]; then
    if [[ ${os_version} -lt 18 ]]; then
        echo -e ""${red}please use Ubuntu 18 or higher version！${plain}\n"" && exit 1
    fi

elif [[ ""${release}"" == ""fedora"" ]]; then
    if [[ ${os_version} -lt 36 ]]; then
        echo -e ""${red}please use Fedora 36 or higher version！${plain}\n"" && exit 1
    fi

elif [[ ""${release}"" == ""debian"" ]]; then
    if [[ ${os_version} -lt 8 ]]; then
        echo -e ""${red} Please use Debian 8 or higher ${plain}\n"" && exit 1
    fi
else
    echo -e ""${red}Failed to check the OS version, please contact the author!${plain}"" && exit 1
fi


install_base() {
    if [[ ""${release}"" == ""centos"" ]]; then
        yum install wget curl tar -y
    else
        apt install wget curl tar -y
    fi
}
#This function will be called when user installed x-ui out of sercurity
config_after_install() {
    echo -e ""${yellow}Install/update finished! For security it's recommended to modify panel settings ${plain}""
    if [[ x""${config_confirm}"" == x""y"" || x""${config_confirm}"" == x""Y"" ]]; then
        echo -e ""${yellow}Your username will be:${config_account}${plain}""
        echo -e ""${yellow}Your password will be:${config_password}${plain}""
        echo -e ""${yellow}Your panel port is:${config_port}${plain}""
        echo -e ""${yellow}Initializing, please wait...${plain}""
        /usr/local/x-ui/x-ui setting -username ${config_account} -password ${config_password}
        echo -e ""${yellow}Account name and password set successfully!${plain}""
        /usr/local/x-ui/x-ui setting -port ${config_port}
        echo -e ""${yellow}Panel port set successfully!${plain}""
    else
        echo -e ""${red}Canceled, will use the default settings.${plain}""
    fi
}

install_x-ui() {
    systemctl stop x-ui
    cd /usr/local/

    if [ $# == 0 ]; then
        last_version=$(curl -Ls ""https://api.github.com/repos/alireza0/x-ui/releases/latest"" | grep '""tag_name"":' | sed -E 's/.*""([^""]+)"".*/\1/')
        if [[ ! -n ""$last_version"" ]]; then
            echo -e ""${red}Failed to fetch x-ui version, it maybe due to Github API restrictions, please try it later${plain}""
            exit 1
        fi
        echo -e ""Got x-ui latest version: ${last_version}, beginning the installation...""
        wget -N --no-check-certificate -O /usr/local/x-ui-linux-${arch}.tar.gz https://github.com/alireza0/x-ui/releases/download/${last_version}/x-ui-linux-${arch}.tar.gz
        if [[ $? -ne 0 ]]; then
            echo -e ""${red}Dowanloading x-ui failed, please be sure that your server can access Github ${plain}""
            exit 1
        fi
    else
        last_version=$1
        url=""https://github.com/alireza0/x-ui/releases/download/${last_version}/x-ui-linux-${arch}.tar.gz""
        echo -e ""Begining to install x-ui v$1""
        wget -N --no-check-certificate -O /usr/local/x-ui-linux-${arch}.tar.gz ${url}
        if [[ $? -ne 0 ]]; then
            echo -e ""${red}dowanload x-ui v$1 failed,please check the verison exists${plain}""
            exit 1
        fi
    fi

    if [[ -e /usr/local/x-ui/ ]]; then
        rm /usr/local/x-ui/ -rf
    fi

    tar zxvf x-ui-linux-${arch}.tar.gz
    rm x-ui-linux-${arch}.tar.gz -f
    cd x-ui
    chmod +x x-ui bin/xray-linux-${arch}
    cp -f x-ui.service /etc/systemd/system/
    wget --no-check-certificate -O /usr/bin/x-ui https://raw.githubusercontent.com/alireza0/x-ui/main/x-ui.sh
    chmod +x /usr/local/x-ui/x-ui.sh
    chmod +x /usr/bin/x-ui
    config_after_install
    #echo -e ""If it is a new installation, the default web port is ${green}54321${plain}, The username and password are ${green}admin${plain} by default""
    #echo -e ""Please make sure that this port is not occupied by other procedures,${yellow} And make sure that port 54321 has been released${plain}""
    #    echo -e ""If you want to modify the 54321 to other ports and enter the x-ui command to modify it, you must also ensure that the port you modify is also released""
    #echo -e """"
    #echo -e ""If it is updated panel, access the panel in your previous way""
    #echo -e """"
    systemctl daemon-reload
    systemctl enable x-ui
    systemctl start x-ui
    echo -e ""${green}x-ui v${last_version}${plain} installation finished, it is up and running now...""
    echo -e """"
    echo -e ""x-ui control menu usages: ""
    echo -e ""----------------------------------------------""
    echo -e ""x-ui              - Enter     Admin menu""
    echo -e ""x-ui start        - Start     x-ui""
    echo -e ""x-ui stop         - Stop      x-ui""
    echo -e ""x-ui restart      - Restart   x-ui""
    echo -e ""x-ui status       - Show      x-ui status""
    echo -e ""x-ui enable       - Enable    x-ui on system startup""
    echo -e ""x-ui disable      - Disable   x-ui on system startup""
    echo -e ""x-ui log          - Check     x-ui logs""
    echo -e ""x-ui v2-ui        - Migrate   v2-ui Account data to x-ui""
    echo -e ""x-ui update       - Update    x-ui""
    echo -e ""x-ui install      - Install   x-ui""
    echo -e ""x-ui uninstall    - Uninstall x-ui""
    echo -e ""----------------------------------------------""
}

echo -e ""${green}Excuting...${plain}""
install_base
install_x-ui $1");
    }

    private static void GenerateUpdateFile(string path)
    {
        File.WriteAllText(path, @"#!/bin/bash
export DEBIAN_FRONTEND=noninteractive

apt-get update -y 
");
    }

    private static void GenerateNginxConfFile(string path)
    {
        File.WriteAllText(path, @"user www-data;
worker_processes auto;
pid /run/nginx.pid;
include /etc/nginx/modules-enabled/*.conf;

events {
  worker_connections 2048;
  multi_accept on;
}

http {

 server {
    listen 80;


    location / {
  proxy_pass https://bing.com; 
  proxy_ssl_server_name on;
  proxy_redirect off;
  sub_filter_once off;
  proxy_set_header Host ""bing.com"";
  proxy_set_header Referer $http_referer;
  proxy_set_header X-Real-IP $remote_addr;
  proxy_set_header User-Agent $http_user_agent;
  proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
  proxy_set_header X-Forwarded-Proto https;
  proxy_set_header Accept-Encoding """";
    }

   location /test {
    proxy_redirect off;
    proxy_pass http://127.0.0.1:1000;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection ""upgrade"";
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

location /wspath {
        if ($http_upgrade != ""websocket"") {
            return 404;
        }
        location ~ /wspath/\d+$ {
            if ($request_uri ~* ""([^/]*$)"" ) {
                set $port $1;
            }
            proxy_redirect off;
            proxy_pass http://127.0.0.1:$port/;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection ""upgrade"";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        }
        return 404;
    }
    
    location ~ /grpcpath/(?<port>\d+) { 
        if ($content_type !~ ""application/grpc"") {
            return 404;
        }
        client_max_body_size 0;
        client_body_timeout 60m;
        send_timeout 60m;
        lingering_close always;

        grpc_read_timeout 3m;
        grpc_send_timeout 2m;
        grpc_set_header Host $host;
        grpc_set_header X-Real-IP $remote_addr;
        grpc_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        grpc_pass grpc://127.0.0.1:$port;
        
        return 404;

    }

}
}");
    }


}