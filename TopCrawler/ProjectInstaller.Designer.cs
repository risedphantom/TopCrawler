using System.ServiceProcess;
using System.ComponentModel;
using System.Configuration.Install;

namespace TopCrawler
{
    partial class ProjectInstaller
    {
        private ServiceProcessInstaller _serviceProcessInstaller;
        private ServiceInstaller _serviceInstaller;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _serviceProcessInstaller = new ServiceProcessInstaller();
            _serviceInstaller = new ServiceInstaller();
            // 
            // _serviceProcessInstaller
            // 
            _serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            _serviceProcessInstaller.Password = null;
            _serviceProcessInstaller.Username = null;
            // 
            // _serviceInstaller
            // 
            _serviceInstaller.DelayedAutoStart = true;
            _serviceInstaller.Description = "Эта служба собирает почту с различный ящиков";
            _serviceInstaller.DisplayName = "Служба сбора почты";
            _serviceInstaller.ServiceName = "TopCrawler";
            _serviceInstaller.StartType = ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            Installers.AddRange(new Installer[] {
            _serviceProcessInstaller,
            _serviceInstaller});
        }

        #endregion
    }
}