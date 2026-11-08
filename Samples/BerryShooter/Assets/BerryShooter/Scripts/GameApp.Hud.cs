using System.Collections.Generic;
using System.Text;
using BerrySdk;
using UnityEngine;
using UnityEngine.UI;

namespace BerryShooter
{
    public sealed partial class GameApp
    {
        private Image flash, hpFill;
        private Text hpText, waveText, pingText, feedText, deadText, scoreText, overText, overHint, crosshair;
        private RectTransform scoreboard;
        private Button againButton;
        private float damageFlash;
        private float hpShown = 1f;
        private int pauseFrame = -1;
        private readonly List<KeyValuePair<string, float>> feed = new List<KeyValuePair<string, float>>();
        private readonly List<Text> tagPool = new List<Text>();

        private void AddFeed(string text)
        {
            feed.Add(new KeyValuePair<string, float>(text, Time.unscaledTime + 7f));
            if (feed.Count > 6) feed.RemoveAt(0);
        }

        private void BuildHud()
        {
            flash = Ui.Fill(hudRoot, "Damage", new Color(1, 0, 0, 0)).GetComponent<Image>();
            flash.raycastTarget = false;
            crosshair = Ui.Label(hudRoot, "+", 44, new Color(1, 1, 1, 0.9f), TextAnchor.MiddleCenter);
            Place(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-30, -30), new Vector2(30, 30));

