using System;
using System.Collections.Generic;
using System.Net.Sockets;
using BerrySdk;
using UnityEngine;
using UnityEngine.UI;

namespace BerryShooter
{
    public enum Page { Menu, Rooms, Lobby, Play, Over }

    public sealed partial class GameApp : MonoBehaviour
    {
        private BerryHub hub;
        private GameSession session;
        private SceneBuilder scene;
        private Camera cam;
        private Canvas canvas;
        private Transform gunModel;
        private Renderer muzzle;
        private Page screen = Page.Menu;
        private bool paused;
        private float yaw, pitch, recoil, menuOrbit, uiTimer, muzzleTimer;
        private int lastKills, lastHp = 100, lastWave;
        private readonly HashSet<string> unlocked = new HashSet<string>();
        private bool scoresSent, argsConsumed, langSynced;
        private string blockedRoomId = "";
        private int builtVersion = -1;
        private bool inputWarned;

        private void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 144;
            hub = new BerryHub();
            hub.Toast += ShowToast;
            Sfx.Init(gameObject);
            Ui.EnsureEventSystem();
            SetupCamera();
            canvas = Ui.MakeCanvas("UI", 10);
            scene = new SceneBuilder(transform);
            RebuildUi();
            SetScreen(Page.Menu);
        }

