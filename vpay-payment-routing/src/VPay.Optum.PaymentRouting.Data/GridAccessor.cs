using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dapper;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data
{
    // NOTE: This class was generated from the SqlMapper.GridReader

    [ExcludeFromCodeCoverage]
    public class GridAccessor : IGridAccessor
    {
        private readonly SqlMapper.GridReader _gridReader;

        public GridAccessor(SqlMapper.GridReader gridReader) =>
            _gridReader = gridReader;

        public bool IsConsumed => _gridReader.IsConsumed;

        public IDbCommand Command
        {
            get => _gridReader.Command;
            set => _gridReader.Command = value;
        }

        public IEnumerable<dynamic> Read(bool buffered = true) =>
            _gridReader.Read(buffered);

        public IEnumerable<T> Read<T>(bool buffered = true) =>
            _gridReader.Read<T>(buffered);

        public IEnumerable<object> Read(Type type, bool buffered = true) =>
            _gridReader.Read(type, buffered);

        public IEnumerable<TReturn> Read<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn = "id", bool buffered = true) =>
            _gridReader.Read(types, map, splitOn, buffered);

        public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func, string splitOn = "id", bool buffered = true) =>
            _gridReader.Read(func, splitOn, buffered);

        public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> func, string splitOn = "id", bool buffered = true) =>
            _gridReader.Read(func, splitOn, buffered);

        public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn = "id", bool buffered = true) =>
            _gridReader.Read(func, splitOn, buffered);

        public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> func, string splitOn = "id", bool buffered = true) =>
            _gridReader.Read(func, splitOn, buffered);

        public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> func, string splitOn = "id", bool buffered = true) =>
            _gridReader.Read(func, splitOn, buffered);

        public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> func, string splitOn = "id", bool buffered = true) =>
            _gridReader.Read(func, splitOn, buffered);

        public dynamic ReadFirst() =>
            _gridReader.ReadFirst();

        public T ReadFirst<T>() =>
            _gridReader.ReadFirst<T>();

        public object ReadFirst(Type type) =>
            _gridReader.ReadFirst(type);

        public dynamic ReadFirstOrDefault() =>
            _gridReader.ReadFirstOrDefault();

        public T ReadFirstOrDefault<T>() =>
            _gridReader.ReadFirstOrDefault<T>();

        public object ReadFirstOrDefault(Type type) =>
            _gridReader.ReadFirstOrDefault(type);

        public dynamic ReadSingle() =>
            _gridReader.ReadSingle();

        public T ReadSingle<T>() =>
            _gridReader.ReadSingle<T>();

        public object ReadSingle(Type type) =>
            _gridReader.ReadSingle(type);

        public dynamic ReadSingleOrDefault() =>
            _gridReader.ReadSingleOrDefault();

        public T ReadSingleOrDefault<T>() =>
            _gridReader.ReadSingleOrDefault<T>();

        public object ReadSingleOrDefault(Type type) =>
            _gridReader.ReadSingleOrDefault(type);

        public Task<IEnumerable<dynamic>> ReadAsync(bool buffered = true) =>
            _gridReader.ReadAsync(buffered);

        public Task<IEnumerable<object>> ReadAsync(Type type, bool buffered = true) =>
            _gridReader.ReadAsync(type, buffered);

        public Task<IEnumerable<T>> ReadAsync<T>(bool buffered = true) =>
            _gridReader.ReadAsync<T>(buffered);

        public Task<dynamic> ReadFirstAsync() =>
            _gridReader.ReadFirstAsync();

        public Task<object> ReadFirstAsync(Type type) =>
            _gridReader.ReadFirstAsync(type);

        public Task<T> ReadFirstAsync<T>() =>
            _gridReader.ReadFirstAsync<T>();

        public Task<dynamic> ReadFirstOrDefaultAsync() =>
            _gridReader.ReadFirstOrDefaultAsync();

        public Task<object> ReadFirstOrDefaultAsync(Type type) =>
            _gridReader.ReadFirstOrDefaultAsync(type);

        public Task<T> ReadFirstOrDefaultAsync<T>() =>
            _gridReader.ReadFirstOrDefaultAsync<T>();

        public Task<dynamic> ReadSingleAsync() =>
            _gridReader.ReadSingleAsync();

        public Task<object> ReadSingleAsync(Type type) =>
            _gridReader.ReadSingleAsync(type);

        public Task<T> ReadSingleAsync<T>() =>
            _gridReader.ReadSingleAsync<T>();

        public Task<dynamic> ReadSingleOrDefaultAsync() =>
            _gridReader.ReadSingleOrDefaultAsync();

        public Task<object> ReadSingleOrDefaultAsync(Type type) =>
            _gridReader.ReadSingleOrDefaultAsync(type);

        public Task<T> ReadSingleOrDefaultAsync<T>() =>
            _gridReader.ReadSingleOrDefaultAsync<T>();

        public void Dispose() =>
            _gridReader.Dispose();
    }
}
