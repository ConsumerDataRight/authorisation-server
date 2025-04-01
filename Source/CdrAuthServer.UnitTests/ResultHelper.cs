using CdrAuthServer.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CdrAuthServer.UnitTests
{
    internal static class ResultHelper
    {
        public static void AssertInstanceOf<T>(object obj, out T converted)
            where T : class
        {
            Assert.IsInstanceOf<T>(obj);
            converted = (T)obj;
        }

        public static void AssertJsonInstanceOf<T>(ObjectResult objectResult, out T converted)
            where T : class, new()
        {
            Assert.IsNotNull(objectResult.Value);
            Assert.IsAssignableFrom<string>(objectResult.Value!);
            try
            {
                converted = JsonConvert.DeserializeObject<T>((string)objectResult.Value!)!;
            }
            catch (JsonException)
            {
                Assert.Fail($"{nameof(objectResult)} cannot be deserialised to {typeof(T).Name}");
                converted = new T();
            }
        }

        public static void AssertJsonInstanceOf<T>(string value, out T instance)
            where T : class, new()
        {
            try
            {
                instance = JsonConvert.DeserializeObject<T>(value)!;
            }
            catch (JsonException)
            {
                Assert.Fail($"{nameof(value)} cannot be deserialised to {typeof(T).Name}");
                instance = new T();
            }
        }

        public static void AssertErrorExpectation(ObjectResult objectResult, string errorCode, string errorTitle, string errorDetail)
        {
            Assert.IsInstanceOf<ResponseErrorList>(objectResult!.Value);
            var errors = objectResult.Value as ResponseErrorList;
            Assert.IsNotNull(errors);
            Assert.AreEqual(1, errors!.Errors.Count);
            var error = errors.Errors[0];
            Assert.AreEqual(errorCode, error.Code);
            Assert.AreEqual(errorTitle, error.Title);
            Assert.AreEqual(errorDetail, error.Detail);
        }
    }
}
