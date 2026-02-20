using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace AMBATU_LAUNCH.Helpers
{
    public static class IconHelper
    {
        public static async Task<BitmapImage> GetIconFromPath(string path)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                
                if (thumbnail != null)
                {
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(thumbnail);
                    return bitmapImage;
                }
            }
            catch (Exception)
            {
                // Fallback
            }
            return new BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png"));
        }
    }
}
