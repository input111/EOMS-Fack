using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;

namespace EOMS2;

public partial class MainPage : ContentPage
{
    private string generatedText = string.Empty;
    private string selectedImagePath = string.Empty;

    public MainPage()
    {
        InitializeComponent();
        WorkOrderEntry.TextChanged += OnTextChanged;
        AffectedPONEntry.TextChanged += OnTextChanged;
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrEmpty(e.NewTextValue))
        {
            string cleanText = e.NewTextValue.Replace(" ", "").Replace("\n", "").Replace("\r", "");
            if (cleanText != e.NewTextValue)
            {
                entry.Text = cleanText;
            }
        }
    }

    private async void OnClearClicked(object sender, EventArgs e)
    {
        bool result = await DisplayAlert("确认", "确定要删除所有填写的信息吗？", "是", "否");
        if (result)
        {
            WorkOrderEntry.Text = string.Empty;
            AffectedPONEntry.Text = string.Empty;
            // 如果还有其他 Entry 需要清空，也在这里加上
            // 例如：GeneratedTextLabel.Text = string.Empty;
        }
    }
    private async Task<bool> RequestStoragePermissionAsync()
    {
#if ANDROID
        var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.StorageWrite>();
        }
        return status == PermissionStatus.Granted;
#else
        return true;
#endif
    }
    private async void OnGenerateClicked(object sender, EventArgs e)
    {
        try
        {
            GenerateButton.IsEnabled = false;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            if (string.IsNullOrWhiteSpace(WorkOrderEntry.Text))
            {
                await DisplayAlert("提示", "请输入工单编号", "确定");
                return;
            }

            if (string.IsNullOrWhiteSpace(AffectedPONEntry.Text))
            {
                await DisplayAlert("提示", "请输入影响的PON口", "确定");
                return;
            }

            if (!FireRadio.IsChecked && !PowerRadio.IsChecked && !ConstructionRadio.IsChecked)
            {
                await DisplayAlert("提示", "请选择消除原因", "确定");
                return;
            }

            string reason = "";
            string sourceFolder = "";
            if (FireRadio.IsChecked)
            {
                reason = "因光缆着火造成光缆故障，无法及时修复";
                sourceFolder = "huo";
            }
            else if (PowerRadio.IsChecked)
            {
                reason = "该小区因建设美丽乡村，无法及时修复";
                sourceFolder = "dian";
            }
            else if (ConstructionRadio.IsChecked)
            {
                reason = "因市政施工造成光缆故障，无法及时修复";
                sourceFolder = "shizheng";
            }

            generatedText = $"工单编号：{WorkOrderEntry.Text}\n消除原因：{reason}，请老师消除该PON口告警，谢谢！\nPON口名称：{AffectedPONEntry.Text}";
            GeneratedTextLabel.Text = generatedText;

            // 改回使用同步方法
            WorkOrderRecordPage.SaveWorkOrder(WorkOrderEntry.Text, AffectedPONEntry.Text);

            await ProcessImage(sourceFolder);
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"生成失败: {ex.Message}", "确定");
        }
        finally
        {
            GenerateButton.IsEnabled = true;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async Task ProcessImage(string sourceFolder)
    {
        try
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string sourcePath = Path.Combine(basePath, sourceFolder);

            var images = Directory.GetFiles(sourcePath, "*.*")
                .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (images.Count == 0)
            {
                await DisplayAlert("错误", $"在 {sourceFolder} 文件夹中没有找到图片", "确定");
                return;
            }

            Random rand = new Random();
            string selectedImage = images[rand.Next(images.Count)];
            string fileName = Path.GetFileName(selectedImage);

#if ANDROID
        int sdkInt = (int)Android.OS.Build.VERSION.SdkInt;
        if (sdkInt <= 29)
        {
            string downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            string eomsPath = Path.Combine(downloadsPath, "eoms");
            if (!Directory.Exists(eomsPath))
            {
                Directory.CreateDirectory(eomsPath);
            }
            string destPath = Path.Combine(eomsPath, fileName);
            File.Copy(selectedImage, destPath, true);
            selectedImagePath = destPath;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PreviewImage.Source = ImageSource.FromFile(selectedImagePath);
            });
        }
        else // Android 11 及以上
        {
            // 使用 MediaStore 保存图片到 Pictures/eoms
            string savedPath = null;
            await Task.Run(() =>
            {
                savedPath = EOMS2.AndroidFileHelper.SaveImageToPictures(selectedImage, fileName);
            });
            selectedImagePath = savedPath ?? selectedImage;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PreviewImage.Source = ImageSource.FromFile(selectedImagePath);
            });
            await DisplayAlert("成功", "图片已保存到相册/Pictures/eoms，并可在图库中查看", "确定");
        }
