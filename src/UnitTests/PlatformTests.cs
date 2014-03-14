using System;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    // Some behavior of the logical call context is undocumented.
    // These tests ensure any behavior we rely on doesn't change.
    [TestClass]
    public class PlatformTests
    {
        [TestMethod]
        public void Value_IsInheritedByAnotherThread()
        {
            var slot = Guid.NewGuid().ToString("N");
            const int value = 13;
            CallContext.LogicalSetData(slot, value);
            Task.Run(() =>
            {
                var threadValue = (int)CallContext.LogicalGetData(slot);
                Assert.AreEqual(value, threadValue);
            }).Wait();
        }

        [TestMethod]
        public void Value_ModifiedByThread_IsNotPropagatedToOriginalThread()
        {
            var slot = Guid.NewGuid().ToString("N");
            const int value = 13;
            const int threadValue = 17;
            CallContext.LogicalSetData(slot, value);
            Task.Run(() => CallContext.LogicalSetData(slot, threadValue)).Wait();
            var result = (int)CallContext.LogicalGetData(slot);
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public void MissingValue_AddedByThread_IsNotPropagatedToOriginalThread()
        {
            var slot = Guid.NewGuid().ToString("N");
            const int value = 13;
            Task.Run(() => CallContext.LogicalSetData(slot, value)).Wait();
            var result = CallContext.LogicalGetData(slot);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void MissingValue_AddedByMethod_IsPropagatedToOriginalMethod()
        {
            var slot = Guid.NewGuid().ToString("N");
            const int value = 13;
            Action action = () => CallContext.LogicalSetData(slot, value);
            action();
            var result = (int)CallContext.LogicalGetData(slot);
            Assert.AreEqual(value, result);
        }
    }
}
