using System;
using System.Collections.Generic;
using System.Text;
using BerrySdk;
using UnityEngine;
using UnityEngine.UI;

namespace BerryShooter
{
    public sealed partial class GameApp
    {
        private RectTransform mainRoot, roomsRoot, lobbyRoot, hudRoot, pauseRoot, overRoot, dialogRoot, toastRoot, tagsRoot, rightContent, roomsList, berryList, gameList;
        private Text statusText, toastText, roomsStatus, lobbyHeader, lobbySummary, detailText, lobbyHint;
        private Button startButton, inviteButton;
        private int rightTab;
        private string rightSig = "", roomsSig = "", lobbySig = "", selectedBerry = "";
        private int selectedGame = -1;
        private bool roomsAll;
        private float roomsTimer;
        private readonly List<KeyValuePair<string, float>> toasts = new List<KeyValuePair<string, float>>();

        private static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        private RectTransform NewRoot(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private void BuildAllScreens()
        {
            mainRoot = NewRoot("Main");
            roomsRoot = NewRoot("Rooms");
            lobbyRoot = NewRoot("Lobby");
            hudRoot = NewRoot("Hud");
            tagsRoot = NewRoot("Tags");
            pauseRoot = NewRoot("Pause");
            overRoot = NewRoot("Over");
            dialogRoot = NewRoot("Dialog");
            toastRoot = NewRoot("Toast");
            BuildMain();
            BuildRooms();
            BuildLobby();
            BuildHud();
            BuildPause();
            BuildOver();
            var tb = Ui.Panel(toastRoot, "Toast", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-560, -150), new Vector2(560, -20), new Color(0, 0, 0, 0.78f));
            toastText = Ui.Stretch(tb, "", 26, Ui.Ink, TextAnchor.MiddleCenter);
            tb.gameObject.SetActive(false);
        }

        private void ApplyScreens()
        {
            if (mainRoot == null) return;
            mainRoot.gameObject.SetActive(screen == Page.Menu);
            roomsRoot.gameObject.SetActive(screen == Page.Rooms);
            lobbyRoot.gameObject.SetActive(screen == Page.Lobby);
            hudRoot.gameObject.SetActive(screen == Page.Play);
            tagsRoot.gameObject.SetActive(screen == Page.Play);
            overRoot.gameObject.SetActive(screen == Page.Over);
            pauseRoot.gameObject.SetActive(screen == Page.Play && paused);
            if (screen == Page.Over) RefreshOver();
            if (screen == Page.Lobby) { lobbySig = ""; RefreshLobby(); }
            if (screen == Page.Menu) RebuildRight();
        }

        private void ShowToast(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            toasts.Add(new KeyValuePair<string, float>(text, Time.unscaledTime + 4f));
            if (toasts.Count > 3) toasts.RemoveAt(0);
        }

        private void UpdateToasts(float dt)
        {
            if (toastText == null) return;
            toasts.RemoveAll(t => t.Value < Time.unscaledTime);
            var parent = toastText.transform.parent.gameObject;
            parent.SetActive(toasts.Count > 0);
            if (toasts.Count == 0) return;
            var sb = new StringBuilder();
            for (int i = 0; i < toasts.Count; i++) { if (i > 0) sb.Append('\n'); sb.Append(toasts[i].Key); }
            Ui.Set(toastText, sb.ToString());
        }

