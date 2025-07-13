using ClosedXML.Excel;
using Microsoft.Maui.Controls.PlatformConfiguration;
using System.Net.Mail;
using System.Net;
using System.Text.Json;
namespace EOMS2;

public partial class oldEOMS : ContentPage
{
    private string generatedText = string.Empty;
    private string excelFilePath = string.Empty;

    public oldEOMS()
    {
        InitializeComponent();
        WorkOrderEntry.TextChanged += OnTextChanged;
        OriginalPONEntry.TextChanged += OnTextChanged;
        CommunityNameEntry.TextChanged += OnTextChanged;
        CommunityCodeEntry.TextChanged += OnTextChanged;
        NewPONEntry.TextChanged += OnTextChanged;
        VLANEntry.TextChanged += OnTextChanged;
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

    private async void OnGenerateClicked(object sender, EventArgs e)
    {
        // 显示加载动画
            GenerateButton.Text = "";
            GenerateLoadingIndicator.IsVisible = true;
            GenerateLoadingIndicator.IsRunning = true;
            GenerateButton.IsEnabled = false;

        try
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(WorkOrderEntry.Text))
            {
                throw new Exception("请输入工单编号");
            }

            if (string.IsNullOrWhiteSpace(OriginalPONEntry.Text))
            {
                throw new Exception("请输入原PON口");
            }

            // 生成文本信息
            generatedText = $"{OriginalPONEntry.Text}，该PON口业务已调整，目前无业务，告警无法消除，请老师清除该告警，谢谢！。EOMS工单号：{WorkOrderEntry.Text}";
            GeneratedTextLabel.Text = generatedText;

            // 生成Excel文件
            await GenerateExcelFile();
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", ex.Message, "确定");
        }
        finally
        {
            // 恢复按钮状态
            GenerateButton.Text = "生成";
            GenerateLoadingIndicator.IsVisible = false;
            GenerateLoadingIndicator.IsRunning = false;
            GenerateButton.IsEnabled = true;
        }
    }

    private async void OnSendEmailClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(generatedText) || string.IsNullOrEmpty(excelFilePath))
        {
            await DisplayAlert("提示", "请先生成信息和Excel文件", "确定");
            return;
        }

        // 显示加载动画
        SendEmailButton.Text = "";
        EmailLoadingIndicator.IsVisible = true;
        EmailLoadingIndicator.IsRunning = true;
        SendEmailButton.IsEnabled = false;

        try
        {
           
            await SendEmail();
            await DisplayAlert("成功", "邮件已发送", "确定");
        }
        catch (Exception ex)
        {
            // 错误处理已在 SendEmail 方法中完成
        }
        finally
        {
            // 恢复按钮状态
            SendEmailButton.Text = "发送邮件";
            EmailLoadingIndicator.IsVisible = false;
            EmailLoadingIndicator.IsRunning = false;
            SendEmailButton.IsEnabled = true;
        }
    }

    private async Task GenerateExcelFile()
    {
        try
        {
            string fileName = string.IsNullOrWhiteSpace(CommunityNameEntry.Text) ?
                $"PON口割接-{WorkOrderEntry.Text}.xlsx" :
                $"{CommunityNameEntry.Text}-{WorkOrderEntry.Text}.xlsx";

            string folderPath;
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("提示", "需要存储权限才能保存文件", "确定");
                    return;
                }

                folderPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath,
                    Android.OS.Environment.DirectoryDownloads);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
            }
            else
            {
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }

            excelFilePath = Path.Combine(folderPath, fileName);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("割接信息");

                worksheet.Cell(1, 1).Value = "割接前";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 7).Value = "割接后";
                worksheet.Cell(1, 7).Style.Font.Bold = true;

                string[] headers = new string[] { "地市", "小区编码", "小区名称", "OLTPON口名称", "VLAN", "地市", "小区编码", "小区名称", "OLTPON口名称", "VLAN" };
                for (int i = 0; i < 5; i++)
                {
                    worksheet.Cell(2, i + 1).Value = headers[i];
                    worksheet.Cell(2, i + 6).Value = headers[i + 5];
                }

                worksheet.Cell(3, 1).Value = "许昌市";
                worksheet.Cell(3, 2).Value = CommunityCodeEntry.Text;
                worksheet.Cell(3, 3).Value = CommunityNameEntry.Text;
                worksheet.Cell(3, 4).Value = OriginalPONEntry.Text;
                worksheet.Cell(3, 5).Value = VLANEntry.Text;

                worksheet.Cell(3, 6).Value = "许昌市";
                worksheet.Cell(3, 7).Value = CommunityCodeEntry.Text;
                worksheet.Cell(3, 8).Value = CommunityNameEntry.Text;
                worksheet.Cell(3, 9).Value = NewPONEntry.Text;
                worksheet.Cell(3, 10).Value = VLANEntry.Text;

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(excelFilePath);
            }

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await DisplayAlert("成功", $"文件已保存到Downloads文件夹：\n{fileName}", "确定");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"Excel生成失败: {ex.Message}", "确定");
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

    private async void OnSettingsClicked(object sender, EventArgs e)//设置按钮点击事件
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
    private async Task SendEmail()
    {
        try
        {
            // 读取邮件设置
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "emailsettings.json");
            if (!File.Exists(filePath))
            {
                await DisplayAlert("错误", "请先在设置中配置邮件信息", "确定");
                return;
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            var settings = JsonSerializer.Deserialize<EmailSettings>(jsonString);


            // 邮件配置
            string smtpServer = "smtp.163.com";
            int smtpPort = 25; // 使用SSL端口
            string senderEmail = settings.SenderEmail;
            string password = settings.Password;
            string recipientEmail = settings.RecipientEmail;
            string ccEmail = settings.CcEmail;
            string subject = "请老师帮忙消除PON口告警，谢谢！";
            string body = generatedText;


            using (var message = new MailMessage())
            {
                message.From = new MailAddress(senderEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = false;

                message.To.Add(recipientEmail);
                if (!string.IsNullOrEmpty(ccEmail))
                {
                    message.CC.Add(ccEmail);
                }

                // 添加Excel附件
                if (File.Exists(excelFilePath))
                {
                    var attachment = new Attachment(excelFilePath);
                    message.Attachments.Add(attachment);
                }

                // 配置SMTP客户端
                using (var client = new SmtpClient())
                {
                    // 基本设置
                    client.Host = smtpServer;
                    client.Port = smtpPort;
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(senderEmail, password);

                    // 设置超时时间
                    client.Timeout = 40000; // 30秒

                    // 禁用证书验证（如果需要）
                    ServicePointManager.ServerCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;

                    // 发送邮件
                    await client.SendMailAsync(message);
                }
            }
        }
        catch (SmtpException smtpEx)
        {
            await DisplayAlert("SMTP错误",
                $"邮件发送失败：\n{smtpEx.Message}\n\n" +
                $"状态码：{smtpEx.StatusCode}\n" +
                $"错误详情：{smtpEx.InnerException?.Message ?? "无"}",
                "确定");
            throw;
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误",
                $"邮件发送失败：\n{ex.Message}\n\n" +
                "请检查：\n" +
                "1. 网络连接是否正常\n" +
                "2. 邮箱和授权码是否正确\n" +
                "3. 邮箱是否开启SMTP服务",
                "确定");
            throw;
        }
    }
}