using System;
using System.ComponentModel;
using System.Configuration.Install;

namespace TopCrawler
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            BeforeInstall += ProjectInstaller_BeforeInstall;
            BeforeUninstall += ProjectInstaller_BeforeInstall;
        }

        private void ProjectInstaller_BeforeInstall(object sender, InstallEventArgs e)
        {
            // Configure ServiceName 
            if (!String.IsNullOrEmpty(Context.Parameters["ServiceName"]))
                _serviceInstaller.ServiceName = Context.Parameters["ServiceName"];

            // Configure DisplayName 
            if (!String.IsNullOrEmpty(Context.Parameters["DisplayName"]))
                _serviceInstaller.DisplayName = Context.Parameters["DisplayName"];

            // Configure Description
            if (!String.IsNullOrEmpty(Context.Parameters["Description"]))
                _serviceInstaller.Description = Context.Parameters["Description"];
        }
    }
}
