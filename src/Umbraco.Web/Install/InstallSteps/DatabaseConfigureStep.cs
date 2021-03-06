﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Persistence;
using Umbraco.Web.Install.Models;

namespace Umbraco.Web.Install.InstallSteps
{
    [InstallSetupStep(InstallationType.NewInstall,
        "DatabaseConfigure", "database", 10, "Setting up a database, so Umbraco has a place to store your website",
        PerformsAppRestart = true)]
    internal class DatabaseConfigureStep : InstallSetupStep<DatabaseModel>
    {
        private readonly ApplicationContext _applicationContext;

        public DatabaseConfigureStep(ApplicationContext applicationContext)
        {
            _applicationContext = applicationContext;
        }

        public override InstallSetupResult Execute(DatabaseModel database)
        {
            //if the database model is null then we will apply the defaults
            if (database == null)
            {
                database = new DatabaseModel();
            }

            if (CheckConnection(database) == false)
            {
                throw new InvalidOperationException("Could not connect to the database");
            }
            ConfigureConnection(database);
            return null;
        }

        private bool CheckConnection(DatabaseModel database)
        {
            string connectionString;
            var dbContext = _applicationContext.DatabaseContext;
            if (database.ConnectionString.IsNullOrWhiteSpace() == false)
            {
                connectionString = database.ConnectionString;
            }
            else if (database.DatabaseType == DatabaseType.SqlCe)
            {
                //we do not test this connection
                return true;
                //connectionString = dbContext.GetEmbeddedDatabaseConnectionString();
            }
            else if (database.IntegratedAuth)
            {
                connectionString = dbContext.GetIntegratedSecurityDatabaseConnectionString(
                    database.Server, database.DatabaseName);
            }
            else
            {
                string providerName;
                connectionString = dbContext.GetDatabaseConnectionString(
                    database.Server, database.DatabaseName, database.Login, database.Password,
                    database.DatabaseType.ToString(),
                    out providerName);
            }

            return SqlExtensions.IsConnectionAvailable(connectionString);
        }

        private void ConfigureConnection(DatabaseModel database)
        {
            var dbContext = _applicationContext.DatabaseContext;
            if (database.ConnectionString.IsNullOrWhiteSpace() == false)
            {
                dbContext.ConfigureDatabaseConnection(database.ConnectionString);
            }
            else if (database.DatabaseType == DatabaseType.SqlCe)
            {
                dbContext.ConfigureEmbeddedDatabaseConnection();
            }
            else if (database.IntegratedAuth)
            {
                dbContext.ConfigureIntegratedSecurityDatabaseConnection(
                    database.Server, database.DatabaseName);
            }
            else
            {
                dbContext.ConfigureDatabaseConnection(
                    database.Server, database.DatabaseName, database.Login, database.Password,
                    database.DatabaseType.ToString());
            }
        }

        public override string View
        {
            get { return ShouldDisplayView() ? base.View : ""; }
        }

        public override bool RequiresExecution(DatabaseModel model)
        {
            return ShouldDisplayView();
        }

        private bool ShouldDisplayView()
        {
            //If the connection string is already present in web.config we don't need to show the settings page and we jump to installing/upgrading.
            var databaseSettings = ConfigurationManager.ConnectionStrings[GlobalSettings.UmbracoConnectionName];

            if (_applicationContext.DatabaseContext.IsConnectionStringConfigured(databaseSettings))
            {
                try
                {
                    //Since a connection string was present we verify the db can connect and query
                    var result = _applicationContext.DatabaseContext.ValidateDatabaseSchema();
                    result.DetermineInstalledVersion();
                    return false;
                }
                catch (Exception)
                {
                    //something went wrong, could not connect so probably need to reconfigure
                    return true;
                }
            }

            return true;
        }
    }
}
