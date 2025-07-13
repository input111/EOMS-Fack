using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace EOMS2
{
    public partial class DelayRequestPage : ContentPage
    {
        // ���ҳ��״̬��־�������ظ�����
        private bool _isConfigLoaded = false;

        public DelayRequestPage()
        {
            InitializeComponent();

            // ʹ��ҳ������¼������ǹ��캯������������
            this.Appearing += OnPageAppearing;
        }

        private void OnPageAppearing(object sender, EventArgs e)
        {
            // �����״μ���ʱִ�����ü���
            if (!_isConfigLoaded)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // �ӳټ������ã���UI����Ⱦ���
                    await Task.Delay(200);
                    await LoadConfigAsync();
                    _isConfigLoaded = true;
                });
            }
        }

        private void OnGenerateClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(HandlerEntry.Text) || string.IsNullOrEmpty(HandlerPhoneEntry.Text))
                {
                    DisplayAlert("��ʾ", "�����봦���˺͵绰����", "ȷ��");
                    return;
                }

                string handler = HandlerEntry.Text?.Trim() ?? "";
                string handlerPhone = HandlerPhoneEntry.Text?.Trim() ?? "";

                // �޸����ڸ�ʽ����ʽ������ʹ��ToString���ܵ��µĸ�ʽ����
                string recoveryDate = $"{RecoveryDatePicker.Date.Month}��{RecoveryDatePicker.Date.Day}��";
                string recoveryTime = $"{RecoveryTimePicker.Time.Hours:D2}:{RecoveryTimePicker.Time.Minutes:D2}";

                // ʹ�ü��ַ���ƴ�Ӷ��Ǹ��ӵĸ�ʽ��
                string result = "����ʩ����ɹ��¹��ϣ�Ԥ�ƻָ�ʱ�䣺" + recoveryDate + " " +
                                recoveryTime + "������ʦ������ˡ��ֳ�������Ա��" + handler + handlerPhone + "��";

                PreviewLabel.Text = result;
                DisplayAlert("��ʾ", "�����ɣ��������ư�ť", "ȷ��");
            }
            catch (Exception ex)
            {
                // ��ʾ��ϸ������Ϣ�Ա����
                DisplayAlert("����", $"�����ı�ʱ���ִ���: {ex.Message}", "ȷ��");
            }
        }

        // ���ȱʧ�ĸ��Ʒ���
        private async void OnCopyClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(PreviewLabel.Text) ||
                    PreviewLabel.Text == "���ɵ���Ϣ���ڴ���ʾ...")
                {
                    await DisplayAlert("��ʾ", "���������ı�", "ȷ��");
                    return;
                }

                await Clipboard.SetTextAsync(PreviewLabel.Text);
                await DisplayAlert("�ɹ�", "�ı��Ѹ��Ƶ�������", "ȷ��");
            }
            catch (Exception ex)
            {
                await DisplayAlert("����", $"�����ı�ʱ���ִ���: {ex.Message}", "ȷ��");
            }
        }

        // ���ȱʧ�ı������÷���
        private async void OnSaveConfigClicked(object sender, EventArgs e)
        {
            try
            {
                // ���������Ϣ
                Preferences.Set("Handler", HandlerEntry.Text?.Trim() ?? "");
                Preferences.Set("HandlerPhone", HandlerPhoneEntry.Text?.Trim() ?? "");
                Preferences.Set("Reason", ReasonEditor.Text?.Trim() ?? "");

                // �������ں�ʱ�䣨ʹ�ù̶���ʽ��
                Preferences.Set("RecoveryDate", RecoveryDatePicker.Date.ToString("yyyy-MM-dd"));
                Preferences.Set("RecoveryTime", RecoveryTimePicker.Time.ToString(@"hh\:mm\:ss"));

                await DisplayAlert("�ɹ�", "�����ѱ���", "ȷ��");
            }
            catch (Exception ex)
            {
                await DisplayAlert("����", $"��������ʱ���ִ���: {ex.Message}", "ȷ��");
            }
        }

        private async Task LoadConfigAsync()
        {
            try
            {
                // ʹ����������UI������UIˢ�´���
                BatchBegin();

                HandlerEntry.Text = Preferences.Get("Handler", string.Empty);
                HandlerPhoneEntry.Text = Preferences.Get("HandlerPhone", string.Empty);
                ReasonEditor.Text = Preferences.Get("Reason", string.Empty);

                // ��������
                try
                {
                    string savedDate = Preferences.Get("RecoveryDate", DateTime.Now.ToString("yyyy-MM-dd"));
                    if (DateTime.TryParse(savedDate, out DateTime date))
                    {
                        RecoveryDatePicker.Date = date;
                    }
                    else
                    {
                        RecoveryDatePicker.Date = DateTime.Now;
                    }
                }
                catch
                {
                    RecoveryDatePicker.Date = DateTime.Now;
                }

                // ����ʱ��
                try
                {
                    string savedTime = Preferences.Get("RecoveryTime", DateTime.Now.ToString("HH:mm:ss"));
                    if (TimeSpan.TryParse(savedTime, out TimeSpan time))
                    {
                        RecoveryTimePicker.Time = time;
                    }
                    else
                    {
                        RecoveryTimePicker.Time = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);
                    }
                }
                catch
                {
                    RecoveryTimePicker.Time = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);
                }

                BatchCommit();
            }
            catch (Exception ex)
            {
                await DisplayAlert("����", $"��������ʱ���ִ���: {ex.Message}", "ȷ��");
            }
        }
    }
}