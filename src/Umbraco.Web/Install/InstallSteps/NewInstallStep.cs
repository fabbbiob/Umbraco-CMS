using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Security;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Web.Install.Models;

namespace Umbraco.Web.Install.InstallSteps
{
    /// <summary>
    /// This is the first UI step for a brand new install
    /// </summary>
    /// <remarks>
    /// By default this will show the user view which is the most basic information to configure a new install, but if an install get's interupted because of an 
    /// error, etc... and the end-user refreshes the installer then we cannot show the user screen because they've already entered that information so instead we'll    
    /// display a simple continue installation view.
    /// </remarks>
    [InstallSetupStep(InstallationType.NewInstall,
        "User", 20, "")]
    internal class NewInstallStep : InstallSetupStep<UserModel>
    {
        private readonly ApplicationContext _applicationContext;

        public NewInstallStep(ApplicationContext applicationContext)
        {
            _applicationContext = applicationContext;
        }

        private MembershipProvider CurrentProvider
        {
            get
            {
                var provider = Membership.Providers[UmbracoConfig.For.UmbracoSettings().Providers.DefaultBackOfficeUserProvider];
                if (provider == null)
                {
                    throw new InvalidOperationException("No MembershipProvider found with name " + UmbracoConfig.For.UmbracoSettings().Providers.DefaultBackOfficeUserProvider);
                }
                return provider;
            }
        }

        public override InstallSetupResult Execute(UserModel user)
        {
            var admin = _applicationContext.Services.UserService.GetUserById(0);
            if (admin == null)
            {
                throw new InvalidOperationException("Could not find the admi user!");
            }

            var membershipUser = CurrentProvider.GetUser(0, true);
            if (membershipUser == null)
            {
                throw new InvalidOperationException("No user found in membership provider with id of 0");
            }

            try
            {
                var success = membershipUser.ChangePassword("default", user.Password.Trim());
                if (success == false)
                {
                    throw new FormatException("Password must be at least " + CurrentProvider.MinRequiredPasswordLength + " characters long and contain at least " + CurrentProvider.MinRequiredNonAlphanumericCharacters + " symbols");
                }
            }
            catch (Exception ex)
            {
                throw new FormatException("Password must be at least " + CurrentProvider.MinRequiredPasswordLength + " characters long and contain at least " + CurrentProvider.MinRequiredNonAlphanumericCharacters + " symbols");
            }

            admin.Email = user.Email.Trim();
            admin.Name = user.Name.Trim();
            admin.Username = user.Email.Trim();

            _applicationContext.Services.UserService.Save(admin);
            
            return null;
        }

        /// <summary>
        /// Return a custom view model for this step
        /// </summary>
        public override object ViewModel
        {
            get
            {
                return new
                {
                    minCharLength = Membership.Providers[Constants.Conventions.User.UmbracoUsersProviderName].MinRequiredPasswordLength,
                    minNonAlphaNumericLength = Membership.Providers[Constants.Conventions.User.UmbracoUsersProviderName].MinRequiredNonAlphanumericCharacters
                };
            }
        }

        public override string View
        {
            get { return RequiresExecution(null)
                //the user UI
                ? "user" 
                //the continue install UI
                : "continueinstall"; }
        }

        public override bool RequiresExecution(UserModel model)
        {
            //if there's already a version then there should def be a user
            if (GlobalSettings.ConfigurationStatus.IsNullOrWhiteSpace() == false) return false;

            //now we have to check if this is really a new install, the db might be configured and might contain data
            var databaseSettings = ConfigurationManager.ConnectionStrings[GlobalSettings.UmbracoConnectionName];

            if (_applicationContext.DatabaseContext.IsConnectionStringConfigured(databaseSettings)
                && _applicationContext.DatabaseContext.IsDatabaseConfigured)
            {
                //check if we have the default user configured already
                var result = _applicationContext.DatabaseContext.Database.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM umbracoUser WHERE id=0 AND userPassword='default'");
                if (result == 1)
                {
                    //the user has not been configured
                    return true;
                }
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}