            var hpBox = Ui.Panel(hudRoot, "HP", new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40), new Vector2(460, 100), new Color(0, 0, 0, 0.55f));
            hpFill = Ui.Panel(hpBox, "Fill", Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4), Ui.Good).GetComponent<Image>();
            hpText = Ui.Stretch(hpBox, "100", 32, Ui.Ink, TextAnchor.MiddleCenter);

            var top = Ui.Panel(hudRoot, "Wave", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-380, -96), new Vector2(380, -24), new Color(0, 0, 0, 0.5f));
            waveText = Ui.Stretch(top, "", 28, Ui.Ink, TextAnchor.MiddleCenter);

            var pingBox = Ui.Panel(hudRoot, "Ping", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-560, -190), new Vector2(-30, -24), new Color(0, 0, 0, 0.5f));
            pingText = Ui.Stretch(pingBox, "", 21, Ui.Ink, TextAnchor.UpperLeft);

            feedText = Ui.Label(hudRoot, "", 24, Ui.Ink, TextAnchor.UpperRight);
            Place(feedText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-700, -460), new Vector2(-30, -200));

            deadText = Ui.Label(hudRoot, Tr.T("YOU ARE DOWN") + "\n" + Tr.T("Your team can bring you back at the next respawn"), 40, Ui.Bad, TextAnchor.MiddleCenter);
            Place(deadText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-700, 120), new Vector2(700, 320));

            scoreboard = Ui.Column(hudRoot, "Scoreboard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520, -320), new Vector2(520, 320), Ui.Card, 24, 8);
            Ui.Label(scoreboard, Tr.T("SCOREBOARD"), 34, Ui.Ink, TextAnchor.MiddleLeft, 48, true);
            scoreText = Ui.Label(scoreboard, "", 26, Ui.Ink, TextAnchor.UpperLeft);
            scoreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 480;
        }

        private void BuildPause()
        {
            Ui.Fill(pauseRoot, "Dim", new Color(0, 0, 0, 0.6f));
            var card = Ui.Column(pauseRoot, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, -320), new Vector2(280, 320), Ui.Card, 28, 12);
            Ui.Label(card, Tr.T("PAUSED"), 44, Ui.Ink, TextAnchor.MiddleLeft, 60, true);
            Ui.Label(card, Tr.T("The match keeps running for the others."), 22, Ui.Muted, TextAnchor.UpperLeft, 56);
            Ui.Btn(card, Tr.T("Resume"), ResumeGame, 70, true, 30);
            Ui.Btn(card, Tr.T("Settings"), OpenSettingsDialog, 64);
            Ui.Btn(card, Tr.T("Leave match"), () => LeaveToMenu(true), 64);
            Ui.Btn(card, Tr.T("Quit game"), Application.Quit, 64);
        }

        private void BuildOver()
        {
            Ui.Fill(overRoot, "Dim", new Color(0, 0, 0, 0.55f));
            var card = Ui.Column(overRoot, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520, -400), new Vector2(520, 400), Ui.Card, 28, 10);
            Ui.Label(card, Tr.T("GAME OVER"), 52, Ui.Accent, TextAnchor.MiddleLeft, 70, true);
            overText = Ui.Label(card, "", 26, Ui.Ink, TextAnchor.UpperLeft);
            overText.gameObject.AddComponent<LayoutElement>().preferredHeight = 420;
            overHint = Ui.Label(card, "", 22, Ui.Warn, TextAnchor.MiddleLeft, 34);
            var row = Ui.Row(card, 70);
            againButton = Ui.Btn(row, Tr.T("Play again"), () => { if (session != null) { session.Restart(); Sfx.Play(Sfx.Kind.Join); } }, 70, true, 28);
            Ui.Btn(row, Tr.T("Main menu"), () => LeaveToMenu(true), 70, false, 28);
        }

        private void ResumeGame()
        {
            paused = false;
            ApplyPauseUi();
        }

        private void ApplyPauseUi()
        {
            pauseRoot.gameObject.SetActive(paused && screen == Page.Play);
            if (paused) pauseFrame = Time.frameCount;
            if (!paused) CloseDialog();
            if (paused) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        private void HandlePauseKey()
        {
            if (paused && Time.frameCount > pauseFrame && Input.GetKeyDown(KeyCode.Escape) && dialogRoot.childCount == 0) ResumeGame();
        }

        private void UpdateHudFast()
        {
            PlayerState me = session.LocalPlayer;
            damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.unscaledDeltaTime);
            var c = flash.color;
            c.a = damageFlash;
            flash.color = c;
            float hp = me != null ? Mathf.Clamp01(me.Hp / 100f) : 0f;
            hpShown = Mathf.MoveTowards(hpShown, hp, Time.unscaledDeltaTime * 2f);
            hpFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, hpShown), 1f);
            hpFill.color = Color.Lerp(Ui.Bad, Ui.Good, hpShown);
            scoreboard.gameObject.SetActive(Input.GetKey(KeyCode.Tab) && !paused);
            deadText.gameObject.SetActive(me != null && !me.Alive);
            crosshair.gameObject.SetActive(!paused && me != null && me.Alive);
            HandlePauseKey();
        }

        private void UpdateHud()
        {
            if (session == null) return;
            World w = session.World;
            PlayerState me = session.LocalPlayer;
            Ui.Set(hpText, me != null ? me.Hp.ToString() : "0");
            Ui.Set(waveText, Tr.F("WAVE {0}     enemies {1}     team kills {2}", w.Wave, w.Remaining, w.TeamKills));

            var sb = new StringBuilder();
            if (session.Role == Role.Client)
            {
                int game = session.PingMs;
                sb.AppendLine(Tr.T("Game ping to host") + ": " + TextUtil.Hex(Ui.ColorHex(Ui.PingColor(game)), BerryUtil.PingText(game)));
                BerryPlayer host = hub.FindByDisplay(session.HostName);
                int berry = host != null ? hub.BerryPing(host.Username) : -1;
                sb.AppendLine(Tr.T("Berry ping to host") + ": " + TextUtil.Hex(Ui.ColorHex(Ui.PingColor(berry)), BerryUtil.PingText(berry)));
            }
            else sb.AppendLine(session.Role == Role.Solo ? Tr.T("Solo game (no network)") : Tr.F("You host. Port {0}   players {1}", session.Port, w.Players.Count));
            sb.AppendLine(Tr.T("Transport") + ": " + session.Transport);
            BerryNetwork n = hub.Net ?? Berry.Network;
            if (n != null && n.Peers.Count > 0) sb.AppendLine(Tr.T("Virtual LAN") + ": " + n.State + "   " + Tr.T("Worst ping") + " " + BerryUtil.PingText(BerryUtil.WorstPing(n)));
            sb.Append(Tr.F("Traffic {0} KB in / {1} KB out", session.BytesIn / 1024, session.BytesOut / 1024));
            Ui.Set(pingText, sb.ToString());

            feed.RemoveAll(f => f.Value < Time.unscaledTime);
            var fb = new StringBuilder();
            foreach (var f in feed) fb.AppendLine(f.Key);
            Ui.Set(feedText, fb.ToString());

            var ranked = new List<PlayerState>(w.Players.Values);
            ranked.Sort((a, b) => b.Kills.CompareTo(a.Kills));
            var s = new StringBuilder();
            s.AppendLine("<size=20>" + Tr.T("PLAYER") + "      " + Tr.T("KILLS") + "   " + Tr.T("DEATHS") + "   " + Tr.T("PING") + "</size>");
            foreach (PlayerState p in ranked)
            {
                string tick = p.Badge == "none" || string.IsNullOrEmpty(p.Badge) ? "" : TextUtil.Hex(BerryUtil.BadgeColorHex(p.Badge), " ✓");
                int ping = p.Id == session.LocalId ? (session.Role == Role.Client ? session.PingMs : 0) : p.PingMs;
                s.AppendLine(TextUtil.Hex(BerryUtil.TierColorHex(p.Tier), (p.Club.Length > 0 ? "[" + p.Club + "] " : "") + p.Name) + tick + "      " + p.Kills + "       " + p.Deaths + "       " + BerryUtil.PingText(ping) + (p.Alive ? "" : "   (" + Tr.T("down") + ")"));
            }
            Ui.Set(scoreText, s.ToString());
        }

        private void RefreshOver()
        {
            if (session == null || overText == null) return;
            World w = session.World;
            PlayerState me = session.LocalPlayer;
            var sb = new StringBuilder();
            sb.AppendLine(Tr.F("You reached wave {0}. Team kills: {1}", w.Wave, w.TeamKills));
            if (me != null) sb.AppendLine(Tr.F("Your kills: {0}   deaths: {1}", me.Kills, me.Deaths));
            sb.AppendLine("");
            var ranked = new List<PlayerState>(w.Players.Values);
            ranked.Sort((a, b) => b.Kills.CompareTo(a.Kills));
            int rank = 1;
            foreach (PlayerState p in ranked) sb.AppendLine("#" + rank++ + "  " + TextUtil.Hex(BerryUtil.TierColorHex(p.Tier), p.Name) + "   " + p.Kills + " " + Tr.T("kills"));
            sb.AppendLine("");
            sb.AppendLine("<size=20>" + Tr.T("Your scores were sent to Berry (boards 'kills' and 'best_wave').") + "</size>");
            Ui.Set(overText, sb.ToString());
            bool authority = session.Role != Role.Client;
            againButton.gameObject.SetActive(authority);
            Ui.Set(overHint, authority ? "" : Tr.T("Waiting for the host to start a new match..."));
        }

        private Text NewTag()
        {
            Text t = Ui.Label(tagsRoot, "", 22, Ui.Ink, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.rectTransform.sizeDelta = new Vector2(420, 34);
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.zero;
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.9f);
            o.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        private void UpdateTags()
        {
            if (session == null || cam == null) return;
            int used = 0;
            Canvas c = canvas;
            float scale = c.scaleFactor <= 0f ? 1f : c.scaleFactor;
            foreach (PlayerView v in scene.Views)
            {
                if (v == null || !v.Visible || !v.Alive) continue;
                PlayerState p;
                if (!session.World.Players.TryGetValue(v.Id, out p)) continue;
                Vector3 sp = cam.WorldToScreenPoint(v.HeadPos);
                if (sp.z <= 0.3f || sp.z > 60f) continue;
                while (tagPool.Count <= used) tagPool.Add(NewTag());
                Text t = tagPool[used++];
                t.gameObject.SetActive(true);
                t.rectTransform.anchoredPosition = new Vector2(sp.x / scale, sp.y / scale);
                string tick = p.Badge == "none" || string.IsNullOrEmpty(p.Badge) ? "" : TextUtil.Hex(BerryUtil.BadgeColorHex(p.Badge), " ✓");
                Ui.Set(t, TextUtil.Hex(BerryUtil.TierColorHex(p.Tier), (p.Club.Length > 0 ? "[" + p.Club + "] " : "") + p.Name) + tick + "  " + TextUtil.Hex(Ui.ColorHex(Color.Lerp(Ui.Bad, Ui.Good, p.Hp / 100f)), p.Hp.ToString()));
            }
            for (int i = used; i < tagPool.Count; i++) tagPool[i].gameObject.SetActive(false);
        }

        private void HideTags()
        {
            foreach (Text t in tagPool) if (t != null) t.gameObject.SetActive(false);
        }
    }
}
