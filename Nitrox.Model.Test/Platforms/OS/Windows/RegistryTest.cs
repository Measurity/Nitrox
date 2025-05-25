using System.Threading.Tasks;

namespace NitroxModel.Platforms.OS.Windows;

[TestClass]
#if NET9_0_OR_GREATER
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
public class RegistryTest
{
    [OSTestMethod("windows")]
    public async Task WaitsForRegistryKeyToExist()
    {
        const string PATH_TO_KEY = @"SOFTWARE\Nitrox\test";

        RegistryEx.Write(PATH_TO_KEY, 0);
        Task<bool> readTask = Task.Run(async () =>
        {
            try
            {
                await RegistryEx.CompareWaitAsync<int>(PATH_TO_KEY,
                                                   v => v == 1337,
                                                   TimeSpan.FromSeconds(5));
                return true;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        });

        RegistryEx.Write(PATH_TO_KEY, 1337);
        Assert.IsTrue(await readTask);

        // Cleanup (we can keep "Nitrox" key intact).
        RegistryEx.Delete(PATH_TO_KEY);
        Assert.IsNull(RegistryEx.Read<string>(PATH_TO_KEY));
    }
}
