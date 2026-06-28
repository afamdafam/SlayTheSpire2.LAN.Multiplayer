using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using SlayTheSpire2.LAN.Multiplayer.Components;
using SlayTheSpire2.LAN.Multiplayer.Services;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace SlayTheSpire2.LAN.Multiplayer.Patchs.Screens
{
    /// <summary>Set to true before pushing NJoinFriendScreen to activate LAN mode.</summary>
    internal static class LanJoinMode
    {
        internal static bool IsActive;
    }

    /// <summary>
    /// Builds the hidden LAN overlay once on screen creation. The overlay is a
    /// Minecraft-style panel: scrollable lobby list on top, direct connect below.
    /// </summary>
    [HarmonyPatch(typeof(NJoinFriendScreen), "_Ready")]
    internal class NJoinFriendScreenReadyPatch
    {
        // Kept as static refs so OnSubmenuOpened can show/populate them
        internal static Control? LanOverlay;
        internal static VBoxContainer? LanLobbyVBox;
        internal static MegaLabel? LanStatusLabel;
        internal static AddressLineEdit? AddressInput;

        private static void Prefix(NJoinFriendScreen __instance)
        {
            var refreshButtonRef = __instance.GetNode<NJoinFriendRefreshButton>("%RefreshButton");

            // Root overlay — passes mouse events in empty areas so BackButton stays clickable
            var overlay = new Control { Name = "LANOverlay", Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
            overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            // Outer VBox — floats directly in the overlay, no border panel
            var outerVBox = new VBoxContainer
            {
                Alignment = BoxContainer.AlignmentMode.Begin,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            outerVBox.AddThemeConstantOverride("separation", 10);
            outerVBox.SetAnchorsPreset(Control.LayoutPreset.Center);
            outerVBox.OffsetLeft = -380;
            outerVBox.OffsetTop = -260;
            outerVBox.OffsetRight = 380;
            outerVBox.OffsetBottom = 310;
            overlay.AddChildSafely(outerVBox);

            // ── Lobby list (entries go directly in the panel) ────────────────
            var scroll = new ScrollContainer
            {
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            outerVBox.AddChildSafely(scroll);

            var lobbyVBox = new VBoxContainer
            {
                Name = "LANLobbyVBox",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            lobbyVBox.AddThemeConstantOverride("separation", 4);
            scroll.AddChildSafely(lobbyVBox);

            // Initial status label inside the lobby box
            var statusLabel = (MegaLabel)__instance.GetNode("TitleLabel").Duplicate();
            statusLabel.Name = "LANStatusLabel";
            statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            statusLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            statusLabel.SetTextAutoSize("Scanning for LAN lobbies...");
            lobbyVBox.AddChildSafely(statusLabel);

            // ── Separator ────────────────────────────────────────────────────
            var sep = new HSeparator
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            outerVBox.AddChildSafely(sep);

            // ── Direct Connect label ─────────────────────────────────────────
            var directLabel = (MegaLabel)__instance.GetNode("TitleLabel").Duplicate();
            directLabel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            directLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            directLabel.SetTextAutoSize("Direct Connect:");
            outerVBox.AddChildSafely(directLabel);

            // ── Address input + Join button (side by side) ───────────────────
            var connectRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            connectRow.AddThemeConstantOverride("separation", 8);
            outerVBox.AddChildSafely(connectRow);

            var addressInput = new AddressLineEdit
            {
                Name = "AddressInput",
                Text = SettingsService.Instance.SettingsModel.IPAddress,
                Alignment = HorizontalAlignment.Center,
                CustomMinimumSize = new Vector2(0, 50),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            connectRow.AddChildSafely(addressInput);

            var joinButton = JoinButton.Create(refreshButtonRef);
            joinButton.Name = "JoinButton";
            joinButton.CustomMinimumSize = new Vector2(140, 50);
            joinButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
            connectRow.AddChildSafely(joinButton);

            joinButton.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ =>
            {
                var info = addressInput.GetAddressInfo();
                if (!info.IsValid || info.Address == null) return;
                DoJoin(__instance, info.Address, info.Port ?? 33771);
            }));

            __instance.AddChildSafely(overlay);

            LanOverlay = overlay;
            LanLobbyVBox = lobbyVBox;
            LanStatusLabel = statusLabel;
            AddressInput = addressInput;
        }

        internal static void DoJoin(NJoinFriendScreen instance, string address, ushort port)
        {
            SettingsService.Instance.SettingsModel.IPAddress = address;
            SettingsService.Instance.WriteSettings();
            DisplayServer.WindowSetTitle("Slay The Spire 2 (Client)");
            TaskHelper.RunSafely(instance.JoinGameAsync(
                new ENetClientConnectionInitializer(
                    SettingsService.Instance.SettingsModel.NetId, address, port)));
        }
    }

    /// <summary>
    /// Switches the screen between LAN mode and Steam mode each time it opens.
    /// </summary>
    [HarmonyPatch(typeof(NJoinFriendScreen), "OnSubmenuOpened")]
    internal class NJoinFriendScreenOnSubmenuOpenedPatch
    {
        internal static CancellationTokenSource? BrowseCts;

        private static bool Prefix(NJoinFriendScreen __instance)
        {
            var traverse = Traverse.Create(__instance);
            var buttonContainer = traverse.Field("_buttonContainer").GetValue<Control>();
            var noFriendsLabel = traverse.Field("_noFriendsLabel").GetValue<MegaLabel>();
            var loadingIndicator = traverse.Field("_loadingFriendsIndicator").GetValue<Control>();
            var loadingOverlay = traverse.Field("_loadingOverlay").GetValue<Control>();
            var refreshButton = traverse.Field("_refreshButton").GetValue<Control>();
            var titleLabel = __instance.GetNode<MegaLabel>("TitleLabel");

            if (!LanJoinMode.IsActive)
            {
                // ── Steam mode: hide LAN overlay, restore screen to normal ──
                if (NJoinFriendScreenReadyPatch.LanOverlay != null)
                    NJoinFriendScreenReadyPatch.LanOverlay.Visible = false;

                buttonContainer.Visible = true;
                refreshButton.Visible = true;
                titleLabel.SetTextAutoSize(
                    new LocString("main_menu_ui", "JOIN_FRIENDS_MENU.title").GetFormattedText());

                return true; // let original Steam scan run
            }

            // ── LAN mode ────────────────────────────────────────────────────
            loadingOverlay.Visible = false;
            buttonContainer.Visible = false;
            noFriendsLabel.Visible = false;
            loadingIndicator.Visible = false;
            refreshButton.Visible = false;
            titleLabel.SetTextAutoSize("LAN Lobby");

            if (NJoinFriendScreenReadyPatch.LanOverlay != null)
                NJoinFriendScreenReadyPatch.LanOverlay.Visible = true;

            // Reset lobby list and show scanning state
            var lobbyVBox = NJoinFriendScreenReadyPatch.LanLobbyVBox;
            var statusLabel = NJoinFriendScreenReadyPatch.LanStatusLabel;

            if (lobbyVBox != null)
            {
                // Free lobby entries but keep the status label alive across visits
                foreach (var child in lobbyVBox.GetChildren())
                    if (child != statusLabel)
                        child.QueueFreeSafely();

                if (statusLabel != null && GodotObject.IsInstanceValid(statusLabel))
                {
                    statusLabel.SetTextAutoSize("Scanning for LAN lobbies...");
                    statusLabel.Visible = true;
                }
            }

            // Start LAN discovery
            BrowseCts?.Cancel();
            BrowseCts = new CancellationTokenSource();
            BrowseCts.CancelAfter(TimeSpan.FromSeconds(8));

            var foundAddresses = new HashSet<string>();
            var token = BrowseCts.Token;

            TaskHelper.RunSafely(LanDiscoveryService.Instance.BrowseAsync(lobbyInfo =>
            {
                var key = $"{lobbyInfo.Address}:{lobbyInfo.Port}";
                if (!foundAddresses.Add(key)) return;

                if (GodotObject.IsInstanceValid(statusLabel))
                    statusLabel!.Visible = false;

                if (lobbyVBox == null || !GodotObject.IsInstanceValid(lobbyVBox)) return;

                var entry = new LanLobbyListEntry(lobbyInfo);
                entry.Selected += () =>
                    NJoinFriendScreenReadyPatch.DoJoin(__instance, lobbyInfo.Address, lobbyInfo.Port);
                lobbyVBox.AddChildSafely(entry);
            }, token));

            return false; // skip Steam scan
        }
    }

    /// <summary>Cancels LAN browsing and resets the mode flag when the screen closes.</summary>
    [HarmonyPatch(typeof(NJoinFriendScreen), "OnSubmenuClosed")]
    internal class NJoinFriendScreenOnSubmenuClosedPatch
    {
        private static void Postfix()
        {
            NJoinFriendScreenOnSubmenuOpenedPatch.BrowseCts?.Cancel();
            NJoinFriendScreenOnSubmenuOpenedPatch.BrowseCts = null;
            LanJoinMode.IsActive = false;
        }
    }
}
