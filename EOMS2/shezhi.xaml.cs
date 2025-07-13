using System.Text.Json;

namespace EOMS2;

public partial class shezhi : ContentPage
{
    private const string SETTINGS_FILE = "emailsettings.json";
    public shezhi()
    {
        InitializeComponent();
        LoadSettings();
    }
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            var settings = new EmailSettings
            {
                SenderEmail = SenderEmailEntry.Text,
                Password = PasswordEntry.Text,
                RecipientEmail = RecipientEmailEntry.Text,
                CcEmail = CcEmailEntry.Text
            };

            string jsonString = JsonSerializer.Serialize(settings);
            string filePath = Path.Combine(FileSystem.AppDataDirectory, SETTINGS_FILE);
            await File.WriteAllTextAsync(filePath, jsonString);

            await DisplayAlert("成功", "邮件设置已保存", "确定");
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"保存设置失败: {ex.Message}", "确定");
        }
    }

    private void LoadSettings()
    {
        try
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, SETTINGS_FILE);
            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<EmailSettings>(jsonString);

                SenderEmailEntry.Text = settings.SenderEmail;
                PasswordEntry.Text = settings.Password;
                RecipientEmailEntry.Text = settings.RecipientEmail;
                CcEmailEntry.Text = settings.CcEmail;
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("错误", $"加载设置失败: {ex.Message}", "确定");
        }
    }
    public class EmailSettings
    {
        public string SenderEmail { get; set; }
        public string Password { get; set; }
        public string RecipientEmail { get; set; }
        public string CcEmail { get; set; }
    }
}