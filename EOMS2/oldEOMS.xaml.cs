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
        // ��ʾ���ض���
            GenerateButton.Text = "";
            GenerateLoadingIndicator.IsVisible = true;
            GenerateLoadingIndicator.IsRunning = true;
            GenerateButton.IsEnabled = false;

        try
        {
            // ��֤�����ֶ�
            if (string.IsNullOrWhiteSpace(WorkOrderEntry.Text))
            {
                throw new Exception("�����빤�����");
            }

            if (string.IsNullOrWhiteSpace(OriginalPONEntry.Text))
            {
                throw new Exception("������ԭPON��");
            }

            // �����ı���Ϣ
            generatedText = $"{OriginalPONEntry.Text}����PON��ҵ���ѵ�����Ŀǰ��ҵ�񣬸澯�޷�����������ʦ����ø澯��лл����EOMS�����ţ�{WorkOrderEntry.Text}";
            GeneratedTextLabel.Text = generatedText;

            // ����Excel�ļ�
            await GenerateExcelFile();
        }
        catch (Exception ex)
        {
            await DisplayAlert("����", ex.Message, "ȷ��");
        }
        finally
        {
            // �ָ���ť״̬
            GenerateButton.Text = "����";
            GenerateLoadingIndicator.IsVisible = false;
            GenerateLoadingIndicator.IsRunning = false;
            GenerateButton.IsEnabled = true;
        }
    }

    private async void OnSendEmailClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(generatedText) || string.IsNullOrEmpty(excelFilePath))
        {
            await DisplayAlert("��ʾ", "����������Ϣ��Excel�ļ�", "ȷ��");
            return;
        }

        // ��ʾ���ض���
        SendEmailButton.Text = "";
        EmailLoadingIndicator.IsVisible = true;
        EmailLoadingIndicator.IsRunning = true;
        SendEmailButton.IsEnabled = false;

        try
        {
           
            await SendEmail();
            await DisplayAlert("�ɹ�", "�ʼ��ѷ���", "ȷ��");
        }
        catch (Exception ex)
        {
            // ���������� SendEmail ���������
        }
        finally
        {
            // �ָ���ť״̬
            SendEmailButton.Text = "�����ʼ�";
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
                $"PON�ڸ��-{WorkOrderEntry.Text}.xlsx" :
                $"{CommunityNameEntry.Text}-{WorkOrderEntry.Text}.xlsx";

            string folderPath;
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("��ʾ", "��Ҫ�洢Ȩ�޲��ܱ����ļ�", "ȷ��");
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
                var worksheet = workbook.Worksheets.Add("�����Ϣ");

                worksheet.Cell(1, 1).Value = "���ǰ";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 7).Value = "��Ӻ�";
                worksheet.Cell(1, 7).Style.Font.Bold = true;

                string[] headers = new string[] { "����", "С������", "С������", "OLTPON������", "VLAN", "����", "С������", "С������", "OLTPON������", "VLAN" };
                for (int i = 0; i < 5; i++)
                {
                    worksheet.Cell(2, i + 1).Value = headers[i];
                    worksheet.Cell(2, i + 6).Value = headers[i + 5];
                }

                worksheet.Cell(3, 1).Value = "�����";
                worksheet.Cell(3, 2).Value = CommunityCodeEntry.Text;
                worksheet.Cell(3, 3).Value = CommunityNameEntry.Text;
                worksheet.Cell(3, 4).Value = OriginalPONEntry.Text;
                worksheet.Cell(3, 5).Value = VLANEntry.Text;

                worksheet.Cell(3, 6).Value = "�����";
                worksheet.Cell(3, 7).Value = CommunityCodeEntry.Text;
                worksheet.Cell(3, 8).Value = CommunityNameEntry.Text;
                worksheet.Cell(3, 9).Value = NewPONEntry.Text;
                worksheet.Cell(3, 10).Value = VLANEntry.Text;

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(excelFilePath);
            }

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await DisplayAlert("�ɹ�", $"�ļ��ѱ��浽Downloads�ļ��У�\n{fileName}", "ȷ��");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("����", $"Excel����ʧ��: {ex.Message}", "ȷ��");
            throw;
        }
    }

    private async void OnCopyClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(generatedText))
        {
            await DisplayAlert("��ʾ", "����������Ϣ", "ȷ��");
            return;
        }

        await Clipboard.SetTextAsync(generatedText);
        await DisplayAlert("�ɹ�", "�ı��Ѹ��Ƶ�������", "ȷ��");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)//���ð�ť����¼�
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
            // ��ȡ�ʼ�����
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "emailsettings.json");
            if (!File.Exists(filePath))
            {
                await DisplayAlert("����", "�����������������ʼ���Ϣ", "ȷ��");
                return;
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            var settings = JsonSerializer.Deserialize<EmailSettings>(jsonString);


            // �ʼ�����
            string smtpServer = "smtp.163.com";
            int smtpPort = 25; // ʹ��SSL�˿�
            string senderEmail = settings.SenderEmail;
            string password = settings.Password;
            string recipientEmail = settings.RecipientEmail;
            string ccEmail = settings.CcEmail;
            string subject = "����ʦ��æ����PON�ڸ澯��лл��";
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

                // ���Excel����
                if (File.Exists(excelFilePath))
                {
                    var attachment = new Attachment(excelFilePath);
                    message.Attachments.Add(attachment);
                }

                // ����SMTP�ͻ���
                using (var client = new SmtpClient())
                {
                    // ��������
                    client.Host = smtpServer;
                    client.Port = smtpPort;
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(senderEmail, password);

                    // ���ó�ʱʱ��
                    client.Timeout = 40000; // 30��

                    // ����֤����֤�������Ҫ��
                    ServicePointManager.ServerCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;

                    // �����ʼ�
                    await client.SendMailAsync(message);
                }
            }
        }
        catch (SmtpException smtpEx)
        {
            await DisplayAlert("SMTP����",
                $"�ʼ�����ʧ�ܣ�\n{smtpEx.Message}\n\n" +
                $"״̬�룺{smtpEx.StatusCode}\n" +
                $"�������飺{smtpEx.InnerException?.Message ?? "��"}",
                "ȷ��");
            throw;
        }
        catch (Exception ex)
        {
            await DisplayAlert("����",
                $"�ʼ�����ʧ�ܣ�\n{ex.Message}\n\n" +
                "���飺\n" +
                "1. ���������Ƿ�����\n" +
                "2. �������Ȩ���Ƿ���ȷ\n" +
                "3. �����Ƿ���SMTP����",
                "ȷ��");
            throw;
        }
    }
}