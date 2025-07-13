using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace EOMS2.Views
{
    public partial class PhotoManagerPage : ContentPage
    {

        private readonly string basePath;
        private readonly Dictionary<string, (string folder, Button actionButton, Button deleteButton, Label status)> folderInfo;

        public PhotoManagerPage()
        {
            InitializeComponent();
            basePath = AppDomain.CurrentDomain.BaseDirectory;

            folderInfo = new Dictionary<string, (string, Button, Button, Label)>
            {
                { "火灾", ("huo", FireFolderButton, FireDeleteImageButton, FireFolderStatus) },
                { "美丽乡村", ("dian", PowerFolderButton, PowerDeleteImageButton, PowerFolderStatus) },
                { "施工", ("shizheng", ConstructionFolderButton, ConstructionDeleteImageButton, ConstructionFolderStatus) }
            };

            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, EventArgs e)
        {
            await CheckAllFoldersStatus();
        }

        private async Task CheckAllFoldersStatus()
        {
            foreach (var info in folderInfo)
            {
                await CheckFolderStatus(info.Key, info.Value.folder, info.Value.actionButton, info.Value.deleteButton, info.Value.status);
            }
        }

        private async Task CheckFolderStatus(string name, string folder, Button actionButton, Button deleteButton, Label status)
        {
            string folderPath = Path.Combine(basePath, folder);
            bool exists = Directory.Exists(folderPath);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                actionButton.Text = exists ? $"上传{name}照片" : $"创建{name}文件夹";
                status.Text = exists ? $"文件夹已存在: {folderPath}" : "文件夹不存在";
                status.TextColor = exists ? Colors.Green : Colors.Orange;

                deleteButton.IsVisible = exists;
                deleteButton.IsEnabled = exists;
            });
        }

        private async void OnFireFolderClicked(object sender, EventArgs e)
        {
            var info = folderInfo["火灾"];
            await HandleFolderOperation("火灾", "huo", info.actionButton, info.deleteButton, info.status);
        }

        private async void OnPowerFolderClicked(object sender, EventArgs e)
        {
            var info = folderInfo["美丽乡村"];
            await HandleFolderOperation("美丽乡村", "dian", info.actionButton, info.deleteButton, info.status);
        }

        private async void OnConstructionFolderClicked(object sender, EventArgs e)
        {
            var info = folderInfo["施工"];
            await HandleFolderOperation("施工", "shizheng", info.actionButton, info.deleteButton, info.status);
        }

        // {{ edit_1 }}
        private async Task HandleFolderOperation(string name, string folder, Button actionButton, Button deleteButton, Label status)
        {
            string folderPath = Path.Combine(basePath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                await DisplayAlert("成功", $"{name}文件夹已创建", "确定");
            }

            // 添加上传文件的逻辑
            try
            {
                var result = await FilePicker.PickMultipleAsync(new PickOptions
                {
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    foreach (var file in result)
                    {
                        string destinationPath = Path.Combine(folderPath, file.FileName);
                        using Stream sourceStream = await file.OpenReadAsync();
                        using FileStream destinationStream = File.Create(destinationPath);
                        await sourceStream.CopyToAsync(destinationStream);
                    }
                    await DisplayAlert("成功", $"已上传 {result.Count()} 张照片", "确定");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"上传照片失败: {ex.Message}", "确定");
            }

            await CheckFolderStatus(name, folder, actionButton, deleteButton, status);
        }

        // 新增的删除图片按钮点击事件处理方法
        private async void OnFireDeleteImageClicked(object sender, EventArgs e)
        {
            var info = folderInfo["火灾"];
            string folderPath = Path.Combine(basePath, info.folder);
            await DeleteImagesFromFolder(folderPath);
        }

        private async void OnPowerDeleteImageClicked(object sender, EventArgs e)
        {
            var info = folderInfo["美丽乡村"];
            string folderPath = Path.Combine(basePath, info.folder);
            await DeleteImagesFromFolder(folderPath);
        }

        private async void OnConstructionDeleteImageClicked(object sender, EventArgs e)
        {
            var info = folderInfo["施工"];
            string folderPath = Path.Combine(basePath, info.folder);
            await DeleteImagesFromFolder(folderPath);
        }

        // 辅助方法：删除文件夹中的图片
        private async Task DeleteImagesFromFolder(string folderPath)
        {
            try
            {
                var images = Directory.GetFiles(folderPath, "*.*")
                    .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                   file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                   file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (images.Count == 0)
                {
                    await DisplayAlert("提示", "文件夹中没有图片可删除", "确定");
                    return;
                }

                bool confirm = await DisplayAlert("确认", $"确定要删除 {images.Count} 张图片吗？", "是", "否");
                if (confirm)
                {
                    foreach (var image in images)
                    {
                        File.Delete(image);
                    }
                    await DisplayAlert("成功", "图片已删除", "确定");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"删除图片失败: {ex.Message}", "确定");
            }
        }
    }
}