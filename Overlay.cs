using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

using GameOverlay.Drawing;
using GameOverlay.Windows;

namespace CSGO_External_Overlay
{
    public class Overlay : IDisposable
    {
        private readonly GraphicsWindow _window;

        private readonly Dictionary<string, SolidBrush> _brushes;
        private readonly Dictionary<string, Font> _fonts;
        private readonly Dictionary<string, Image> _images;

        struct GameInfo
        {
            public Memory.WindowData windowData;

            public int server;
            public int client;
            public int engine;

            public int client_state;

            public float aimbot_distance;
            public float aimbot_min_distance;
            public Vector3 aimbot_Angle;
        }
        private GameInfo csgo;

        public Overlay()
        {
            // 注意：CSGO为32位程序
            Memory.Initialize("csgo");
            Memory.SetForegroundWindow();

            csgo.windowData = Memory.GetGameWindowData();

            csgo.server = Memory.GetModule("server.dll");
            csgo.client = Memory.GetModule("client.dll");
            csgo.engine = Memory.GetModule("engine.dll");

            /////////////////////////////////////////////

            _brushes = new Dictionary<string, SolidBrush>();
            _fonts = new Dictionary<string, Font>();
            _images = new Dictionary<string, Image>();

            var gfx = new Graphics()
            {
                VSync = false,
                MeasureFPS = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true
            };

            _window = new GraphicsWindow(csgo.windowData.Left, csgo.windowData.Top, csgo.windowData.Width, csgo.windowData.Height, gfx)
            {
                FPS = 300,
                IsTopmost = true,
                IsVisible = true
            };

            _window.SetupGraphics += _window_SetupGraphics;
            _window.DrawGraphics += _window_DrawGraphics;
            _window.DestroyGraphics += _window_DestroyGraphics;
        }

        private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            if (e.RecreateResources)
            {
                foreach (var pair in _brushes) pair.Value.Dispose();
                foreach (var pair in _images) pair.Value.Dispose();
            }

            _brushes["black"] = gfx.CreateSolidBrush(0, 0, 0);
            _brushes["white"] = gfx.CreateSolidBrush(255, 255, 255);
            _brushes["red"] = gfx.CreateSolidBrush(255, 0, 98);
            _brushes["green"] = gfx.CreateSolidBrush(0, 128, 0);
            _brushes["blue"] = gfx.CreateSolidBrush(30, 144, 255);
            _brushes["background"] = gfx.CreateSolidBrush(0x33, 0x36, 0x3F);
            _brushes["grid"] = gfx.CreateSolidBrush(255, 255, 255, 0.2f);
            _brushes["deepPink"] = gfx.CreateSolidBrush(247, 63, 147, 255);

            _brushes["transparency"] = gfx.CreateSolidBrush(0, 0, 0, 0);

            if (e.RecreateResources) return;

            _fonts["arial"] = gfx.CreateFont("Arial", 12);
            _fonts["Microsoft YaHei"] = gfx.CreateFont("Microsoft YaHei", 12);
            _fonts["consolas"] = gfx.CreateFont("Consolas", 14);
        }

        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;
            gfx.ClearScene(_brushes["transparency"]);
            ResizeWindow(gfx);

            // 绘制边框
            //gfx.DrawRectangle(_brushes["white"], 0, 0, _window.Width, _window.Height, 0.7f);

            // 绘制帧数
            gfx.DrawText(_fonts["Microsoft YaHei"], 12, _brushes["blue"], 10, _window.Height / 2.5f,
                $"FPS：{gfx.FPS}\nFrameTime：{e.FrameTime}\nFrameCount：{e.FrameCount}\nDeltaTime：{e.DeltaTime}");

            ///////////////////////////////////////////////////////
            /////////////////   用户自定义绘制区域  /////////////////
            ///////////////////////////////////////////////////////

            csgo.aimbot_min_distance = 8848;

            csgo.client_state = Memory.ReadMemory<int>(csgo.engine + Offsets.signatures.dwClientState);

            float lViewAngles_Y = Memory.ReadMemory<float>(csgo.client_state + Offsets.signatures.dwClientState_ViewAngles);
            float lViewAngles_X = Memory.ReadMemory<float>(csgo.client_state + Offsets.signatures.dwClientState_ViewAngles + 0x4);

            // 绘制区域
            gfx.DrawText(_fonts["Microsoft YaHei"], 12.0f, _brushes["blue"], 10, _window.Height / 2,
                $"鼠标Y：{lViewAngles_Y}\n鼠标X：{lViewAngles_X}");

