using Android.Content;
using Android.Provider;
using Android.App;
using System.IO;
using Android.Widget;

namespace EOMS2;

public static class AndroidFileHelper
{
    public static string SaveImageToPictures(string sourceImagePath, string fileName)
    {

        var context = Android.App.Application.Context;
        var contentResolver = context.ContentResolver;

        var values = new ContentValues();
        values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
        values.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");
        values.Put(MediaStore.IMediaColumns.RelativePath, "Pictures/eoms");

        var uri = contentResolver.Insert(MediaStore.Images.Media.ExternalContentUri, values);

        using (var input = File.OpenRead(sourceImagePath))
        using (var output = contentResolver.OpenOutputStream(uri))
        {
            input.CopyTo(output);
        }

        // 获取实际保存路径（可选）
        string savedPath = GetFilePathFromUri(context, uri);
        return savedPath ?? fileName;
    }

    public static void OpenFolder(string folderPath)
    {
        var context = Android.App.Application.Context;
        Java.IO.File file = new Java.IO.File(folderPath);
        if (!file.Exists())
            return;

        var uri = FileProvider.GetUriForFile(context, context.PackageName + ".fileprovider", file);
        Intent intent = new Intent(Intent.ActionView);
        // {{ edit_1 }}
        intent.SetDataAndType(uri, "*/*"); // 使用通用 MIME 类型
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);

        try
        {
            context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            // 提供用户反馈
            Toast.MakeText(context, "无法打开文件夹: " + ex.Message, ToastLength.Long).Show();
        }
    }

    private static string GetFilePathFromUri(Context context, Android.Net.Uri uri)
    {
        string[] projection = { MediaStore.Images.Media.InterfaceConsts.Data };
        using (var cursor = context.ContentResolver.Query(uri, projection, null, null, null))
        {
            if (cursor != null && cursor.MoveToFirst())
            {
                int columnIndex = cursor.GetColumnIndex(projection[0]);
                if (columnIndex >= 0)
                {
                    return cursor.GetString(columnIndex);
                }
            }
        }
        return null;
    }
}