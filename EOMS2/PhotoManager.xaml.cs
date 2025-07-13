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
                { "����", ("huo", FireFolderButton, FireDeleteImageButton, FireFolderStatus) },
                { "�������", ("dian", PowerFolderButton, PowerDeleteImageButton, PowerFolderStatus) },
                { "ʩ��", ("shizheng", ConstructionFolderButton, ConstructionDeleteImageButton, ConstructionFolderStatus) }
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
                actionButton.Text = exists ? $"�ϴ�{name}��Ƭ" : $"����{name}�ļ���";
                status.Text = exists ? $"�ļ����Ѵ���: {folderPath}" : "�ļ��в�����";
                status.TextColor = exists ? Colors.Green : Colors.Orange;

                deleteButton.IsVisible = exists;
                deleteButton.IsEnabled = exists;
            });
        }

        private async void OnFireFolderClicked(object sender, EventArgs e)
        {
            var info = folderInfo["����"];
            await HandleFolderOperation("����", "huo", info.actionButton, info.deleteButton, info.status);
        }

        private async void OnPowerFolderClicked(object sender, EventArgs e)
        {
            var info = folderInfo["�������"];
            await HandleFolderOperation("�������", "dian", info.actionButton, info.deleteButton, info.status);
        }

        private async void OnConstructionFolderClicked(object sender, EventArgs e)
        {
            var info = folderInfo["ʩ��"];
            await HandleFolderOperation("ʩ��", "shizheng", info.actionButton, info.deleteButton, info.status);
        }

        // {{ edit_1 }}
        private async Task HandleFolderOperation(string name, string folder, Button actionButton, Button deleteButton, Label status)
        {
            string folderPath = Path.Combine(basePath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                await DisplayAlert("�ɹ�", $"{name}�ļ����Ѵ���", "ȷ��");
            }

            // ����ϴ��ļ����߼�
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
                    await DisplayAlert("�ɹ�", $"���ϴ� {result.Count()} ����Ƭ", "ȷ��");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("����", $"�ϴ���Ƭʧ��: {ex.Message}", "ȷ��");
            }

            await CheckFolderStatus(name, folder, actionButton, deleteButton, status);
        }

        // ������ɾ��ͼƬ��ť����¼�������
        private async void OnFireDeleteImageClicked(object sender, EventArgs e)
        {
            var info = folderInfo["����"];
            string folderPath = Path.Combine(basePath, info.folder);
            await DeleteImagesFromFolder(folderPath);
        }

        private async void OnPowerDeleteImageClicked(object sender, EventArgs e)
        {
            var info = folderInfo["�������"];
            string folderPath = Path.Combine(basePath, info.folder);
            await DeleteImagesFromFolder(folderPath);
        }

        private async void OnConstructionDeleteImageClicked(object sender, EventArgs e)
        {
            var info = folderInfo["ʩ��"];
            string folderPath = Path.Combine(basePath, info.folder);
            await DeleteImagesFromFolder(folderPath);
        }

        // ����������ɾ���ļ����е�ͼƬ
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
                    await DisplayAlert("��ʾ", "�ļ�����û��ͼƬ��ɾ��", "ȷ��");
                    return;
                }

                bool confirm = await DisplayAlert("ȷ��", $"ȷ��Ҫɾ�� {images.Count} ��ͼƬ��", "��", "��");
                if (confirm)
                {
                    foreach (var image in images)
                    {
                        File.Delete(image);
                    }
                    await DisplayAlert("�ɹ�", "ͼƬ��ɾ��", "ȷ��");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("����", $"ɾ��ͼƬʧ��: {ex.Message}", "ȷ��");
            }
        }
    }
}