        private void BuildMain()
        {
            var left = Ui.Column(mainRoot, "Menu", new Vector2(0, 0), new Vector2(0, 1), new Vector2(50, 50), new Vector2(640, -50), Ui.Card, 28, 10);
            Ui.Label(left, "BERRY SHOOTER", 52, Ui.Accent, TextAnchor.MiddleLeft, 70, true);
            statusText = Ui.Label(left, "", 22, Ui.Muted, TextAnchor.UpperLeft, 96);
            Ui.Btn(left, Tr.T("Play solo"), StartSolo, 66, true, 28);
            Ui.Btn(left, Tr.T("Room list"), () => SetScreen(Page.Rooms), 62, false, 26);
            Ui.Btn(left, Tr.T("Create a Berry room"), OpenHostDialog, 62, false, 26);
            Ui.Btn(left, Tr.T("Join with a room code"), OpenJoinDialog, 62, false, 26);
            Ui.Btn(left, Tr.T("Connect by IP address"), OpenIpDialog, 62, false, 26);
            Ui.Btn(left, Tr.T("Settings"), OpenSettingsDialog, 62, false, 26);
            Ui.Btn(left, Tr.T("Quit game"), Application.Quit, 62, false, 26);

            var right = Ui.Column(mainRoot, "Info", new Vector2(0, 0), new Vector2(1, 1), new Vector2(690, 50), new Vector2(-50, -50), Ui.Card, 22, 10);
            var tabs = Ui.Row(right, 56, 6);
            string[] names = { "Launcher", "Account", "Room", "Network", "Friends", "Lab" };
            for (int i = 0; i < names.Length; i++)
            {
                int t = i;
                Ui.Btn(tabs, Tr.T(names[i]), () => SetTab(t), 56, rightTab == i, 22);
            }
            rightContent = Ui.Scroll(right, -1f);
        }

        private void SetTab(int t)
        {
            rightTab = t;
            rightSig = "";
            if (t == 4 && hub.Friends.Count == 0) hub.LoadFriends();
            RebuildBar();
            RebuildRight();
        }

        private void RebuildBar()
        {
            if (mainRoot == null) return;
            Transform info = mainRoot.Find("Info");
            if (info == null) return;
            Transform tabs = info.GetChild(0);
            string[] names = { "Launcher", "Account", "Room", "Network", "Friends", "Lab" };
            for (int i = 0; i < tabs.childCount && i < names.Length; i++)
            {
                var img = tabs.GetChild(i).GetComponent<Image>();
                if (img != null) img.color = i == rightTab ? Ui.Accent : new Color(1, 1, 1, 0.09f);
            }
        }

        private string RightSignature()
        {
            switch (rightTab)
            {
                case 0: return hub.LauncherInfo();
                case 1: return hub.AccountInfo();
                case 2: return hub.RoomInfo() + PlayersSummary();
                case 3: return hub.NetworkInfo();
                case 4: return hub.Friends.Count + ":" + hub.Busy + ":" + (Berry.Room != null ? Berry.Room.Role.ToString() : "");
                default: return hub.LastTokenInfo + Berry.IsReady;
            }
        }

        private string PlayersSummary()
        {
            if (!Berry.IsReady || Berry.Players.Count == 0) return "";
            var sb = new StringBuilder("\n<b>" + Tr.T("People in the Berry room") + "</b>\n");
            foreach (BerryPlayer p in Berry.Players)
            {
                int ping = hub.BerryPing(p.Username);
                if (ping < 0) ping = p.Stats.NowMs;
                sb.AppendLine(TextUtil.Hex(BerryUtil.TierColorHex(p.Tier), p.DisplayName) + "   " + BerryUtil.RoleText(p.IsHost && p.Role == "member" ? "host" : p.Role, p.AdminLevel, Tr.Lang) + "   " + BerryUtil.PingText(ping));
            }
            return sb.ToString();
        }

        private void RebuildRight()
        {
            if (rightContent == null) return;
            Ui.Clear(rightContent);
            switch (rightTab)
            {
                case 0: Ui.Label(rightContent, hub.LauncherInfo(), 24, Ui.Ink, TextAnchor.UpperLeft); break;
                case 1: Ui.Label(rightContent, hub.AccountInfo(), 24, Ui.Ink, TextAnchor.UpperLeft); break;
                case 2: Ui.Label(rightContent, hub.RoomInfo() + PlayersSummary(), 24, Ui.Ink, TextAnchor.UpperLeft); break;
                case 3: Ui.Label(rightContent, hub.NetworkInfo(), 24, Ui.Ink, TextAnchor.UpperLeft); break;
                case 4: BuildFriendsTab(); break;
                default: BuildLabTab(); break;
            }
            rightSig = RightSignature();
        }