#else
            string eomsPath = Path.Combine(basePath, "eoms");
            if (!Directory.Exists(eomsPath))
            {
                Directory.CreateDirectory(eomsPath);
            }
            string destPath = Path.Combine(eomsPath, fileName);
            File.Copy(selectedImage, destPath, true);
            selectedImagePath = destPath;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PreviewImage.Source = ImageSource.FromFile(selectedImagePath);
            });
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"处理图片时出错: {ex.Message}", "确定");
            throw;
        }
    }

    private async void OnCopyClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(generatedText))
        {
            await DisplayAlert("提示", "请先生成信息", "确定");
            return;
        }

        await Clipboard.SetTextAsync(generatedText);
        await DisplayAlert("成功", "文本已复制到剪贴板", "确定");
    }

    private async void OnSendEmailClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(generatedText) || string.IsNullOrEmpty(selectedImagePath))
        {
            await DisplayAlert("提示", "请先生成信息", "确定");
            return;
        }

        try
        {
            // 显示加载动画
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            await SendEmail();
            await DisplayAlert("成功", "邮件已发送", "确定");
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"邮件发送失败: {ex.Message}", "确定");
        }
        finally
        {
            // 隐藏加载动画
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async Task SendEmail()
    {
        try
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "emailsettings.json");
            if (!File.Exists(filePath))
            {
                await DisplayAlert("错误", "请先在设置中配置邮件信息", "确定");
                return;
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            var settings = JsonSerializer.Deserialize<EmailSettings>(jsonString);

            using var message = new MailMessage
            {
                From = new MailAddress(settings.SenderEmail),
                Subject = "请老师帮忙消除PON口告警，谢谢！",
                Body = generatedText,
                IsBodyHtml = false
            };

            message.To.Add(settings.RecipientEmail);
            if (!string.IsNullOrEmpty(settings.CcEmail))
            {
                message.CC.Add(settings.CcEmail);
            }

            if (File.Exists(selectedImagePath))
            {
                message.Attachments.Add(new Attachment(selectedImagePath));
            }

            using var client = new SmtpClient
            {
                Host = "smtp.163.com",
                Port = 25,
                EnableSsl = true,
                Credentials = new NetworkCredential(settings.SenderEmail, settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 40000
            };

            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"发送邮件失败: {ex.Message}", "确定");
            throw;
        }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new shezhi());
    }

    public class EmailSettings
    {
        public string SenderEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string CcEmail { get; set; } = string.Empty;
    }
    private async Task HandleFolderOperation(string name, string folder, Button button, Label status)
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory; // Define basePath here
        string folderPath = Path.Combine(basePath, folder);

        if (!Directory.Exists(folderPath))
        {
            try
            {
                Directory.CreateDirectory(folderPath);
                await CheckFolderStatus(name, folder, button, status);
                await DisplayAlert("成功", $"{name}文件夹创建成功", "确定");
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"创建文件夹失败: {ex.Message}", "确定");
            }
        }
        else
        {
            try
            {
                var result = await FilePicker.PickMultipleAsync(new PickOptions
                {
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    string lastImagePath = null;
                    foreach (var file in result)
                    {
                        string destinationPath = Path.Combine(folderPath, file.FileName);
                        using Stream sourceStream = await file.OpenReadAsync();
                        using FileStream destinationStream = File.Create(destinationPath);
                        await sourceStream.CopyToAsync(destinationStream);
                        lastImagePath = destinationPath; // 记录最后一张图片的路径
                    }
                    await DisplayAlert("成功", $"已上传 {result.Count()} 张照片", "确定");

                    // 显示最后一张图片
                    if (lastImagePath != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            PreviewImage.Source = ImageSource.FromFile(lastImagePath);
                            // Ensure that the "PreviewImage" control is defined in your XAML file and properly named.  
                            // If it is missing, you need to add it to the XAML file.  

                            // Example XAML addition:  
                            // <Image x:Name="PreviewImage" WidthRequest="200" HeightRequest="200" />  

                            // If the control is already defined in the XAML file, ensure that the "x:Name" matches the name used in the code.
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"上传照片失败: {ex.Message}", "确定");
            }
        }
    }

    private async Task CheckFolderStatus(string name, string folder, Button button, Label status)
    {
        try
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string folderPath = Path.Combine(basePath, folder);

            if (Directory.Exists(folderPath))
            {
                status.Text = $"{name} 文件夹已存在";
                button.IsEnabled = true;
            }
            else
            {
                status.Text = $"{name} 文件夹不存在";
                button.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"检查文件夹状态时出错: {ex.Message}", "确定");
        }
    }

}