        private void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            cam.enabled = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 300f;
            cam.cullingMask = ~0;
            cam.depth = 0;
            cam.orthographic = false;
            cam.rect = new Rect(0, 0, 1, 1);
            cam.allowMSAA = true;
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(g.GetComponent<Collider>());
            g.name = "Gun";
            g.transform.SetParent(cam.transform, false);
            g.transform.localPosition = new Vector3(0.24f, -0.24f, 0.55f);
            g.transform.localScale = new Vector3(0.09f, 0.11f, 0.5f);
            g.GetComponent<Renderer>().sharedMaterial = Mats.Solid(new Color(0.16f, 0.17f, 0.2f));
            var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(m.GetComponent<Collider>());
            m.name = "Muzzle";
            m.transform.SetParent(g.transform, false);
            m.transform.localPosition = new Vector3(0, 0, 0.62f);
            m.transform.localScale = new Vector3(1.4f, 1.4f, 0.5f);
            muzzle = m.GetComponent<Renderer>();
            muzzle.sharedMaterial = Mats.Glow(new Color(1f, 0.85f, 0.35f));
            muzzle.enabled = false;
            gunModel = g.transform;
            gunModel.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Berry.Ready += OnBerryReady;
            Berry.ContextChanged += OnBerryContext;
            Berry.Pushed += OnBerryPushed;
            Berry.Disconnected += OnBerryDisconnected;
        }

        private void OnDisable()
        {
            Berry.Ready -= OnBerryReady;
            Berry.ContextChanged -= OnBerryContext;
            Berry.Pushed -= OnBerryPushed;
            Berry.Disconnected -= OnBerryDisconnected;
        }

        private void Start() { if (Berry.IsReady) OnBerryReady(); }

        private void OnDestroy() { if (session != null) session.Close(); }

        private void OnApplicationQuit() { if (session != null) session.Close(); }

        private void OnBerryReady()
        {
            if (!langSynced)
            {
                langSynced = true;
                Tr.Init(Berry.Context.Language);
            }
            ShowToast(Berry.IsMock ? Tr.T("Berry launcher session not found: test mode with fake data") : Tr.T("Connected to the Berry launcher"));
            hub.Presence(Tr.T("In the menu"));
            hub.RefreshNetwork();
            TryAutoStart();
            RefreshMainSoon();
        }

        private void OnBerryContext(BerryContext c)
        {
            if (!BerryUtil.IsInRoom(c)) blockedRoomId = "";
            TryAutoStart();
            RefreshMainSoon();
        }

        private void OnBerryPushed(string name, object data)
        {
            if (name != null && name.StartsWith("player.")) ShowToast(Tr.T("Launcher") + ": " + name.Replace("player.", "player "));
        }

        private void OnBerryDisconnected(string reason) { ShowToast(Tr.F("Launcher connection closed ({0}). The game keeps running.", reason)); }

        private void TryAutoStart()
        {
            if (session != null || !Berry.IsReady || Berry.IsMock) return;
            if (!argsConsumed)
            {
                argsConsumed = true;
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-connect" && i + 1 < args.Length)
                    {
                        string[] hp = args[i + 1].Split(':');
                        int port = Settings.Port, parsed;
                        if (hp.Length > 1 && int.TryParse(hp[1], out parsed) && parsed > 0 && parsed < 65536) port = parsed;
                        StartClientSession(hp[0], port);
                        return;
                    }
                    if (args[i] == "-host")
                    {
                        StartHostSession(Settings.Port, RoomSize());
                        return;
                    }
                }
            }
            if (!BerryUtil.IsInRoom(Berry.Context)) return;
            if (Berry.Room.Id.Length > 0 && Berry.Room.Id == blockedRoomId) return;
            string addr = BerryUtil.HostAddressToConnect(Berry.Context);
            if (addr != null) StartClientSession(addr, Settings.Port);
            else if (Berry.IsHost) StartHostSession(Settings.Port, RoomSize());
        }

        private static int RoomSize() { return Berry.Room != null && Berry.Room.MaxPlayers > 1 ? Mathf.Min(Berry.Room.MaxPlayers, Proto.MaxPlayers) : 4; }

        public void StartSolo()
        {
            EndSession(false);
            session = GameSession.Solo(hub.MyProfile());
            AttachSession();
            hub.Presence(Tr.T("Playing solo"));
        }

        public void StartHostSession(int port, int max)
        {
            EndSession(false);
            try { session = GameSession.Host(hub.MyProfile(), port, max, Settings.Transport); }
            catch (Exception e)
            {
                ShowToast(Tr.F("Cannot open port {0}: {1}", port, e.Message));
                session = null;
                return;
            }
            AttachSession();
            ShowToast(Tr.F("Hosting on port {0} ({1})", session.Port, session.Transport));
            hub.Presence(Tr.T("Hosting a match"));
        }

        public void StartClientSession(string ip, int port)
        {
            EndSession(false);
            try { session = GameSession.Client(hub.MyProfile(), ip, port, Settings.Transport, hub.ShouldPreferTcp()); }
            catch (Exception e)
            {
                ShowToast(Tr.F("Cannot connect: {0}", e.Message));
                session = null;
                return;
            }
            AttachSession();
            ShowToast(Tr.F("Connecting to {0}:{1} ...", ip, port));
            hub.Presence(Tr.T("Joining a match"));
        }

        private void AttachSession()
        {
            session.Message += OnSessionMessage;
            session.Tracer += OnSessionTracer;
            scoresSent = false; lastKills = 0; lastHp = 100; lastWave = 0; paused = false;
            feed.Clear();
            yaw = 0f; pitch = 0f;
            selectedBerry = ""; selectedGame = -1;
            SetScreen(Page.Lobby);
        }

        public void LeaveToMenu(bool leaveBerryRoom)
        {
            if (Berry.IsReady && Berry.Room != null) blockedRoomId = Berry.Room.Id;
            EndSession(leaveBerryRoom);
            SetScreen(Page.Menu);
            hub.Presence(Tr.T("In the menu"));
        }

        private void EndSession(bool leaveBerryRoom)
        {
            if (session == null) return;
            session.Close();
            session = null;
            scene.ClearAll();
            gunModel.gameObject.SetActive(false);
            if (leaveBerryRoom && Berry.IsReady && !Berry.IsMock && BerryUtil.IsInRoom(Berry.Context)) hub.LeaveRoom();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (builtVersion != Tr.Version) RebuildUi();
            hub.Tick(dt);
            if (session != null)
            {
                if (session.Problem != null)
                {
                    ShowToast(Tr.T(session.Problem));
                    LeaveToMenu(false);
                }
                else RunSession(dt);
            }
            else MenuCamera(dt);
            uiTimer -= dt;
            if (uiTimer <= 0f)
            {
                uiTimer = 0.25f;
                RefreshSlow();
            }
            UpdateToasts(dt);
            if (muzzleTimer > 0f)
            {
                muzzleTimer -= dt;
                if (muzzle != null) muzzle.enabled = muzzleTimer > 0f;
            }
        }

        private void RunSession(float dt)
        {
            Page want = Page.Lobby;
            if (session.World.State == GameState.Playing) want = Page.Play;
            else if (session.World.State == GameState.Over) want = Page.Over;
            if (session.Role == Role.Client && (!session.Welcomed || session.LocalPlayer == null)) want = Page.Lobby;
            if (want != screen) SetScreen(want);

            LocalInput input = ReadInput(want == Page.Play && !paused);
            session.Update(dt, input);
            scene.Sync(session.World, session.LocalId);

            PlayerState me = session.LocalPlayer;
            if (me != null) ReactToChanges(me);
            if (screen == Page.Play)
            {
                UpdateHudFast();
                PlayCamera(me);
                UpdateTags();
            }
            else
            {
                HideTags();
                gunModel.gameObject.SetActive(false);
                MenuCamera(dt);
            }
        }

        private LocalInput ReadInput(bool active)
        {
            var i = new LocalInput { Yaw = yaw, Pitch = pitch };
            if (!active)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return i;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            float mx = 0f, my = 0f;
            try
            {
                mx = Input.GetAxis("Mouse X");
                my = Input.GetAxis("Mouse Y");
            }
            catch (InvalidOperationException)
            {
                if (!inputWarned) { inputWarned = true; ShowToast(Tr.T("Old Input Manager is off. Set Edit > Project Settings > Player > Active Input Handling to Both.")); }
                return i;
            }
            if (float.IsNaN(mx) || float.IsInfinity(mx)) mx = 0f;
            if (float.IsNaN(my) || float.IsInfinity(my)) my = 0f;
            yaw = Mathf.Repeat(yaw + mx * Settings.Sensitivity, 360f);
            pitch = Mathf.Clamp(pitch - my * Settings.Sensitivity, -85f, 85f);
            i.Yaw = yaw;
            i.Pitch = pitch;
            PlayerState me = session.LocalPlayer;
            if (me != null && me.Alive)
            {
                i.Mx = Input.GetAxisRaw("Horizontal");
                i.Mz = Input.GetAxisRaw("Vertical");
                i.Fire = Input.GetMouseButton(0);
                i.Jump = Input.GetKey(KeyCode.Space);
                i.Sprint = Input.GetKey(KeyCode.LeftShift);
            }
            if (Input.GetKeyDown(KeyCode.Escape)) { paused = true; ApplyPauseUi(); }
            return i;
        }

        private static bool Finite(Vector3 v) { return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z)); }

        private void PlayCamera(PlayerState me)
        {
            Vector3 pos = session.LocalPos;
            if (!Finite(pos)) pos = ArenaLayout.SpawnPoint(session.LocalId);
            Vector3 flat = ArenaLayout.Resolve(pos, 0.3f);
            pos.x = flat.x;
            pos.z = flat.z;
            cam.fieldOfView = Settings.Fov;
            if (me != null && !me.Alive)
            {
                gunModel.gameObject.SetActive(false);
                cam.transform.position = pos + new Vector3(0, 7f, -5f);
                cam.transform.rotation = Quaternion.Euler(55f, yaw, 0f);
                return;
            }
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 14f);
            cam.transform.position = pos + new Vector3(0, World.EyeHeight, 0);
            cam.transform.rotation = Quaternion.Euler(pitch - recoil, yaw, 0f);
            gunModel.gameObject.SetActive(true);
            gunModel.localPosition = new Vector3(0.24f, -0.24f, 0.55f - recoil * 0.025f);
        }

        private void MenuCamera(float dt)
        {
            menuOrbit += dt * 0.12f;
            cam.transform.position = new Vector3(Mathf.Cos(menuOrbit) * 30f, 13f, Mathf.Sin(menuOrbit) * 30f);
            cam.transform.LookAt(new Vector3(0, 1.5f, 0));
            cam.fieldOfView = 60f;
        }

        private void ReactToChanges(PlayerState me)
        {
            if (me.Kills > lastKills)
            {
                Sfx.Play(Sfx.Kind.Kill, 0.8f);
                if (me.Kills >= 1) Unlock("first_blood", "First blood");
                if (me.Kills >= 10) Unlock("ten_kills", "Ten kills in one match");
                if (me.Kills >= 40) Unlock("forty_kills", "Forty kills in one match");
            }
            lastKills = me.Kills;
            if (me.Hp < lastHp) { Sfx.Play(Sfx.Kind.Hurt); damageFlash = 0.35f; }
            lastHp = me.Hp;
            if (session.World.Wave != lastWave)
            {
                lastWave = session.World.Wave;
                if (lastWave > 0)
                {
                    Sfx.Play(Sfx.Kind.Wave, 0.7f);
                    hub.Presence(Tr.F("Wave {0} - {1} kills", lastWave, me.Kills));
                }
                if (lastWave >= 5) Unlock("wave_5", "Reached wave 5");
                if (lastWave >= 10) Unlock("wave_10", "Reached wave 10");
                if (lastWave >= 2 && session.Role != Role.Solo) Unlock("team_player", "Survived a wave together");
            }
            if (session.World.State == GameState.Over && !scoresSent)
            {
                scoresSent = true;
                Sfx.Play(Sfx.Kind.Over);
                hub.Score("kills", me.Kills);
                hub.Score("best_wave", session.World.Wave);
                hub.Presence(Tr.F("Game over - wave {0}", session.World.Wave));
            }
            if (session.World.State != GameState.Over) scoresSent = false;
        }

        private void Unlock(string id, string title)
        {
            if (!unlocked.Add(id)) return;
            hub.Achievement(id, title);
            ShowToast(Tr.T("Achievement") + ": " + Tr.T(title));
        }

        private void OnSessionMessage(string coded) { AddFeed(Tr.Feed(coded)); }

        private void OnSessionTracer(int shooter, Vector3 a, Vector3 b, bool hit)
        {
            bool mine = session != null && shooter == session.LocalId;
            Vector3 from = a;
            if (mine && cam != null) from = cam.transform.position + cam.transform.forward * 0.9f + cam.transform.right * 0.2f - cam.transform.up * 0.15f;
            scene.Tracer(from, b, shooter < 0, hit);
            if (shooter >= 0) Sfx.Play(Sfx.Kind.Shot, mine ? 0.5f : 0.2f);
            if (mine)
            {
                recoil = Mathf.Min(recoil + 1.2f, 5f);
                muzzleTimer = 0.05f;
                if (muzzle != null) muzzle.enabled = true;
                if (hit) Sfx.Play(Sfx.Kind.Hit, 0.7f);
            }
        }

        private void SetScreen(Page s)
        {
            screen = s;
            ApplyScreens();
            if (s == Page.Rooms) hub.RefreshRooms(roomsAll);
            if (s != Page.Play) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        private void RefreshMainSoon() { uiTimer = 0f; }

        private bool IsHost { get { return session != null && (session.Role == Role.Host || session.Role == Role.Solo); } }

        private void RebuildUi()
        {
            builtVersion = Tr.Version;
            tagPool.Clear();
            toasts.Clear();
            foreach (Transform child in canvas.transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            BuildAllScreens();
            ApplyScreens();
            rightSig = "";
            lobbySig = "";
            roomsSig = "";
            if (reopenSettings)
            {
                reopenSettings = false;
                OpenSettingsDialog();
            }
        }
    }
}