        private void BuildFriendsTab()
        {
            bool canInvite = Berry.IsReady && Berry.IsHost;
            Ui.Btn(rightContent, Tr.T("Refresh friends"), hub.LoadFriends, 54, false, 24);
            if (hub.Friends.Count == 0) Ui.Label(rightContent, Tr.T("No friends loaded yet (needs the 'friends' permission; the launcher asks you once)."), 22, Ui.Muted, TextAnchor.UpperLeft);
            foreach (BerryFriend f in hub.Friends)
            {
                BerryFriend fr = f;
                string state = fr.Playing ? Tr.T("playing") : fr.Online ? Tr.T("online") : Tr.T("offline");
                var b = Ui.ListItem(rightContent, fr.DisplayName + "  (@" + fr.Username + ")", state, false, canInvite && fr.Online ? (UnityEngine.Events.UnityAction)(() => hub.Invite(fr.Username)) : null, 58);
                b.interactable = canInvite && fr.Online;
            }
            if (!canInvite) Ui.Label(rightContent, Tr.T("You can invite friends when you host a Berry room."), 22, Ui.Muted, TextAnchor.UpperLeft);
        }

        private void BuildLabTab()
        {
            Ui.Label(rightContent, Tr.T("Try each part of the SDK. Every button needs its permission in berry-game.json."), 22, Ui.Muted, TextAnchor.UpperLeft);
            Ui.Btn(rightContent, Tr.T("Presence: show a status in the launcher"), () => { hub.Presence(Tr.T("Testing the SDK")); ShowToast(Tr.T("Status sent")); }, 56, false, 24);
            Ui.Btn(rightContent, Tr.T("Unlock a test achievement"), () => { hub.Achievement("lab_test", "SDK test"); ShowToast(Tr.T("Achievement sent")); }, 56, false, 24);
            Ui.Btn(rightContent, Tr.T("Submit a test score"), () => { hub.Score("lab", UnityEngine.Random.Range(1, 100)); ShowToast(Tr.T("Score sent")); }, 56, false, 24);
            Ui.Btn(rightContent, Tr.T("Custom event"), () => { hub.Custom("custom.lab", "text", "hello"); ShowToast(Tr.T("Event sent")); }, 56, false, 24);
            Ui.Btn(rightContent, Tr.T("Get an identity token for my server"), hub.GetToken, 56, false, 24);
            Ui.Btn(rightContent, Tr.T("Copy the token"), hub.CopyToken, 56, false, 24);
            if (hub.LastTokenInfo.Length > 0) Ui.Label(rightContent, hub.LastTokenInfo, 22, Ui.Warn, TextAnchor.UpperLeft);
        }

        private void RefreshSlow()
        {
            if (screen == Page.Menu)
            {
                var sb = new StringBuilder();
                sb.AppendLine(BerryUtil.LinkStateText(Berry.LinkState, Tr.Lang));
                PlayerProfile me = hub.MyProfile();
                sb.Append(Tr.T("Player") + ": " + TextUtil.Hex(BerryUtil.TierColorHex(me.Tier), me.Name));
                Ui.Set(statusText, sb.ToString());
                string sig = RightSignature();
                if (sig != rightSig) RebuildRight();
            }
            else if (screen == Page.Rooms)
            {
                roomsTimer -= 0.25f;
                if (roomsTimer <= 0f) { roomsTimer = 4f; hub.RefreshRooms(roomsAll); }
                RefreshRooms();
            }
            else if (screen == Page.Lobby) RefreshLobby();
            else if (screen == Page.Play) UpdateHud();
            else if (screen == Page.Over) RefreshOver();
        }

