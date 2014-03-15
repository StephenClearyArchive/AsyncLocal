using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx.AsyncLocal;

namespace UnitTests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void IsValueSet_BeforeValueIsSet_ReturnsFalse()
        {
            var local = new AsyncLocal<int>();
            Assert.IsFalse(local.IsValueSet);
        }

        [TestMethod]
        public void IsValueSet_AfterValueIsSet_ReturnsTrue()
        {
            var local = new AsyncLocal<int>();
            local.Value = 13;
            Assert.IsTrue(local.IsValueSet);
        }

        [TestMethod]
        public void IsValueSet_AfterValueIsSetAndCleared_ReturnsFalse()
        {
            var local = new AsyncLocal<int>();
            local.Value = 13;
            local.ClearValue();
            Assert.IsFalse(local.IsValueSet);
        }

        [TestMethod]
        public void IsValueSet_WithoutSpecifiedDefaultValue_AfterValueIsRead_ReturnsFalse()
        {
            var local = new AsyncLocal<int>();
            var value = local.Value;
            Assert.IsFalse(local.IsValueSet);
        }

        [TestMethod]
        public void IsValueSet_WithSpecifiedDefaultValue_AfterValueIsRead_ReturnsFalse()
        {
            var local = new AsyncLocal<int>(13);
            var value = local.Value;
            Assert.IsFalse(local.IsValueSet);
        }

        [TestMethod]
        public void Value_WithoutSpecifiedDefaultValue_ReturnsTypeDefault()
        {
            var local = new AsyncLocal<int>();
            var value = local.Value;
            Assert.AreEqual(0, value);
        }

        [TestMethod]
        public void Value_WithSpecifiedDefaultValue_ReturnsSpecifiedDefault()
        {
            var local = new AsyncLocal<int>(13);
            var value = local.Value;
            Assert.AreEqual(13, value);
        }

        [TestMethod]
        public void Value_SetBySyncMethod_IsInheritedByAnotherThread()
        {
            var local = new AsyncLocal<int>();
            local.Value = 13;
            Task.Run(() => Assert.AreEqual(13, local.Value)).Wait();
        }

        [TestMethod]
        public async Task Value_SetByAsyncMethod_IsInheritedByAnotherThread()
        {
            var local = new AsyncLocal<int>();
            local.Value = 13;
            await Task.Run(() => Assert.AreEqual(13, local.Value));
        }

        [TestMethod]
        public void Value_IsInheritedBySyncMethod()
        {
            var local = new AsyncLocal<int>();
            local.Value = 13;
            Action action = () => Assert.AreEqual(13, local.Value);
            action();
        }

        [TestMethod]
        public async Task Value_IsInheritedByAsyncMethod()
        {
            var local = new AsyncLocal<int>();
            local.Value = 13;
            Func<Task> action = async () =>
            {
                await Task.Yield();
                Assert.AreEqual(13, local.Value);
            };
            await action();
        }

        [TestMethod]
        public async Task Value_SetAfterAsyncMethodStarts_IsNotInheritedByAsyncMethod()
        {
            var local = new AsyncLocal<int>();
            var tcs = new TaskCompletionSource<object>();
            Func<Task> action = async () =>
            {
                await tcs.Task;
                Assert.AreEqual(0, local.Value);
            };
            var task = action();
            local.Value = 13;
            tcs.TrySetResult(null);
            await task;
        }

        [TestMethod]
        public async Task Value_SetBeforeAsyncMethodStarts_UpdatedAfterAsyncMethodStarts_OnlyOriginalValueIsInheritedByAsyncMethod()
        {
            var local = new AsyncLocal<int>();
            local.Value = 13;
            var tcs = new TaskCompletionSource<object>();
            Func<Task> action = async () =>
            {
                await tcs.Task;
                Assert.AreEqual(13, local.Value);
            };
            var task = action();
            local.Value = 17;
            tcs.TrySetResult(null);
            await task;
        }

        [TestMethod]
        public void Value_DoesNotTravelAcrossAppDomains()
        {
            DomainHelper.Local.Value = 13;

            var location = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var domain = AppDomain.CreateDomain("Other", null, location, null, false);
            var domainHelperType = typeof(DomainHelper);
            var proxy = (DomainHelper)domain.CreateInstanceAndUnwrap(domainHelperType.Assembly.FullName, domainHelperType.FullName);

            Assert.AreEqual(17, proxy.GetValue());
            Assert.AreEqual(13, DomainHelper.Local.Value);
        }

        [Serializable]
        public class DomainHelper
        {
            private readonly int _localValue = Local.Value;

            public static readonly AsyncLocal<int> Local = new AsyncLocal<int>(17);

            public int GetValue()
            {
                return _localValue;
            }
        }
    }
}
