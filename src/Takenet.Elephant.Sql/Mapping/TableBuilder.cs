using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Takenet.Elephant.Sql.Mapping
{
    /// <summary>
    /// Utility class to allow build tables using fluent notation.
    /// </summary>
    public sealed class TableBuilder
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the table columns.
        /// </summary>
        /// <value>
        /// The columns.
        /// </value>
        public List<KeyValuePair<string, SqlType>> Columns { get; }

        /// <summary>
        /// Gets the table key columns names.
        /// </summary>
        /// <value>
        /// The key columns.
        /// </value>
        public List<string> KeyColumns { get; }

        private TableBuilder(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            Name = name;
            Columns = new List<KeyValuePair<string, SqlType>>();
            KeyColumns = new List<string>();
        }

        /// <summary>
        /// Creates a table builder using the specified table name.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static TableBuilder WithName(string columnName)
        {            
            return new TableBuilder(columnName);
        }

        /// <summary>
        /// Adds a column to the table with the specified name and type.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="sqlType"></param>
        /// <returns></returns>
        public TableBuilder WithColumn(string columnName, SqlType sqlType)
        {
            Columns.Add(new KeyValuePair<string, SqlType>(columnName, sqlType));
            return this;
        }

        /// <summary>
        /// Adds columns to the table with the specified names and types.
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public TableBuilder WithColumns(params KeyValuePair<string, SqlType>[] columns)
        {
            Columns.AddRange(columns);
            return this;
        }

        /// <summary>
        /// Adds a key column to the table from the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public TableBuilder WithColumnFromType<T>(string columnName)
        {
            if (columnName == null) throw new ArgumentNullException(nameof(columnName));
            var column = new KeyValuePair<string, SqlType>(columnName, new SqlType(DbTypeMapper.GetDbType(typeof(T))));
            Columns.Add(column);
            return this;
        }

        /// <summary>
        /// Adds a key column to the table from the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnName"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public TableBuilder WithColumnFromType<T>(string columnName, int length)
        {
            if (columnName == null) throw new ArgumentNullException(nameof(columnName));
            var column = new KeyValuePair<string, SqlType>(columnName, new SqlType(DbTypeMapper.GetDbType(typeof(T)), length));
            Columns.Add(column);
            return this;
        }

        /// <summary>
        /// Adds a key column to the table from the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnName"></param>
        /// <param name="precision"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public TableBuilder WithColumnFromType<T>(string columnName, int precision, int scale)
        {
            if (columnName == null) throw new ArgumentNullException(nameof(columnName));
            var column = new KeyValuePair<string, SqlType>(columnName, new SqlType(DbTypeMapper.GetDbType(typeof(T)), precision, scale));
            Columns.Add(column);
            return this;
        }

        /// <summary>
        /// Adds columns to the table from the specified type properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TableBuilder WithColumnsFromTypeProperties<T>()
        {
            return WithColumnsFromTypeProperties<T>(p => true);
        }

        /// <summary>
        /// Adds columns to the table from the specified type <see cref="DataMemberAttribute"/> decorated properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TableBuilder WithColumnsFromTypeDataMemberProperties<T>()
        {
            return WithColumnsFromTypeProperties<T>(p => p.GetCustomAttribute<DataMemberAttribute>() != null);
        }

        /// <summary>
        /// Adds columns to the table from the specified type properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public TableBuilder WithColumnsFromTypeProperties<T>(Func<PropertyInfo, bool> filter)
        {
            Columns.AddRange(
                typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(filter).ToSqlColumns());
            return this;
        }

        /// <summary>
        /// Adds key columns to the table from the specified type properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TableBuilder WithKeyColumnsFromTypeProperties<T>()
        {
            return WithKeyColumnsFromTypeProperties<T>(p => true);
        }

        /// <summary>
        /// Adds key columns to the table from the specified type <see cref="DataMemberAttribute"/> decorated properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TableBuilder WithKeyColumnsFromTypeDataMemberProperties<T>()
        {
            return WithKeyColumnsFromTypeProperties<T>(p => p.GetCustomAttribute<DataMemberAttribute>() != null);
        }

        /// <summary>
        /// Adds key columns to the table from the specified type properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public TableBuilder WithKeyColumnsFromTypeProperties<T>(Func<PropertyInfo, bool> filter)
        {
            var columns = typeof (T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(filter).ToSqlColumns();
            Columns.AddRange(columns);
            KeyColumns.AddRange(columns.Keys);            
            return this;
        }

        /// <summary>
        /// Adds a key column to the table from the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keyColumnName"></param>
        /// <param name="isIdentity"></param>
        /// <returns></returns>
        public TableBuilder WithKeyColumnFromType<T>(string keyColumnName, bool isIdentity = false)
        {
            if (keyColumnName == null) throw new ArgumentNullException(nameof(keyColumnName));
            var column = new KeyValuePair<string, SqlType>(keyColumnName, new SqlType(DbTypeMapper.GetDbType(typeof (T)), isIdentity));
            Columns.Add(column);
            KeyColumns.Add(keyColumnName);
            return this;
        }

        /// <summary>
        /// Adds key columns names to the table. The specified columns must exists on the table.
        /// </summary>
        /// <param name="keyColumnsNames"></param>
        /// <returns></returns>
        public TableBuilder WithKeyColumnsNames(params string[] keyColumnsNames)
        {
            KeyColumns.AddRange(keyColumnsNames);
            return this;
        }

        /// <summary>
        /// Builds a table with the builder data.
        /// </summary>
        /// <returns></returns>
        public ITable Build()
        {
            return new Table(Name, KeyColumns.ToArray(), Columns.ToDictionary(c => c.Key, c => c.Value));            
        }
    }
}