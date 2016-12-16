﻿namespace Task.Schedu.Data
{
    using System;
    using System.Collections.Generic;
    using MySql.Data.MySqlClient;
    using DapperExtensions;
    using DapperExtensions.Mapper;
    using DapperExtensions.Sql;
    using System.Data;
    using System.Data.SqlClient;
    using System.Reflection;
    /// <summary>
    /// 数据库类型
    /// </summary>
    public enum DbType
    {
        SqlServer,
        MySql
    }
    public class DbAccess<T>
    {
        protected DbType _dbType = DbType.MySql;
        public DbAccess(DbType dbType)
        {
            this._dbType = dbType;
        }
        /// <summary>
        /// Void
        /// </summary>
        /// <param name="action"></param>
        protected void Void(Action<IDatabase> action, string dbConnect)
        {
            using (var client = DbFactory.CreateDb(_dbType, dbConnect))
            {
                action(client);
            }
        }

        /// <summary>
        /// FindBy
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        protected IEnumerable<T> FindBy(Func<IDatabase, IEnumerable<T>> func, string dbConnect)
        {
            using (var client = DbFactory.CreateDb(_dbType, dbConnect))
            {
                return func(client);
            }
        }
        /// <summary>
        /// FirstOrDefault
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        protected T FirstOrDefault(Func<IDatabase, T> func, string dbConnect)
        {
            using (var client = DbFactory.CreateDb(_dbType, dbConnect))
            {
                return func(client);
            }
        }
        /// <summary>
        ///  Insert、Update、Delete
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        protected bool Commit(Func<IDatabase, bool> func, string dbConnect)
        {
            using (var client = DbFactory.CreateDb(_dbType, dbConnect))
            {
                return func(client);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class DbFactory
    {
        public static IDatabase CreateDb(DbType dbType, string dbConnect)
        {
            IDbConnection connection = null;
            IDatabase dbDatabase = null;
            var config = new DapperExtensionsConfiguration();
            switch (dbType)
            {
                case DbType.SqlServer:
                    connection = new SqlConnection(dbConnect);
                    config = new DapperExtensionsConfiguration(typeof(AutoClassMapper<>), new List<Assembly>(),
                        new SqlServerDialect());

                    break;
                case DbType.MySql:
                    connection = new MySqlConnection(dbConnect);
                    config = new DapperExtensionsConfiguration(typeof(AutoClassMapper<>), new List<Assembly>(),
                        new MySqlDialect());
                    break;
            }
            var sqlGenerator = new SqlGeneratorImpl(config);
            dbDatabase = new Database(connection, sqlGenerator);
            return dbDatabase;
        }
    }
}