using Evolution_Backend.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Evolution_Backend
{
    internal static class BsonExtension
    {
        public static TDocument FindOne<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, FindOptions options = null)
        {
            return collection.Find(filter, options).FirstOrDefault();
        }

        public static async Task<TDocument> FindOneAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, FindOptions options = null)
        {
            return await collection.Find(filter, options).FirstOrDefaultAsync();
        }

        public static IEnumerable<TDocument> FindMany<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, FindOptions options = null)
        {
            return collection.Find(filter, options).ToEnumerable();
        }

        public static async Task<IEnumerable<TDocument>> FindManyAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, FindOptions options = null)
        {
            return await collection.Find(filter, options).ToListAsync();
        }

        public static T GetValue<T>(this BsonDocument document, params string[] sourcePaths)
        {
            if (sourcePaths == null || sourcePaths.Length <= 0)
                return default(T);

            try
            {
                BsonValue value = document[sourcePaths[0]];
                for (int i = 1; i < sourcePaths.Length; i++)
                {
                    value = value[sourcePaths[i]];
                }
                return BsonSerializer.Deserialize<T>(value.ToJson());
            }
            catch
            {
                return default(T);
            }
        }

        public static T MapTo<T>(this BsonDocument document)
        {
            return BsonSerializer.Deserialize<T>(document);
        }

        public static BsonDocument createFilter(this string name, object value, string type)
        {
            switch (type)
            {
                case FilterType.Equal:
                    return name.findEqual(value);
                case FilterType.NotEqual:
                    return name.findNotEqual(value);
                case FilterType.Contain:
                    return name.findContain(value);
                case FilterType.NotContain:
                    return name.findNotContain(value);
                case FilterType.LikeWith:
                    return name.findLikeWith(value);
                case FilterType.StartWith:
                    return name.findStartWith(value);
                case FilterType.EndWith:
                    return name.findEndWith(value);
                case FilterType.GreatThan:
                    return name.findGreatThan(value);
                case FilterType.GreatThanOrEqual:
                    return name.findGreatThanOrEqual(value);
                case FilterType.LessThan:
                    return name.findLessThan(value);
                case FilterType.LessThanOrEqual:
                    return name.findLessThanOrEqual(value);
            }
            return new BsonDocument();
        }

        public static BsonDocument CreateFilter(this IEnumerable<FilterRequest> filters)
        {
            if (filters == null || !filters.Any())
                return null;

            var bsonFilter = new BsonDocument();
            foreach (var filter in filters)
            {
                bsonFilter.AddRange(filter.createFilter());
            }

            return new BsonDocument("$match", bsonFilter);
        }

        public static BsonDocument CreateFilter(params FilterRequest[] filters)
        {
            if (filters == null || !filters.Any())
                return null;

            var bsonFilter = new BsonDocument();
            foreach (var filter in filters)
            {
                bsonFilter.AddRange(filter.createFilter());
            }

            return new BsonDocument("$match", bsonFilter);
        }

        public static BsonDocument CreateSort(this IEnumerable<SortRequest> sorts)
        {
            if (sorts == null || !sorts.Any())
                return null;

            var bsonSort = new BsonDocument();
            foreach (var sort in sorts)
            {
                bsonSort.Add(sort.Name, !string.IsNullOrEmpty(sort.Type) && sort.Type.ToLower() == "desc" ? -1 : 1);
            }

            return new BsonDocument("$sort", bsonSort);
        }

        public static BsonDocument CreateSort(params SortRequest[] sorts)
        {
            if (sorts == null || !sorts.Any())
                return null;

            var bsonSort = new BsonDocument();
            foreach (var sort in sorts)
            {
                bsonSort.Add(sort.Name, !string.IsNullOrEmpty(sort.Type) && sort.Type.ToLower() == "desc" ? -1 : 1);
            }

            return new BsonDocument("$sort", bsonSort);
        }

        public static async Task<long> ResultAsync(this IAggregateFluent<AggregateCountResult> aggregateCount)
        {
            var countResult = await aggregateCount.FirstOrDefaultAsync();
            return countResult == null ? 0 : countResult.Count;
        }

        public static async Task<Dictionary<string, long>> Count<TDocument>(this IMongoCollection<TDocument> collection, BsonDocument filter = null, Dictionary<string, BsonDocument> countInfos = null)
        {
            var aggregate = collection.Aggregate().AppendStage<BsonDocument>(new BsonDocument("$match", new BsonDocument()));

            if (filter != null)
                aggregate = aggregate.AppendStage<BsonDocument>(filter);

            var countData = new Dictionary<string, long>();
            if (countInfos != null && countInfos.Any())
            {
                foreach (var countInfo in countInfos)
                {
                    var aggregateCount = countInfo.Value == null ? aggregate.Count() : aggregate.AppendStage<BsonDocument>(countInfo.Value).Count();
                    var countResult = await aggregateCount.ResultAsync();
                    countData.Add(countInfo.Key, countResult);
                }
            }

            return countData;
        }

        public static async Task<ReadResponse<TResult>> Read<TDocument, TResult>(this IMongoCollection<TDocument> collection, IEnumerable<BsonDocument> stages = null, int skip = 0, int limit = 0, IEnumerable<BsonDocument> afterStages = null)
        {
            var aggregate = collection.Aggregate()
                .AppendStage<TResult>(new BsonDocument("$match", new BsonDocument()));

            if (stages != null && stages.Any())
            {
                foreach (var stage in stages.Where(st => st != null))
                {
                    aggregate = aggregate.AppendStage<TResult>(stage);
                }
            }

            var aggregateCount = aggregate.Count();

            if (skip >= 0)
                aggregate = aggregate.Skip(skip);

            if (limit > 0)
                aggregate = aggregate.Limit(limit);

            if (afterStages != null && afterStages.Any())
            {
                foreach (var stage in afterStages.Where(st => st != null))
                {
                    aggregate = aggregate.AppendStage<TResult>(stage);
                }
            }

            return new ReadResponse<TResult>
            {
                Datas = await aggregate.ToListAsync(),
                Total = await aggregateCount.ResultAsync()
            };
        }

        #region Private methods

        static BsonArray toBsonArray(this object value)
        {
            var bsonArray = new BsonArray();
            try
            {
                var jValue = JToken.FromObject(value);
                if (jValue.Type == JTokenType.Array)
                {
                    foreach (var item in (JArray)jValue)
                    {
                        bsonArray.Add(BsonValue.Create(item.ToObject<object>()));
                    }
                }
                else
                    bsonArray.Add(BsonValue.Create(value));
            }
            catch { }

            return bsonArray;
        }

        static BsonValue toBsonValue(this object value)
        {
            try
            {
                var jValue = JToken.FromObject(value);
                if (jValue.Type == JTokenType.Array)
                {
                    var values = (JArray)jValue;
                    value = values[0].ToObject<object>();
                }
                return BsonValue.Create(value);
            }
            catch
            {
                return BsonNull.Value;
            }
        }

        static BsonDocument findEqual(this string name, object value)
        {
            return new BsonDocument(name, value.toBsonValue());
        }

        static BsonDocument findNotEqual(this string name, object value)
        {
            return new BsonDocument(name, new BsonDocument("$ne", value.toBsonValue()));
        }

        static BsonDocument findContain(this string name, object value)
        {
            return new BsonDocument(name, new BsonDocument("$in", value.toBsonArray()));
        }

        static BsonDocument findNotContain(this string name, object value)
        {
            return new BsonDocument(name, new BsonDocument("$nin", value.toBsonArray()));
        }

        static BsonDocument findLikeWith(this string name, object value)
        {
            if (value.GetType() == typeof(JArray))
            {
                try
                {
                    var jArray = (JArray)value;
                    value = jArray[0].ToObject<string>();
                }
                catch
                {
                    value = "";
                }
            }
            return new BsonDocument(name, new BsonRegularExpression("^.*" + value + ".*$", "i"));
        }

        static BsonDocument findStartWith(this string name, object value)
        {
            if (value.GetType() == typeof(JArray))
            {
                try
                {
                    var jArray = (JArray)value;
                    value = jArray[0].ToObject<string>();
                }
                catch
                {
                    value = "";
                }
            }
            return new BsonDocument(name, new BsonRegularExpression("^" + value + ".*$", "i"));
        }

        static BsonDocument findEndWith(this string name, object value)
        {
            if (value.GetType() == typeof(JArray))
            {
                try
                {
                    var jArray = (JArray)value;
                    value = jArray[0].ToObject<string>();
                }
                catch
                {
                    value = "";
                }
            }
            return new BsonDocument(name, new BsonRegularExpression("^.*" + value + "", "i"));
        }

        static BsonDocument findGreatThan(this string name, object value)
        {
            return new BsonDocument(name, new BsonDocument("$gt", value.toBsonValue()));
        }

        static BsonDocument findGreatThanOrEqual(this string name, object value)
        {
            return new BsonDocument(name, new BsonDocument("$gte", value.toBsonValue()));
        }

        static BsonDocument findLessThan(this string name, object value)
        {
            return new BsonDocument(name, new BsonDocument("$lt", value.toBsonValue()));
        }

        static BsonDocument findLessThanOrEqual(this string name, object value)
        {
            return new BsonDocument(name, new BsonDocument("$lte", value.toBsonValue()));
        }

        static BsonDocument createFilter(this FilterRequest filter)
        {
            if (!filter.Names.Any())
                return new BsonDocument();

            if (filter.Names.Count == 1)
                return (filter.Names[0]).createFilter(filter.Value, filter.Type);

            var bsonArray = new BsonArray();
            foreach (var name in filter.Names)
            {
                bsonArray.Add(name.createFilter(filter.Value, filter.Type));
            }
            return new BsonDocument("$or", bsonArray);
        }

        #endregion
    }
}
