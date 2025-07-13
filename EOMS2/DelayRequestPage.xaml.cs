using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace EOMS2
{
    public partial class DelayRequestPage : ContentPage
    {
        // 添加页面状态标志，避免重复加载
        private bool _isConfigLoaded = false;

        public DelayRequestPage()
        {
            InitializeComponent();

            // 使用页面出现事件而不是构造函数来加载配置
            this.Appearing += OnPageAppearing;
        }

        private void OnPageAppearing(object sender, EventArgs e)
        {
            // 仅在首次加载时执行配置加载
            if (!_isConfigLoaded)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // 延迟加载配置，让UI先渲染完成
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
                    DisplayAlert("提示", "请输入处理人和电话号码", "确定");
                    return;
                }

                string handler = HandlerEntry.Text?.Trim() ?? "";
                string handlerPhone = HandlerPhoneEntry.Text?.Trim() ?? "";

                // 修改日期格式化方式，避免使用ToString可能导致的格式错误
                string recoveryDate = $"{RecoveryDatePicker.Date.Month}月{RecoveryDatePicker.Date.Day}日";
                string recoveryTime = $"{RecoveryTimePicker.Time.Hours:D2}:{RecoveryTimePicker.Time.Minutes:D2}";

                // 使用简单字符串拼接而非复杂的格式化
                string result = "市政施工造成光缆故障，预计恢复时间：" + recoveryDate + " " +
                                recoveryTime + "，请老师给予审核。现场处理人员：" + handler + handlerPhone + "。";

                PreviewLabel.Text = result;
                DisplayAlert("提示", "已生成，请点击复制按钮", "确定");
            }
            catch (Exception ex)
            {
                // 显示详细错误信息以便调试
                DisplayAlert("错误", $"生成文本时出现错误: {ex.Message}", "确定");
            }
        }

        // 添加缺失的复制方法
        private async void OnCopyClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(PreviewLabel.Text) ||
                    PreviewLabel.Text == "生成的消息将在此显示...")
                {
                    await DisplayAlert("提示", "请先生成文本", "确定");
                    return;
                }

                await Clipboard.SetTextAsync(PreviewLabel.Text);
                await DisplayAlert("成功", "文本已复制到剪贴板", "确定");
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"复制文本时出现错误: {ex.Message}", "确定");
            }
        }

        // 添加缺失的保存配置方法
        private async void OnSaveConfigClicked(object sender, EventArgs e)
        {
            try
            {
                // 保存基本信息
                Preferences.Set("Handler", HandlerEntry.Text?.Trim() ?? "");
                Preferences.Set("HandlerPhone", HandlerPhoneEntry.Text?.Trim() ?? "");
                Preferences.Set("Reason", ReasonEditor.Text?.Trim() ?? "");

                // 保存日期和时间（使用固定格式）
                Preferences.Set("RecoveryDate", RecoveryDatePicker.Date.ToString("yyyy-MM-dd"));
                Preferences.Set("RecoveryTime", RecoveryTimePicker.Time.ToString(@"hh\:mm\:ss"));

                await DisplayAlert("成功", "配置已保存", "确定");
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"保存配置时出现错误: {ex.Message}", "确定");
            }
        }

        private async Task LoadConfigAsync()
        {
            try
            {
                // 使用批量更新UI，减少UI刷新次数
                BatchBegin();

                HandlerEntry.Text = Preferences.Get("Handler", string.Empty);
                HandlerPhoneEntry.Text = Preferences.Get("HandlerPhone", string.Empty);
                ReasonEditor.Text = Preferences.Get("Reason", string.Empty);

                // 加载日期
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

                // 加载时间
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
                await DisplayAlert("错误", $"加载配置时出现错误: {ex.Message}", "确定");
            }
        }
    }
}