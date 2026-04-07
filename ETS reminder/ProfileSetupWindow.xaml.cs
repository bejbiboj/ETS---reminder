using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace ETS_reminder;

public partial class ProfileSetupWindow : Window
{
    private static readonly string[] AvatarColors =
    [
        "#E67E22", "#E74C3C", "#9B59B6", "#3498DB",
        "#1ABC9C", "#2ECC71", "#F39C12", "#E91E63",
        "#00BCD4", "#8BC34A", "#FF5722", "#607D8B"
    ];

    private string _selectedColor = "#E67E22";
    private readonly UserProfile? _existingProfile;

    public ProfileSetupWindow(UserProfile? existingProfile = null)
    {
        InitializeComponent();
        Icon = App.GetWindowIcon();
        _existingProfile = existingProfile;

        PopulateColors();

        if (_existingProfile != null)
        {
            Title = "Edit Profile";
            SaveButton.Content = "Save Profile";
            NameTextBox.Text = _existingProfile.DisplayName;
            EmailTextBox.Text = _existingProfile.Email;
            RoleTextBox.Text = _existingProfile.Role;
            _selectedColor = _existingProfile.AvatarColor;
            UpdateAvatarPreview();
            HighlightSelectedColor();
        }

        NameTextBox.Focus();
    }

    private void PopulateColors()
    {
        foreach (var color in AvatarColors)
        {
            var btn = new System.Windows.Controls.Button
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                Tag = color,
                Style = (Style)FindResource("ColorButton")
            };
            btn.Click += ColorButton_Click;
            ColorPanel.Children.Add(btn);
        }
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string color)
        {
            _selectedColor = color;
            UpdateAvatarPreview();
            HighlightSelectedColor();
        }
    }

    private void HighlightSelectedColor()
    {
        foreach (System.Windows.Controls.Button btn in ColorPanel.Children)
        {
            btn.BorderBrush = (btn.Tag as string) == _selectedColor
                ? System.Windows.Media.Brushes.White
                : System.Windows.Media.Brushes.Transparent;
        }
    }

    private void NameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateAvatarPreview();
    }

    private void UpdateAvatarPreview()
    {
        AvatarPreview.Background = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(_selectedColor));

        var name = NameTextBox.Text.Trim();
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        AvatarInitials.Text = parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpper(),
            _ => $"{parts[0][..1]}{parts[^1][..1]}".ToUpper()
        };
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            System.Windows.MessageBox.Show("Please enter your name.",
                "Name Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        var profile = _existingProfile ?? new UserProfile();
        profile.DisplayName = name;
        profile.Email = EmailTextBox.Text.Trim();
        profile.Role = RoleTextBox.Text.Trim();
        profile.AvatarColor = _selectedColor;
        profile.Save();

        DialogResult = true;
        Close();
    }
}
