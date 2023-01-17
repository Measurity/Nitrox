using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NitroxModel.Platforms.OS.Windows.Internal;

namespace NitroxModel.Helper
{
    [TestClass]
    public class KeyValueStoreTest
    {
        [TestMethod]
        public void SetAndReadValue()
        {
            const string TEST_KEY = "test";

            KeyValueStore.SetValue<int>(TEST_KEY, -50);
            Assert.AreEqual(-50, KeyValueStore.GetValue<int>(TEST_KEY));

            KeyValueStore.SetValue<int>(TEST_KEY, 1337);
            Assert.AreEqual(1337, KeyValueStore.GetValue<int>(TEST_KEY));


            // Cleanup
            KeyValueStore.DeleteKey(TEST_KEY);
            Assert.IsNull(KeyValueStore.GetValue<int>(TEST_KEY));
        }
    }
}
