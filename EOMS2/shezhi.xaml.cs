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

            await DisplayAlert("�ɹ�", "�ʼ������ѱ���", "ȷ��");
        }
        catch (Exception ex)
        {
            await DisplayAlert("����", $"��������ʧ��: {ex.Message}", "ȷ��");
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
            DisplayAlert("����", $"��������ʧ��: {ex.Message}", "ȷ��");
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