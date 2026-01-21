using Avalonia.Media.Imaging;
using Microsoft.Playwright;
using ReQuantum.Modules.Common.Attributes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReQuantum.Modules.PTA.Service;

public interface IQRcodeGettingService
{
    Task<string?> GetQrCodeBase64Async();
}

[AutoInject(Lifetime.Singleton)]
public class QRcodeGettingService : IQRcodeGettingService
{
    public async Task<string?> GetQrCodeBase64Async()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();

        try
        {
            await page.GotoAsync("https://pintia.cn/auth/login?tab=wechatLogin");

            var imgs = await page.QuerySelectorAllAsync("img");
            foreach (var img in imgs)
            {
                var src = await img.GetAttributeAsync("src");
                if (src != null && src.StartsWith("data:image"))
                {
                    return src.Split(',')[1];
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("发生异常: " + ex.Message);
            return null;
        }
    }

    public static Bitmap DecodeBase64ToBitmap(string base64String)
    {
        if (base64String.Contains(","))
        {
            base64String = base64String.Split(',')[1];
        }

        // 将 Base64 转换为字节数组
        byte[] imageBytes = Convert.FromBase64String(base64String);

        // 使用 MemoryStream 将字节流转换为 Avalonia Bitmap
        using var ms = new MemoryStream(imageBytes);
        return new Bitmap(ms);
    }
}