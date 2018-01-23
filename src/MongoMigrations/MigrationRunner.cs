namespace MongoMigrations
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using MongoDB.Bson.Serialization;
	using MongoDB.Driver;

	public class MigrationRunner
	{
		static MigrationRunner()
		{
			Init();
		}

		public static void Init()
		{
			BsonSerializer.RegisterSerializer(typeof (MigrationVersion), new MigrationVersionSerializer());
		}

		public MigrationRunner(string mongoServerLocation, string databaseName)
			: this(new MongoClient(mongoServerLocation).GetDatabase(databaseName))
		{
		}

		public MigrationRunner(IMongoDatabase database)
		{
			Database = database;
			DatabaseStatus = new DatabaseMigrationStatus(this);
			MigrationLocator = new MigrationLocator();
		}

		public IMongoDatabase Database { get; set; }
		public MigrationLocator MigrationLocator { get; set; }
		public DatabaseMigrationStatus DatabaseStatus { get; set; }

		public virtual void UpdateToLatest()
		{
			Console.WriteLine(WhatWeAreUpdating() + " to latest...");
			UpdateTo(MigrationLocator.LatestVersion());
		}

		private string WhatWeAreUpdating()
		{
			return string.Format("Updating server(s) \"{0}\" for database \"{1}\"", ServerAddresses(), Database.DatabaseNamespace.DatabaseName);
		}

	    private string ServerAddresses()
	    {
	        return "Server address"; //String.Join(",", Database.Server.Instances.Select(s => s.Address.ToString()));
	    }

	    protected virtual void ApplyMigrations(IEnumerable<Migration> migrations)
		{
			migrations.ToList()
			          .ForEach(ApplyMigration);
		}

		protected virtual void ApplyMigration(Migration migration)
		{
			Console.WriteLine(new {Message = "Applying migration", migration.Version, migration.Description, Database.DatabaseNamespace.DatabaseName});

			var appliedMigration = DatabaseStatus.StartMigration(migration);
			migration.Database = Database;
			try
			{
				migration.Update();
			}
			catch (Exception exception)
			{
				OnMigrationException(migration, exception);
			}
			DatabaseStatus.CompleteMigration(appliedMigration);
		}

		protected virtual void OnMigrationException(Migration migration, Exception exception)
		{
			var message = new
				{
					Message = "Migration failed to be applied: " + exception.Message,
					migration.Version,
					Name = migration.GetType(),
					migration.Description,
					Database.DatabaseNamespace.DatabaseName
            };
			Console.WriteLine(message);
			throw new MigrationException(message.ToString(), exception);
		}

		public virtual void UpdateTo(MigrationVersion updateToVersion)
		{
			var currentVersion = DatabaseStatus.GetLastAppliedMigration();
			Console.WriteLine(new {Message = WhatWeAreUpdating(), currentVersion, updateToVersion, Database.DatabaseNamespace.DatabaseName});

			var migrations = MigrationLocator.GetMigrationsAfter(currentVersion)
			                                 .Where(m => m.Version <= updateToVersion);

			ApplyMigrations(migrations);
		}
	}
}