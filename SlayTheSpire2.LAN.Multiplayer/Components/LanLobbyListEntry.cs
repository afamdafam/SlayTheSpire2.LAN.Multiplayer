using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SlayTheSpire2.LAN.Multiplayer.Models;

namespace SlayTheSpire2.LAN.Multiplayer.Components
{
    internal partial class LanLobbyListEntry : PanelContainer
    {
        public event Action? Selected;

        public LanLobbyListEntry(LanLobbyInfo lobbyInfo)
        {
            MouseFilter = MouseFilterEnum.Stop;
            CustomMinimumSize = new Vector2(300, 36);

            var styleBox = new StyleBoxTexture
            {
                Texture = GD.Load<CompressedTexture2D>("res://images/ui/tiny_nine_patch.png"),
                TextureMarginLeft = 8, TextureMarginTop = 8, TextureMarginRight = 8, TextureMarginBottom = 8,
                ContentMarginLeft = 8, ContentMarginTop = 4, ContentMarginRight = 8, ContentMarginBottom = 4,
                ModulateColor = new Color(Colors.Black, 0.4f)
            };
            AddThemeStyleboxOverride("panel", styleBox);

            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            this.AddChildSafely(row);

            var nameLabel = new MegaLabel
                { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            nameLabel.SetTextAutoSize($"{lobbyInfo.HostName} ({lobbyInfo.Mode})");
            row.AddChildSafely(nameLabel);

            var addressLabel = new MegaLabel { MouseFilter = MouseFilterEnum.Ignore };
            addressLabel.SetTextAutoSize($"{lobbyInfo.Address}:{lobbyInfo.Port}");
            row.AddChildSafely(addressLabel);

            GuiInput += inputEvent =>
            {
                if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                {
                    Selected?.Invoke();
                }
            };
        }
    }
}