        private void BuildRooms()
        {
            var card = Ui.Column(roomsRoot, "Rooms", new Vector2(0, 0), new Vector2(1, 1), new Vector2(120, 60), new Vector2(-120, -60), Ui.Card, 26, 10);
            Ui.Label(card, Tr.T("Berry rooms"), 44, Ui.Ink, TextAnchor.MiddleLeft, 60, true);
            Ui.Label(card, Tr.T("Pick a room to join it. The launcher asks you to confirm."), 22, Ui.Muted, TextAnchor.MiddleLeft, 34);
            var bar = Ui.Row(card, 60);
            Ui.Btn(bar, Tr.T("Refresh"), () => { roomsTimer = 4f; hub.RefreshRooms(roomsAll); }, 60, false, 24);
            Button toggle = null;
            toggle = Ui.Btn(bar, Tr.T(roomsAll ? "Showing: all games" : "Showing: this game"), () => { roomsAll = !roomsAll; Ui.SetLabel(toggle, Tr.T(roomsAll ? "Showing: all games" : "Showing: this game")); roomsSig = ""; hub.RefreshRooms(roomsAll); }, 60, false, 24);
            Ui.Btn(bar, Tr.T("Join with a room code"), OpenJoinDialog, 60, false, 24);
            Ui.Btn(bar, Tr.T("Create a Berry room"), OpenHostDialog, 60, false, 24);
            Ui.Btn(bar, Tr.T("Back"), () => SetScreen(Page.Menu), 60, false, 24);
            roomsStatus = Ui.Label(card, "", 24, Ui.Warn, TextAnchor.MiddleLeft, 40);
            roomsList = Ui.Scroll(card, -1f);
        }

        private void RefreshRooms()
        {
            string state = hub.RoomsLoading ? Tr.T("Loading...") : hub.RoomsError.Length > 0 ? hub.RoomsError : hub.Rooms.Count == 0 && hub.RoomsLoaded ? Tr.T("No open rooms right now. Create one!") : "";
            Ui.Set(roomsStatus, state);
            var sb = new StringBuilder();
            foreach (BerryRoomInfo r in hub.Rooms) sb.Append(r.Id).Append(r.Name).Append(r.Players).Append(r.PingMs).Append(r.Status).Append(r.Locked).Append(';');
            sb.Append(Tr.Version);
            if (sb.ToString() == roomsSig) return;
            roomsSig = sb.ToString();
            Ui.Clear(roomsList);
            foreach (BerryRoomInfo room in hub.Rooms)
            {
                BerryRoomInfo r = room;
                string left = r.Name + (r.Locked ? "   [" + Tr.T("password") + "]" : "") + (r.GameName.Length > 0 && roomsAll ? "   (" + r.GameName + ")" : "");
                string right = r.Host + "   " + r.Players + "/" + r.MaxPlayers + "   " + BerryUtil.PingText(r.PingMs);
                var b = Ui.ListItem(roomsList, left, right, false, () => OnRoomPicked(r), 66);
                b.interactable = !r.IsFull;
            }
        }

        private void OnRoomPicked(BerryRoomInfo r)
        {
            if (r.Locked) OpenPasswordDialog(r);
            else JoinById(r.Id, null);
        }

        private async void JoinById(string id, string password)
        {
            BerryRoomState st = await hub.JoinRoomById(id, password);
            if (st != null) CloseDialog();
        }

