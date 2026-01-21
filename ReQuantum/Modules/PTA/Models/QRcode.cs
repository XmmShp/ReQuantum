using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel; // 推荐使用此包简化 MVVM

namespace ReQuantum.Modules.PTA.Models
{
    internal partial class QRcode  // ← 添加 partial 关键字
    {
        public static Bitmap DecodeBase64ToBitmap(string base64String)
        {
            if (base64String.Contains(","))
            {
                base64String = base64String.Split(',')[1];
            }

            // 将 Base64 转换为字节数组
            byte[] imageBytes = Convert.FromBase64String(base64String);

            // 使用 MemoryStream 将字节流转换为 Avalonia Bitmap
            using (var ms = new MemoryStream(imageBytes))
            {
                return new Bitmap(ms);
            }
        }

        public partial class LoginViewModel : ObservableObject
        {
            [ObservableProperty]
            private Bitmap? _qrCodeSource;

            public void UpdateQrCode(string base64String)
            {
                try
                {
                    QrCodeSource = DecodeBase64ToBitmap(base64String);
                }
                catch (Exception)
                {
                    Console.WriteLine($"图片转换失败");
                }
            }
        }
    }
}