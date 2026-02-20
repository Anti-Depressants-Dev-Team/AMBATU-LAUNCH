#nullable enable
using Microsoft.UI.Xaml.Media;

namespace AMBATU_LAUNCH.Models
{
    public class AppItem
    {
        public string Name { get; set; } = string.Empty;
        public ImageSource? Icon { get; set; }
        public string ExecutablePath { get; set; } = string.Empty;
        public string Category { get; set; } = "Home";
    }
}
