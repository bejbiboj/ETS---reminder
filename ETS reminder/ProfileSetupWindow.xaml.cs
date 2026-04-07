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
    private string _selectedAvatarId = AvatarCatalog.InitialsId;
    private readonly UserProfile? _existingProfile;

    public ProfileSetupWindow(UserProfile? existingProfile = null)
    {
        InitializeComponent();
        Icon = App.GetWindowIcon();
        _existingProfile = existingProfile;

        PopulateColors();
        PopulateAvatars();

        if (_existingProfile != null)
        {
            Title = "Edit Profile";
            SaveButton.Content = "Save Profile";
            NameTextBox.Text = _existingProfile.DisplayName;
            EmailTextBox.Text = _existingProfile.Email;
            RoleTextBox.Text = _existingProfile.Role;
            _selectedColor = _existingProfile.AvatarColor;
            _selectedAvatarId = _existingProfile.ActiveAvatarId ?? AvatarCatalog.InitialsId;
            UpdateAvatarPreview();
            HighlightSelectedColor();
            HighlightSelectedAvatar();
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

        if (_selectedAvatarId == AvatarCatalog.InitialsId)
        {
            var name = NameTextBox.Text.Trim();
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            AvatarInitials.Text = parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpper(),
                _ => $"{parts[0][..1]}{parts[^1][..1]}".ToUpper()
            };
            AvatarInitials.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            AvatarInitials.FontSize = 28;
        }
        else
        {
            var avatar = AvatarCatalog.GetById(_selectedAvatarId);
            if (avatar != null)
            {
                AvatarInitials.Text = avatar.Symbol;
                AvatarInitials.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji");
                AvatarInitials.FontSize = 26;
            }
        }
    }

    private void PopulateAvatars()
    {
        var profile = _existingProfile;
        var unlocked = profile?.UnlockedAvatars ?? [];

        // Add "Initials" button first
        var initialsBtn = new System.Windows.Controls.Button
        {
            Content = "AB",
            Tag = AvatarCatalog.InitialsId,
            Width = 36, Height = 36,
            FontSize = 12, FontWeight = System.Windows.FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
            BorderThickness = new System.Windows.Thickness(2),
            BorderBrush = System.Windows.Media.Brushes.Transparent,
            Cursor = System.Windows.Input.Cursors.Hand,
            Margin = new System.Windows.Thickness(3),
            ToolTip = "Initials (default)"
        };
        initialsBtn.Click += AvatarButton_Click;
        AvatarPanel.Children.Add(initialsBtn);

        foreach (var avatar in AvatarCatalog.Avatars)
        {
            var isOwned = avatar.Price == 0 || unlocked.Contains(avatar.Id);
            var btn = new System.Windows.Controls.Button
            {
                Content = isOwned ? avatar.Symbol : "\U0001F512",
                Tag = avatar.Id,
                Width = 36, Height = 36,
                FontSize = 18,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji"),
                Background = new SolidColorBrush(Color.FromRgb(0x3E, 0x3E, 0x42)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new System.Windows.Thickness(2),
                BorderBrush = System.Windows.Media.Brushes.Transparent,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new System.Windows.Thickness(3),
                ToolTip = isOwned ? avatar.Name : $"\U0001F512 {avatar.Name} — {avatar.Price} coins (click to buy)",
                Opacity = isOwned ? 1.0 : 0.6
            };
            btn.Click += AvatarButton_Click;
            AvatarPanel.Children.Add(btn);
        }
    }

    private void AvatarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string avatarId)
        {
            // If it's a locked premium avatar, offer to buy
            if (avatarId != AvatarCatalog.InitialsId)
            {
                var avatar = AvatarCatalog.GetById(avatarId);
                if (avatar != null && avatar.Price > 0)
                {
                    var profile = _existingProfile ?? UserProfile.Instance;
                    var unlocked = profile?.UnlockedAvatars ?? [];
                    if (!unlocked.Contains(avatarId))
                    {
                        var result = System.Windows.MessageBox.Show(
                            $"Buy \"{avatar.Name}\" avatar for {avatar.Price} coins?",
                            "Buy Avatar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes) return;

                        if (profile == null || !profile.SpendCoins(avatar.Price))
                        {
                            System.Windows.MessageBox.Show("Not enough coins!",
                                "Insufficient Coins", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        profile.UnlockedAvatars.Add(avatarId);
                        profile.Save();

                        // Refresh the panel to show unlocked
                        AvatarPanel.Children.Clear();
                        PopulateAvatars();
                    }
                }
            }

            _selectedAvatarId = avatarId;
            UpdateAvatarPreview();
            HighlightSelectedAvatar();
        }
    }

    private void HighlightSelectedAvatar()
    {
        foreach (System.Windows.Controls.Button btn in AvatarPanel.Children)
        {
            btn.BorderBrush = (btn.Tag as string) == _selectedAvatarId
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"))
                : System.Windows.Media.Brushes.Transparent;
        }
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
        profile.ActiveAvatarId = _selectedAvatarId;
        profile.Save();

        DialogResult = true;
        Close();
    }
}
