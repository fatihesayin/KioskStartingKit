using System.Diagnostics;
using System.Net;
using System.Reflection;
using Microsoft.Win32;

if (IsChromeInstalled())
{
    Console.WriteLine("Chrome zaten yüklü");
}
else
{
    Console.WriteLine("Google Chrome yüklü değil. Yükleniyor...");
    DownloadAndInstallChrome();
}
ExtractAndRun("Executables/Zadig.exe", "RunZadig.exe");

string subKey = @"SOFTWARE\Policies\Microsoft\Windows\EdgeUI";
string keyName = "AllowEdgeSwipe";
int keyValue = 0;
Register(subKey, keyName, keyValue);
subKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
RegistryKey registryKey = GetOrCreateSubKey(subKey);
Register(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1);

// metods
static void ExtractAndRun(string resourceName, string outputFileName)
{
    var assembly = Assembly.GetExecutingAssembly();
    using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
    {
        if (stream == null)
            throw new ArgumentException("Resource not found: " + resourceName);

        using (FileStream fileStream = new FileStream(outputFileName, FileMode.Create))
        {
            stream.CopyTo(fileStream);
        }
    }

    Process.Start(outputFileName);
}

static void Register(string subKey, string keyName, object keyValue)
{
    try
    {
        RegistryKey key = GetOrCreateSubKey(subKey);
        RegisterKey(key, keyName, keyValue);
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine("Yetki hatası: Bu işlemi yapmak için yönetici izni gereklidir.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Hata: " + ex.Message);
    }
}
static void RegisterKey(RegistryKey key, string keyName, object keyValue)
{
    using (key)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        key.SetValue(keyName, keyValue, RegistryValueKind.DWord);
#pragma warning restore CA1416 // Validate platform compatibility
    }
}

static RegistryKey GetOrCreateSubKey(string subKey)
{
#pragma warning disable CA1416 // Validate platform compatibility
    var keys = Registry.LocalMachine.GetSubKeyNames();
#pragma warning restore CA1416 // Validate platform compatibility
    var keyStr = keys.FirstOrDefault(o => o == subKey);
    RegistryKey? key;
    if (string.IsNullOrWhiteSpace(keyStr))
#pragma warning disable CA1416 // Validate platform compatibility
        key = Registry.LocalMachine.CreateSubKey(subKey);
#pragma warning restore CA1416 // Validate platform compatibility
    else
#pragma warning disable CA1416 // Validate platform compatibility
        key = Registry.LocalMachine.OpenSubKey(keyStr);
#pragma warning restore CA1416 // Validate platform compatibility

    return key!;
}

static bool IsChromeInstalled()
{
    string chromeKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    bool isInstalled = false;

    // HKEY_LOCAL_MACHINE ve HKEY_CURRENT_USER altında kontrol et
#pragma warning disable CA1416 // Validate platform compatibility
    isInstalled = CheckRegistryKey(Registry.LocalMachine, chromeKey) || CheckRegistryKey(Registry.CurrentUser, chromeKey);
#pragma warning restore CA1416 // Validate platform compatibility

    return isInstalled;
}

static bool CheckRegistryKey(RegistryKey baseKey, string subKey)
{
    using (RegistryKey? key = baseKey.OpenSubKey(subKey))
    {
        if (key != null)
        {
            foreach (string subkeyName in key.GetSubKeyNames())
            {
                using (RegistryKey? subkey = key.OpenSubKey(subkeyName))
                {
                    if (subkey != null)
                    {
                        object? displayName = subkey.GetValue("DisplayName");
                        if (displayName != null && displayName.ToString()!.Contains("Google Chrome"))
                        {
                            return true;
                        }
                    }
                }
            }
        }
    }

    return false;
}

static void DownloadAndInstallChrome()
{
    string chromeUrl = "https://dl.google.com/chrome/install/latest/chrome_installer.exe";
    string tempFilePath = Path.GetTempFileName() + ".exe";

#pragma warning disable SYSLIB0014 // Type or member is obsolete
    using (WebClient webClient = new())
    {
        webClient.DownloadFile(chromeUrl, tempFilePath);
    }
#pragma warning restore SYSLIB0014 // Type or member is obsolete

    Process process = new();
    process.StartInfo.FileName = tempFilePath;
    process.StartInfo.UseShellExecute = true;
    process.Start();
    process.WaitForExit();

    Console.WriteLine("Google Chrome yüklemesi tamamlandı.");
}