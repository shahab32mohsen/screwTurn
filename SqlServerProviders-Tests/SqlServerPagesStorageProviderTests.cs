
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.Tests;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Plugins.SqlCommon;

namespace ScrewTurn.Wiki.Plugins.SqlServer.Tests {

	[TestFixture]
	public class SqlServerPagesStorageProviderTests : PagesStorageProviderTestScaffolding {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private const string ConnString = "Data Source=(local)\\SQLExpress;Integrated Security=SSPI;";
		private const string InitialCatalog = "Initial Catalog=ScrewTurnWikiTest;";

		public override IPagesStorageProviderV40 GetProvider() {
			SqlServerPagesStorageProvider prov = new SqlServerPagesStorageProvider();
			prov.SetUp(MockHost(), ConnString + InitialCatalog);
			prov.Init(MockHost(), ConnString + InitialCatalog, null);

			return prov;
		}

		[TestFixtureSetUp]
		public void FixtureSetUp() {
			// Create database with no tables
			SqlConnection cn = new SqlConnection(ConnString);
			cn.Open();

			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = "if (select count(*) from sys.databases where [Name] = 'ScrewTurnWikiTest') = 0 begin create database [ScrewTurnWikiTest] end";
			cmd.ExecuteNonQuery();

			cn.Close();
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			// Clear all tables
			SqlConnection cn = new SqlConnection(ConnString);
			cn.Open();

			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = "use [ScrewTurnWikiTest]; delete from [ContentTemplate]; delete from [Snippet]; delete from [NavigationPath]; delete from [Message]; delete from [PageKeyword]; delete from [CategoryBinding]; delete from [PageContent]; delete from [Category]; delete from [Namespace] where [Name] <> '';";
			try {
				cmd.ExecuteNonQuery();
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}

			cn.Close();
		}

		[TestFixtureTearDown]
		public void FixtureTearDown() {
			// Delete database
			SqlConnection cn = new SqlConnection(ConnString);
			cn.Open();

			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = "alter database [ScrewTurnWikiTest] set single_user with rollback immediate";
			try {
				cmd.ExecuteNonQuery();
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}

			cmd = cn.CreateCommand();
			cmd.CommandText = "drop database [ScrewTurnWikiTest]";
			try {
				cmd.ExecuteNonQuery();
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}

			cn.Close();

			// This is neede because the pooled connection are using a session
			// that is now invalid due to the commands executed above
			SqlConnection.ClearAllPools();
		}

		[Test]
		public void Init() {
			IPagesStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), ConnString + InitialCatalog, null);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[Test]
		public void Init_Upgrade()
		{
			FixtureTearDown();
			FixtureSetUp();

			SqlConnection cn = new SqlConnection(ConnString + InitialCatalog);
			cn.Open();
			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = Properties.Resources.PagesDatabase;

			try
			{
				cmd.ExecuteNonQuery();
			}
			catch (SqlException sqlex)
			{
				Console.WriteLine(sqlex);
				throw new Exception("Could not generate v3 Pages test database", sqlex);
			}
			finally
			{
				cn.Close();
			}

			SqlServerPagesStorageProvider prov = new SqlServerPagesStorageProvider();
			prov.SetUp(MockHost(), ConnString + InitialCatalog);
			prov.Init(MockHost(), ConnString + InitialCatalog, "-");

			// Check if v3 tables were renamed
			cn = new SqlConnection(ConnString + InitialCatalog);
			cn.Open();
			cmd = cn.CreateCommand();
			cmd.CommandText = "select count(*) from INFORMATION_SCHEMA.TABLES where TABLE_NAME LIKE '%_v3'";
			int count = (int)cmd.ExecuteScalar();
			cn.Close();

			Assert.IsTrue(count > 0);
		}

		[TestCase("", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("blah", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("Data Source=(local)\\SQLExpress;User ID=inexistent;Password=password;InitialCatalog=Inexistent;", ExpectedException = typeof(InvalidConfigurationException))]
		public void SetUp_InvalidConnString(string c) {
			SqlServerPagesStorageProvider prov = new SqlServerPagesStorageProvider();
			prov.SetUp(MockHost(), c);
		}
		
	}

}