        private void BuildLobby()
        {
            var card = Ui.Column(lobbyRoot, "Lobby", new Vector2(0, 0), new Vector2(1, 1), new Vector2(60, 50), new Vector2(-60, -50), Ui.Card, 24, 10);
            lobbyHeader = Ui.Label(card, "", 42, Ui.Ink, TextAnchor.MiddleLeft, 56, true);
            lobbySummary = Ui.Label(card, "", 22, Ui.Muted, TextAnchor.UpperLeft, 66);
            var row = Ui.Row(card, 100, 12);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = -1; le.minHeight = 0; le.flexibleHeight = 1;
            var c1 = Ui.Column(row, "BerryPeople", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.18f), 10, 8);
            Ui.Label(c1, Tr.T("In the Berry room"), 28, Ui.Ink, TextAnchor.MiddleLeft, 42, true);
            berryList = Ui.Scroll(c1, -1f);
            var c2 = Ui.Column(row, "GamePeople", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.18f), 10, 8);
            Ui.Label(c2, Tr.T("Joined the game"), 28, Ui.Ink, TextAnchor.MiddleLeft, 42, true);
            gameList = Ui.Scroll(c2, -1f);
            var c3 = Ui.Column(row, "Details", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.18f), 10, 8);
            Ui.Label(c3, Tr.T("Details"), 28, Ui.Ink, TextAnchor.MiddleLeft, 42, true);
            RectTransform dscroll = Ui.Scroll(c3, -1f);
            detailText = Ui.Label(dscroll, "", 22, Ui.Ink, TextAnchor.UpperLeft);
            lobbyHint = Ui.Label(card, "", 24, Ui.Warn, TextAnchor.MiddleLeft, 40);
            var bar = Ui.Row(card, 70);
            startButton = Ui.Btn(bar, Tr.T("Start the match"), () => { if (session != null) { session.StartMatch(); Sfx.Play(Sfx.Kind.Join); } }, 70, true, 28);
            inviteButton = Ui.Btn(bar, Tr.T("Invite friends"), OpenInviteDialog, 70, false, 26);
            Ui.Btn(bar, Tr.T("Leave"), () => LeaveToMenu(true), 70, false, 28);
        }

        private void RefreshLobby()
        {
            if (session == null || lobbyHeader == null) return;
            World w = session.World;
            var sig = new StringBuilder();
            sig.Append(session.Role).Append(session.Welcomed).Append(session.Transport).Append(selectedBerry).Append(selectedGame).Append(Tr.Version).Append(hub.Net != null ? hub.Net.Peers.Count : 0);
            foreach (PlayerState p in w.Players.Values) sig.Append(p.Id).Append(p.Name).Append(p.PingMs).Append(p.Role).Append(';');
            foreach (BerryPlayer p in Berry.Players) sig.Append(p.Username).Append(p.Ready).Append(p.Stats.NowMs).Append(hub.BerryPing(p.Username)).Append(';');
            string s = sig.ToString();
            if (s == lobbySig) return;
            lobbySig = s;

            string title = Berry.Room != null && Berry.Room.Role != BerryRole.None ? Berry.Room.Name : session.Role == Role.Host ? Tr.T("Hosting") : Tr.T("Match");
            Ui.Set(lobbyHeader, Tr.T("Lobby") + " - " + title);
            var sm = new StringBuilder();
            if (Berry.Room != null && Berry.Room.Role != BerryRole.None)
                sm.Append(Tr.T("Room code") + ": " + Berry.Room.Code + "    " + Tr.T("Room type") + ": " + BerryUtil.RoomTypeText(Berry.Room, Tr.Lang) + "    " + Tr.T("Game") + ": " + Berry.Room.GameName + "\n");
            sm.Append(Tr.T("Transport") + ": " + session.Transport + "    " + Tr.T("Port") + ": " + session.Port + "    " + Tr.T("Host") + ": " + (session.HostName.Length > 0 ? session.HostName : "-"));
            Ui.Set(lobbySummary, sm.ToString());

            Ui.Clear(berryList);
            if (Berry.Players.Count == 0) Ui.Label(berryList, Tr.T("No launcher room (solo, direct IP or test mode)."), 22, Ui.Muted, TextAnchor.UpperLeft);
            foreach (BerryPlayer bp in Berry.Players)
            {
                BerryPlayer p = bp;
                int ping = hub.BerryPing(p.Username);
                if (ping < 0) ping = p.Stats.NowMs;
                string role = BerryUtil.RoleText(p.IsHost && p.Role == "member" ? "host" : p.Role, p.AdminLevel, Tr.Lang);
                string left = TextUtil.Hex(BerryUtil.TierColorHex(p.Tier), p.DisplayName) + "   " + role + (p.Ready ? "   " + Tr.T("ready") : "");
                Ui.ListItem(berryList, left, TextUtil.Hex(Ui.ColorHex(Ui.PingColor(ping)), BerryUtil.PingText(ping)), p.Username == selectedBerry, () => { selectedBerry = p.Username; selectedGame = -1; lobbySig = ""; }, 62);
            }
            Ui.Clear(gameList);
            var ordered = new List<PlayerState>(w.Players.Values);
            ordered.Sort((a, b) => a.Id.CompareTo(b.Id));
            foreach (PlayerState gp in ordered)
            {
                PlayerState p = gp;
                int ping = p.Id == session.LocalId ? (session.Role == Role.Client ? session.PingMs : 0) : p.PingMs;
                string tick = p.Badge == "none" || string.IsNullOrEmpty(p.Badge) ? "" : TextUtil.Hex(BerryUtil.BadgeColorHex(p.Badge), " ✓");
                string left = TextUtil.Hex(BerryUtil.TierColorHex(p.Tier), (p.Club.Length > 0 ? "[" + p.Club + "] " : "") + p.Name) + tick + (p.Id == session.LocalId ? "   " + Tr.T("(you)") : "");
                Ui.ListItem(gameList, left, TextUtil.Hex(Ui.ColorHex(Ui.PingColor(ping)), BerryUtil.PingText(ping)), p.Id == selectedGame, () => { selectedGame = p.Id; selectedBerry = ""; lobbySig = ""; }, 62);
            }
            Ui.Set(detailText, DetailsText());
            bool host = session.Role == Role.Host;
            startButton.gameObject.SetActive(host);
            inviteButton.gameObject.SetActive(Berry.IsReady && Berry.IsHost && Berry.Context.Has("friends"));
            string hint = "";
            if (session.Role == Role.Client && !session.Welcomed) hint = Tr.T("Connecting to the host...");
            else if (session.Role == Role.Client) hint = Tr.T("Waiting for the host to start the match...");
            else if (host && w.Players.Count < 2) hint = Tr.T("You can start alone, or wait for friends to join.");
            Ui.Set(lobbyHint, hint);
        }

        private string DetailsText()
        {
            if (selectedBerry.Length > 0)
            {
                BerryPlayer p = Berry.Context.FindPlayer(selectedBerry);
                if (p != null) return hub.PlayerInfo(p);
            }
            if (selectedGame >= 0 && session != null)
            {
                PlayerState p;
                if (session.World.Players.TryGetValue(selectedGame, out p))
                {
                    var sb = new StringBuilder();
                    sb.Append(BerryHub.Line("Name", TextUtil.Hex(BerryUtil.TierColorHex(p.Tier), p.Name)));
                    sb.Append(BerryHub.Line("Role", p.Role));
                    sb.Append(BerryHub.Line("Subscription tier", p.Tier));
                    sb.Append(BerryHub.Line("Verified tick", p.Badge));
                    sb.Append(BerryHub.Line("Club", BerryHub.Dash(p.Club)));
                    sb.Append(BerryHub.Line("Nation", BerryHub.Dash(p.Country)));
                    sb.Append(BerryHub.Line("Game ping", BerryUtil.PingText(p.Id == session.LocalId ? (session.Role == Role.Client ? session.PingMs : 0) : p.PingMs)));
                    BerryPlayer bp = hub.FindByDisplay(p.Name);
                    if (bp != null) sb.Append("\n" + hub.PlayerInfo(bp));
                    return sb.ToString();
                }
            }
            return Tr.T("Select a player to see the details.");
        }

        public RectTransform OpenDialog(string title, float w, float h)
        {
            CloseDialog();
            Ui.Fill(dialogRoot, "Dim", new Color(0, 0, 0, 0.6f));
            var card = Ui.Column(dialogRoot, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-w / 2f, -h / 2f), new Vector2(w / 2f, h / 2f), Ui.Card, 26, 10);
            Ui.Label(card, title, 38, Ui.Ink, TextAnchor.MiddleLeft, 54, true);
            return card;
        }

        private void CloseDialog() { Ui.Clear(dialogRoot); }

        private void OpenHostDialog()
        {
            var d = OpenDialog(Tr.T("Create a Berry room"), 780, 760);
            Ui.Label(d, Tr.T("Room name"), 22, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var name = Ui.Field(d, Tr.T("3 to 60 characters"), hub.MyProfile().Name + " room", 58);
            Ui.Label(d, Tr.T("Maximum players (2 to 8)"), 22, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var max = Ui.Field(d, "4", "4", 58, InputField.ContentType.IntegerNumber);
            Ui.Label(d, Tr.T("Password (optional, 4 or more characters)"), 22, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var pw = Ui.Field(d, "", "", 58, InputField.ContentType.Password);
            bool pub = false;
            Button pubBtn = null;
            pubBtn = Ui.Btn(d, Tr.T("Listed in the room list: no"), () => { pub = !pub; Ui.SetLabel(pubBtn, Tr.T(pub ? "Listed in the room list: yes" : "Listed in the room list: no")); }, 56, false, 24);
            Ui.Label(d, Tr.T("A room with a password is always private."), 20, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var row = Ui.Row(d, 66);
            Ui.Btn(row, Tr.T("Create"), () =>
            {
                int m;
                if (!int.TryParse(max.text, out m)) m = 4;
                HostFromDialog(name.text, Mathf.Clamp(m, 2, Proto.MaxPlayers), pw.text, pub);
            }, 66, true, 26);
            Ui.Btn(row, Tr.T("Cancel"), CloseDialog, 66, false, 26);
        }

        private async void HostFromDialog(string name, int max, string pw, bool pub)
        {
            BerryRoomState st = await hub.HostRoom(name, max, pw, pub);
            if (st != null) CloseDialog();
        }

        private void OpenJoinDialog()
        {
            var d = OpenDialog(Tr.T("Join with a room code"), 700, 520);
            Ui.Label(d, Tr.T("Room code (or link code)"), 22, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var code = Ui.Field(d, "ABC123", "", 58);
            Ui.Label(d, Tr.T("Password (if the room has one)"), 22, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var pw = Ui.Field(d, "", "", 58, InputField.ContentType.Password);
            var row = Ui.Row(d, 66);
            Ui.Btn(row, Tr.T("Join"), () => JoinFromDialog(code.text, pw.text), 66, true, 26);
            Ui.Btn(row, Tr.T("Cancel"), CloseDialog, 66, false, 26);
        }

        private async void JoinFromDialog(string code, string pw)
        {
            BerryRoomState st = await hub.JoinRoom(code, pw);
            if (st != null) CloseDialog();
        }

        private void OpenPasswordDialog(BerryRoomInfo r)
        {
            var d = OpenDialog(r.Name, 700, 420);
            Ui.Label(d, Tr.T("This room has a password."), 22, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var pw = Ui.Field(d, "", "", 58, InputField.ContentType.Password);
            var row = Ui.Row(d, 66);
            Ui.Btn(row, Tr.T("Join"), () => JoinById(r.Id, pw.text), 66, true, 26);
            Ui.Btn(row, Tr.T("Cancel"), CloseDialog, 66, false, 26);
        }

        private void OpenIpDialog()
        {
            var d = OpenDialog(Tr.T("Connect by IP address"), 700, 520);
            Ui.Label(d, Tr.T("Host address (Berry virtual LAN address or LAN address)"), 22, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var ip = Ui.Field(d, "10.77.0.2", Settings.LastIp, 58);
            Ui.Label(d, Tr.T("Port"), 22, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var port = Ui.Field(d, "7777", Settings.Port.ToString(), 58, InputField.ContentType.IntegerNumber);
            var row = Ui.Row(d, 66);
            Ui.Btn(row, Tr.T("Join"), () =>
            {
                int p;
                if (!int.TryParse(port.text, out p) || p < 1024 || p > 65000) { ShowToast(Tr.T("Port must be between 1024 and 65000.")); return; }
                Settings.LastIp = ip.text.Trim();
                Settings.Port = p;
                CloseDialog();
                StartClientSession(ip.text.Trim(), p);
            }, 66, true, 26);
            Ui.Btn(row, Tr.T("Host here"), () =>
            {
                int p;
                if (!int.TryParse(port.text, out p) || p < 1024 || p > 65000) { ShowToast(Tr.T("Port must be between 1024 and 65000.")); return; }
                Settings.Port = p;
                CloseDialog();
                StartHostSession(p, 4);
            }, 66, false, 26);
            Ui.Btn(row, Tr.T("Cancel"), CloseDialog, 66, false, 26);
        }

        private void OpenSettingsDialog()
        {
            var d = OpenDialog(Tr.T("Settings"), 820, 940);
            var langRow = Ui.Row(d, 58);
            Ui.Label(langRow, Tr.T("Language"), 24, Ui.Ink, TextAnchor.MiddleLeft);
            Ui.Btn(langRow, "فارسی", () => { Tr.Set("fa"); OpenSettingsDialogLater(); }, 58, Tr.IsFa, 24);
            Ui.Btn(langRow, "English", () => { Tr.Set("en"); OpenSettingsDialogLater(); }, 58, !Tr.IsFa, 24);
            var trRow = Ui.Row(d, 58);
            Ui.Label(trRow, Tr.T("Network protocol"), 24, Ui.Ink, TextAnchor.MiddleLeft);
            string[] kinds = { "Auto", "UDP", "TCP" };
            for (int i = 0; i < kinds.Length; i++)
            {
                int k = i;
                Ui.Btn(trRow, kinds[i], () => { Settings.Transport = k; ShowToast(Tr.F("Protocol: {0}", kinds[k])); OpenSettingsDialog(); }, 58, Settings.Transport == i, 24);
            }
            Ui.Label(d, Tr.T("Auto tries UDP first and switches to TCP when UDP does not work (or when Berry reports a relay link)."), 20, Ui.Muted, TextAnchor.MiddleLeft, 46);
            Ui.Label(d, Tr.T("Player name (used when Berry gives no name)"), 22, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var nm = Ui.Field(d, "Player-" + Settings.MockId, Settings.PlayerName, 56);
            nm.onEndEdit.AddListener(v => Settings.PlayerName = TextUtil.CleanName(v, ""));
            Slider(d, Tr.T("Mouse sensitivity"), 0.3f, 8f, Settings.Sensitivity, v => Settings.Sensitivity = v);
            Slider(d, Tr.T("Volume"), 0f, 1f, Settings.Volume, v => Settings.Volume = v);
            Slider(d, Tr.T("Field of view"), 60f, 110f, Settings.Fov, v => Settings.Fov = v);
            Ui.Label(d, Tr.T("Game port (host)"), 22, Ui.Muted, TextAnchor.MiddleLeft, 30);
            var port = Ui.Field(d, "7777", Settings.Port.ToString(), 56, InputField.ContentType.IntegerNumber);
            port.onEndEdit.AddListener(v => { int p; if (int.TryParse(v, out p)) Settings.Port = p; });
            Ui.Btn(d, Tr.T("Close"), () => { Settings.Save(); CloseDialog(); }, 64, true, 26);
        }

        private void OpenSettingsDialogLater()
        {
            builtVersion = -1;
            reopenSettings = true;
        }

        private bool reopenSettings;

        private void Slider(Transform parent, string label, float min, float max, float value, UnityEngine.Events.UnityAction<float> change)
        {
            var row = Ui.Row(parent, 46);
            Ui.Label(row, label, 22, Ui.Ink, TextAnchor.MiddleLeft);
            Ui.Sld(row, min, max, value, change, 46f);
        }

        private void OpenInviteDialog()
        {
            if (hub.Friends.Count == 0) hub.LoadFriends();
            var d = OpenDialog(Tr.T("Invite a friend"), 720, 760);
            var list = Ui.Scroll(d, 520);
            if (hub.Friends.Count == 0) Ui.Label(list, Tr.T("No friends loaded yet (needs the 'friends' permission; the launcher asks you once)."), 22, Ui.Muted, TextAnchor.UpperLeft);
            foreach (BerryFriend f in hub.Friends)
            {
                BerryFriend fr = f;
                var b = Ui.ListItem(list, fr.DisplayName, fr.Online ? Tr.T("online") : Tr.T("offline"), false, () => hub.Invite(fr.Username), 58);
                b.interactable = fr.Online;
            }
            var row = Ui.Row(d, 64);
            Ui.Btn(row, Tr.T("Refresh friends"), () => { hub.LoadFriends(); CloseDialog(); }, 64, false, 24);
            Ui.Btn(row, Tr.T("Close"), CloseDialog, 64, true, 24);
        }
    }
}
