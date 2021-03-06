﻿using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public class SDCDatabase : IDatabase
	{
		public ConnectionType ConnectionType { get; set; } = ConnectionType.Local;

		string _name;
		public string Name
		{
			get {
				if (_name == null)
					return $"{_db.DataSource}/{_db.Database}";
				return _name;
			}
			set {
				_name = value;
			}
		}

		public class Options
		{
			public SqlBuilder SqlBuilder;
			public IExpressionTranslator ExpressionTranslator;
		}

		protected readonly Options _options;

		protected readonly DbConnection _db;

		bool _closeDb;

		public SDCDatabase(DbConnection db, Options options = null)
		{
			Check.Null(db, nameof(db));

			_db = db;
			_options = options ?? new Options();

			if (_options.SqlBuilder == null)
				_options.SqlBuilder = new SqlBuilder();

			if (_options.ExpressionTranslator == null)
				_options.ExpressionTranslator = new Expression2SqlTranslator(_options.SqlBuilder, null);
		}

		public virtual void Dispose()
		{
			if (_closeDb)
				_db?.Close();
			_closeDb = false;
		}

		public SqlBuilder GetSqlBuilder()
		{
			return _options.SqlBuilder.New();
		}

		public IExpressionTranslator GetExpressionTranslator(Type typeForWhichMemberNamesWillBeEmitted = null)
		{
			return _options.ExpressionTranslator.New(typeForWhichMemberNamesWillBeEmitted);
		}

		public virtual async Task OpenAsync()
		{
			if (_db.State == System.Data.ConnectionState.Broken)
				_db.Close();
			if (_db.State == System.Data.ConnectionState.Closed)
			{
				await _db.OpenAsync().ConfigureAwait(false);
				_closeDb = true;

				Log.I($"opened connection to database '{_db.DataSource}/{_db.Database}'.", this);
			}
			if (_db.State != System.Data.ConnectionState.Open)
			{
				await Task.Delay(1000).ConfigureAwait(false);

				if (_db.State != System.Data.ConnectionState.Open)
					throw new Exception($"Unable to connect to database '{_db.DataSource}/{_db.Database}'."); // TODO: translation
			}
		}

		public virtual async Task CloseAsync()
		{
			if (_closeDb)
				_db.Close();
			_closeDb = false;
		}

		public virtual string ConvertTypeToDbType(Type type, int width = int.MaxValue)
		{
			SqlBuilder sqlBuilder = GetSqlBuilder();

			Type underlyingType = Nullable.GetUnderlyingType(type);
			bool typeIsNullable = underlyingType != null || !type.GetTypeInfo().IsValueType;
			type = underlyingType ?? type;

			if (type == typeof(string))
			{
				if (width == int.MaxValue)
					sqlBuilder.Expr().NText();
				else
					sqlBuilder.Expr().NVarchar(width);
			}
			else if (type == typeof(bool))
			{
				sqlBuilder.Expr().Boolean();
			}
			else if (type == typeof(Int16) || type == typeof(UInt16))
			{
				sqlBuilder.Expr().Smallint();
			}
			else if (type == typeof(Int32) || type == typeof(UInt32))
			{
				sqlBuilder.Expr().Int();
			}
			else if (type == typeof(Int64) || type == typeof(UInt64))
			{
				sqlBuilder.Expr().Bigint();
			}
			// TODO: handle more conversion from C# type to SQL type
			else
			{
				throw new InvalidCastException($"Unsupported type '{type.ToString()}'."); // TODO: localization and better text
			}

			return sqlBuilder.Build();
		}

		public virtual object ConvertDbValueToValue(string dbType, object dbValue, Type valueType)
		{
			Check.Null(valueType, nameof(valueType));

			Type underlyingType = Nullable.GetUnderlyingType(valueType);
			bool valueTypeIsNullable = underlyingType != null || !valueType.GetTypeInfo().IsValueType;
			valueType = underlyingType ?? valueType;

			if (dbValue == null || dbValue.GetType() == typeof(DBNull))
			{
				if (valueTypeIsNullable)
					return null;
				else
					throw new InvalidCastException($"Can not convert NULL to value type '{valueType.Name}'."); // TODO: translation
			}

			// dbType CAN BE null -> caller don't know this information
			var v = Convert.ChangeType(dbValue, valueType);
			return v;
		}

		public virtual object ConvertValueToDbValue(Type type, object value, string dbType)
		{
			Check.Null(type, nameof(type));

			if (value == null)
				return DBNull.Value;

			// TODO: handle conversions
			// dbType CAN BE null -> caller don't know this information

			return value;
		}

		protected enum CommandType
		{
			Statement,
			Scalar,
			Query,
			Table,
		};

#if DEBUG
		void DumpParameters(DbCommand cmd, [CallerMemberName]string callerName = null)
		{
			string s = "";
			foreach (DbParameter p in cmd.Parameters)
				s += (s == "" ? "" : ", ") +
					$"{_options.SqlBuilder.ParameterPrefix}{p.ParameterName} = {{{p.Value.GetType().Name}: {p.Value.ToString()}}}";
			Log.D(s, this, callerName);
		}
#endif

		protected virtual void CreateParameters(DbCommand cmd, params (string name, object value)[] parameters)
		{
			Check.Null(cmd, nameof(cmd));

			if (parameters == null)
				return;

			foreach (var param in parameters)
			{
				DbParameter dbParam = cmd.CreateParameter();

				dbParam.Value = ConvertValueToDbValue(param.GetType(), param.value, null);
				dbParam.ParameterName = param.name;

				cmd.Parameters.Add(dbParam);
			}
#if DEBUG
			DumpParameters(cmd);
#endif
		}

		protected DbCommand CreateCommand(CommandType type, string sql, params (string name, object value)[] parameters)
		{
			DbCommand cmd = CreateCommand(type, sql);
			CreateParameters(cmd, parameters);
			return cmd;
		}

		protected DbCommand CreateCommand(CommandType type, string sql, params object[] parameters)
		{
			if (parameters == null)
				return CreateCommand(type, sql);

			(string name, object value)[] _parameters = new(string name, object value)[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
				_parameters[i] = ($"{_options.SqlBuilder.AutoGeneratedParameterPrefix}{i + 1}", parameters[i]);

			return CreateCommand(type, sql, _parameters);
		}

		protected virtual DbCommand CreateCommand(CommandType type, string sql)
		{
			Check.Empty(sql, nameof(sql));

			DbCommand cmd = _db.CreateCommand();

			cmd.CommandText = sql;

			if (type == CommandType.Table)
			{
				cmd.CommandType = System.Data.CommandType.TableDirect;
			}
			else
			{
				cmd.CommandType = System.Data.CommandType.Text;
				cmd.CommandText += ";";
			}

			Log.D($"{type}: {sql}", this);

			return cmd;
		}

		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, string sql, params (string name, object value)[] parameters) => await ExecuteCommandAsync<T>(type, CreateCommand(type, sql, parameters), true).ConfigureAwait(false);

		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, string sql, params object[] parameters) => await ExecuteCommandAsync<T>(type, CreateCommand(type, sql, parameters), true).ConfigureAwait(false);

		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, string sql) => await ExecuteCommandAsync<T>(type, CreateCommand(type, sql), true).ConfigureAwait(false);

		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, DbCommand cmd) => await ExecuteCommandAsync<T>(type, cmd, false).ConfigureAwait(false);

		protected virtual async Task<T> ExecuteCommandAsync<T>(CommandType type, DbCommand cmd, bool shouldDisposeCmd)
		{
			Check.Null(cmd, nameof(cmd));
			Check.True(type == CommandType.Statement || type == CommandType.Scalar, $"Incorrect value '{type}'.", nameof(type)); // TODO: translation

			await OpenAsync().ConfigureAwait(false);

			try
			{
				object v = null;

				if (type == CommandType.Statement)
					v = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
				else if (type == CommandType.Scalar)
					v = await cmd.ExecuteScalarAsync().ConfigureAwait(false);

				return (T)ConvertDbValueToValue(null, v, typeof(T));
			}
			finally
			{
				if (shouldDisposeCmd)
					cmd.Dispose();
			}
		}

		protected virtual async Task<IQueryResult<T>> ExecuteReaderAsync<T>(DbCommand cmd, bool shouldDisposeCmd) where T : new()
		{
			Check.Null(cmd, nameof(cmd));

			await OpenAsync().ConfigureAwait(false);

			return new SDCQueryResult<T>(this, shouldDisposeCmd ? cmd : null, await cmd.ExecuteReaderAsync().ConfigureAwait(false));
		}

		public async Task<int> StatementAsync(string sql, params (string name, object value)[] parameters) => await ExecuteCommandAsync<int>(CommandType.Statement, sql, parameters).ConfigureAwait(false);

		public async Task<int> StatementAsync(string sql, params object[] parameters) => await ExecuteCommandAsync<int>(CommandType.Statement, sql, parameters).ConfigureAwait(false);

		public async Task<int> StatementAsync(string sql) => await ExecuteCommandAsync<int>(CommandType.Statement, sql).ConfigureAwait(false);

		public async Task<T> ScalarAsync<T>(string sql, params (string name, object value)[] parameters) => await ExecuteCommandAsync<T>(CommandType.Scalar, sql, parameters).ConfigureAwait(false);

		public async Task<T> ScalarAsync<T>(string sql, params object[] parameters) => await ExecuteCommandAsync<T>(CommandType.Scalar, sql, parameters).ConfigureAwait(false);

		public async Task<T> ScalarAsync<T>(string sql) => await ExecuteCommandAsync<T>(CommandType.Scalar, sql).ConfigureAwait(false);

		public async Task<IQueryResult<T>> QueryAsync<T>(string sql, params (string name, object value)[] parameters) where T : new()
			=> await ExecuteReaderAsync<T>(CreateCommand(CommandType.Query, sql, parameters), true).ConfigureAwait(false);

		public async Task<IQueryResult<T>> QueryAsync<T>(string sql, params object[] parameters) where T : new()
			=> await ExecuteReaderAsync<T>(CreateCommand(CommandType.Query, sql, parameters), true).ConfigureAwait(false);

		public async Task<IQueryResult<T>> QueryAsync<T>(string sql) where T : new()
			=> await ExecuteReaderAsync<T>(CreateCommand(CommandType.Query, sql), true).ConfigureAwait(false);

		public virtual IQuery<T> Query<T>(string sql) where T : new()
		{
			Check.Empty(sql, nameof(sql));

			return new SqlQuery<T>(this, sql, "");
		}

		public ITable<T> Table<T>(string tableName, bool createIfNotExists = true) where T : new() => TableAsync<T>(tableName, createIfNotExists).GetAwaiter().GetResult();

		public virtual async Task<ITable<T>> TableAsync<T>(string tableName, bool createIfNotExists = true) where T : new()
		{
			Check.Empty(tableName, nameof(tableName));

			if (createIfNotExists)
			{
				// TODO: better method to find if table exists?
				try
				{
					await ScalarAsync<object>(GetSqlBuilder().Select().E("1").From(tableName)).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					return await SqlTable<T>.CreateAsync(this, tableName);
				}
			}

			return await SqlTable<T>.OpenAsync(this, tableName);
		}

		public virtual async Task DropTableAsync(string tableName)
		{
			Check.Empty(tableName, nameof(tableName));

			await StatementAsync(GetSqlBuilder().Drop().Table(tableName)).ConfigureAwait(false);
		}
	}
}