            int player_count = Memory.ReadMemory<int>(csgo.server + 0xB28950);
            int player_team = Memory.ReadMemory<int>(Memory.ReadMemory<int>(csgo.server + 0xA7F7E4) + 0x314);

            Vector3 player_Pos = GetBonePosition(Memory.ReadMemory<int>(csgo.client + Offsets.signatures.dwEntityList), 8);

            for (int i = 1; i <= player_count; i++)
            {
                int server_entity = Memory.ReadMemory<int>(csgo.server + 0xA7F7E4 + i * 0x18);

                int server_entity_health = Memory.ReadMemory<int>(server_entity + 0x230);
                if (server_entity_health <= 0) continue;
                int server_entity_team = Memory.ReadMemory<int>(server_entity + 0x314);
                if (server_entity_team == player_team) continue;

                Vector3 server_v3PlayerPos = new Vector3
                {
                    X = Memory.ReadMemory<float>(server_entity + 0x1DC),
                    Y = Memory.ReadMemory<float>(server_entity + 0x1DC + 0x4),
                    Z = Memory.ReadMemory<float>(server_entity + 0x1DC + 0x8)
                };

                Vector2 server_v2PlayerPos = WorldToScreen(server_v3PlayerPos);
                Vector2 server_v2BoxWH = GetBoxWH(server_v3PlayerPos, 75.0f, 0.0f);

                if (!IsNullVector2(server_v2PlayerPos))
                {
                    float box_height = server_v2BoxWH.X;
                    float box_wight = server_v2BoxWH.Y;

                    // 测试
                    //gfx.DrawText(_fonts["Microsoft YaHei"], 10, _brushes["red"], v2PedPos.X, v2PedPos.Y, $"#");

                    // 2D方框
                    gfx.DrawRectangle(_brushes["white"], Rectangle.Create(
                        server_v2PlayerPos.X - box_wight / 2,
                        server_v2PlayerPos.Y - box_height,
                        box_wight,
                        box_height), 0.7f);

                    // 射线
                    gfx.DrawLine(_brushes["white"],
                        csgo.windowData.Width / 2,
                        0,
                        server_v2PlayerPos.X,
                        server_v2PlayerPos.Y - box_height, 0.7f);

                    // 血条
                    gfx.DrawRectangle(_brushes["white"], Rectangle.Create(
                        server_v2PlayerPos.X - box_wight / 2 - box_wight / 8,
                        server_v2PlayerPos.Y,
                        box_wight / 10,
                        box_height * -1.0f), 0.7f);
                    gfx.FillRectangle(_brushes["green"], Rectangle.Create(
                        server_v2PlayerPos.X - box_wight / 2 - box_wight / 8,
                        server_v2PlayerPos.Y,
                        box_wight / 10,
                        box_height * server_entity_health / 100 * -1.0f));

                    // 血量
                    gfx.DrawText(_fonts["Microsoft YaHei"], 8, _brushes["white"],
                        server_v2PlayerPos.X - box_wight / 2,
                        server_v2PlayerPos.Y + box_wight / 8 - box_wight / 10,
                        $"HP: {server_entity_health:0}/{100}\n" +
                        $"ID: {i}");
                }

                int client_entity = Memory.ReadMemory<int>(csgo.client + Offsets.signatures.dwEntityList + i * 0x10);

                int client_entity_health = Memory.ReadMemory<int>(client_entity + Offsets.netvars.m_iHealth);
                if (client_entity_health <= 0) continue;
                int client_entity_team = Memory.ReadMemory<int>(client_entity + Offsets.netvars.m_iTeamNum);
                if (client_entity_team == player_team) continue;

                //for (int j = 0; j < 80; j++)
                //{
                //    Vector2 v2Bone = WorldToScreen(GetBonePosition(entity, j));
                //    gfx.DrawText(_fonts["arial"], 12.0f, _brushes["green"], v2Bone.X, v2Bone.Y, j.ToString());
                //}

                // buggly | https://www.unknowncheats.me/forum/counterstrike-global-offensive/218227-skeleton-esp-bone-ids.html
                // skeleton esp | Bone Ids : https://www.unknowncheats.me/forum/attachments/counterstrike-global-offensive/13413d1480413236-csgo-bone-id-8f0bb9a93378477388dee312b2fad4ca-png

                Vector2 v2Bone2 = WorldToScreen(GetBonePosition(client_entity, 2));
                Vector2 v2Bone1 = WorldToScreen(GetBonePosition(client_entity, 1));

                if (!IsNullVector2(v2Bone1) && !IsNullVector2(v2Bone2))
                {
                    float BoxHeight = v2Bone1.Y - v2Bone2.Y;
                    float BoxWidth = BoxHeight / 2;

                    gfx.DrawRectangle(_brushes["red"], v2Bone1.X - BoxWidth / 2, v2Bone1.Y, v2Bone1.X - BoxWidth / 2 + BoxWidth, v2Bone1.Y - BoxHeight, 1);
                }

                Vector3 v3Bone8 = GetBonePosition(client_entity, 8);
                Vector2 v2Bone8 = WorldToScreen(v3Bone8);

                if (!IsNullVector2(v2Bone8))
                {
                    csgo.aimbot_distance = (float)Math.Sqrt(Math.Pow(v2Bone8.X - csgo.windowData.Width / 2, 2) + Math.Pow(v2Bone8.Y - csgo.windowData.Height / 2, 2));

                    if (csgo.aimbot_distance < csgo.aimbot_min_distance)
                    {
                        csgo.aimbot_min_distance = csgo.aimbot_distance;
                        csgo.aimbot_Angle = ClampAngle(CalcAngle(player_Pos, v3Bone8));
                    }
                }

                DrawBone(gfx, 8, 7, client_entity);

                DrawBone(gfx, 7, 40, client_entity);
                DrawBone(gfx, 40, 41, client_entity);

                DrawBone(gfx, 7, 12, client_entity);
                DrawBone(gfx, 12, 13, client_entity);

                DrawBone(gfx, 7, 6, client_entity);
                DrawBone(gfx, 6, 4, client_entity);
                DrawBone(gfx, 4, 3, client_entity);

                DrawBone(gfx, 3, 74, client_entity);
                DrawBone(gfx, 74, 75, client_entity);

                DrawBone(gfx, 3, 67, client_entity);
                DrawBone(gfx, 67, 68, client_entity);
            }

            if (Convert.ToBoolean(WinAPI.GetAsyncKeyState(0xA0) & 0x8000) && csgo.aimbot_min_distance != 8848)
            {
                Memory.WriteMemory<float>(Memory.ReadMemory<int>(csgo.engine + 0x589FE4) + 0x4D90, csgo.aimbot_Angle.X);
                Memory.WriteMemory<float>(Memory.ReadMemory<int>(csgo.engine + 0x589FE4) + 0x4D94, csgo.aimbot_Angle.Y);
            }
        }

        private void ResizeWindow(Graphics gfx)
        {
            // 窗口移动跟随
            csgo.windowData = Memory.GetGameWindowData();
            _window.X = csgo.windowData.Left;
            _window.Y = csgo.windowData.Top;
            _window.Width = csgo.windowData.Width;
            _window.Height = csgo.windowData.Height;
            gfx.Resize(_window.Width, _window.Height);
        }

        private void DrawBone(Graphics gfx, int bone0, int bone1, int entity)
        {
            Vector2 v2Bone0 = WorldToScreen(GetBonePosition(entity, bone0));
            Vector2 v2Bone1 = WorldToScreen(GetBonePosition(entity, bone1));

            if (!IsNullVector2(v2Bone0) && !IsNullVector2(v2Bone1))
            {
                gfx.DrawLine(_brushes["red"], v2Bone0.X, v2Bone0.Y, v2Bone1.X, v2Bone1.Y, 1);
            }
        }

        private Vector3 GetBonePosition(int entity, int BoneID)
        {
            int boneMatrix = Memory.ReadMemory<int>(entity + Offsets.netvars.m_dwBoneMatrix);
            Vector3 head_pos;
            head_pos.X = Memory.ReadMemory<float>(boneMatrix + 0x30 * BoneID + 0x0C);
            head_pos.Y = Memory.ReadMemory<float>(boneMatrix + 0x30 * BoneID + 0x1C);
            head_pos.Z = Memory.ReadMemory<float>(boneMatrix + 0x30 * BoneID + 0x2C);
            return head_pos;
        }

        private bool IsNullVector2(Vector2 vector)
        {
            if (vector == new Vector2(0, 0))
                return true;
            return false;
        }

        private Vector2 WorldToScreen(Vector3 target)
        {
            Vector2 _worldToScreenPos;
            Vector3 _camera;

            float[] viewmatrix = Memory.ReadMatrix<float>(csgo.client + Offsets.signatures.dwViewMatrix, 16);

            _camera.Z = viewmatrix[8] * target.X + viewmatrix[9] * target.Y + viewmatrix[10] * target.Z + viewmatrix[11];
            if (_camera.Z < 0.001f)
                return new Vector2(0, 0);

            _camera.X = csgo.windowData.Width / 2;
            _camera.Y = csgo.windowData.Height / 2;
            _camera.Z = 1 / _camera.Z;

            _worldToScreenPos.X = viewmatrix[0] * target.X + viewmatrix[1] * target.Y + viewmatrix[2] * target.Z + viewmatrix[3];
            _worldToScreenPos.Y = viewmatrix[4] * target.X + viewmatrix[5] * target.Y + viewmatrix[6] * target.Z + viewmatrix[7];

            _worldToScreenPos.X = _camera.X + _camera.X * _worldToScreenPos.X * _camera.Z;
            _worldToScreenPos.Y = _camera.Y - _camera.Y * _worldToScreenPos.Y * _camera.Z;

            return _worldToScreenPos;
        }

        private Vector2 GetBoxWH(Vector3 target, float topOffset, float bottomOffset)
        {
            Vector2 _worldToScreenPos;
            Vector3 _camera;

            float[] viewmatrix = Memory.ReadMatrix<float>(csgo.client + Offsets.signatures.dwViewMatrix, 16);

            _camera.Z = viewmatrix[8] * target.X + viewmatrix[9] * target.Y + viewmatrix[10] * target.Z + viewmatrix[11];
            if (_camera.Z < 0.001f)
                return new Vector2(0, 0);

            _camera.Y = csgo.windowData.Height / 2;
            _camera.Z = 1 / _camera.Z;

            _worldToScreenPos.X = viewmatrix[4] * target.X + viewmatrix[5] * target.Y + viewmatrix[6] * (target.Z + topOffset) + viewmatrix[7];
            _worldToScreenPos.Y = viewmatrix[4] * target.X + viewmatrix[5] * target.Y + viewmatrix[6] * (target.Z + bottomOffset) + viewmatrix[7];

            _worldToScreenPos.X = _camera.Y - _camera.Y * _worldToScreenPos.X * _camera.Z;
            _worldToScreenPos.Y = _camera.Y - _camera.Y * _worldToScreenPos.Y * _camera.Z;

            _worldToScreenPos.X = _worldToScreenPos.Y - _worldToScreenPos.X;
            _worldToScreenPos.Y = _worldToScreenPos.X * 0.5f;

            return _worldToScreenPos;
        }

        private float DegToRad(float deg) { return (float)(deg * (Math.PI / 180f)); }
        private float RadToDeg(float deg) { return (float)(deg * (180f / Math.PI)); }

        private Vector3 CalcAngle(Vector3 src, Vector3 dst)
        {
            Vector3 ret = new Vector3();
            Vector3 vDelta = src - dst;
            float fHyp = (float)Math.Sqrt((vDelta.X * vDelta.X) + (vDelta.Y * vDelta.Y));

            ret.X = RadToDeg((float)Math.Atan(vDelta.Z / fHyp));
            ret.Y = RadToDeg((float)Math.Atan(vDelta.Y / vDelta.X));

            if (vDelta.X >= 0.0f)
                ret.Y += 180.0f;
            return ret;
        }

        private Vector3 ClampAngle(Vector3 qaAng)
        {

            if (qaAng.X > 89.0f && qaAng.X <= 180.0f)
                qaAng.X = 89.0f;

            while (qaAng.X > 180.0f)
                qaAng.X = qaAng.X - 360.0f;

            if (qaAng.X < -89.0f)
                qaAng.X = -89.0f;

            while (qaAng.Y > 180.0f)
                qaAng.Y = qaAng.Y - 360.0f;

            while (qaAng.Y < -180.0f)
                qaAng.Y = qaAng.Y + 360.0f;

            return qaAng;
        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            foreach (var pair in _brushes) pair.Value.Dispose();
            foreach (var pair in _fonts) pair.Value.Dispose();
            foreach (var pair in _images) pair.Value.Dispose();
        }

        /////////////////////////////////////////////

        public void Run()
        {
            _window.Create();
            _window.Join();
        }

        ~Overlay()
        {
            Dispose(false);
        }

        #region IDisposable Support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _window.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
