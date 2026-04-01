using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace X360Joystic
{
    public partial class MainWindow : Window
    {
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        static extern uint XInputGetState(uint dwUserIndex, ref XINPUT_STATE pState);

        [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
        static extern uint XInputSetState(uint dwUserIndex, ref XINPUT_VIBRATION pVibration);

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetBatteryInformation")]
        static extern uint XInputGetBatteryInformation(uint dwUserIndex, byte devType,
                                                       ref XINPUT_BATTERY_INFORMATION pBatteryInformation);

        const uint ERROR_SUCCESS = 0;
        const int  DEADZONE      = 8000;

        // Overlay brush: semi-transparent teal for pressed state
        static readonly Brush PressedOverlay = MakeBrushA(200, 0x4E, 0xC9, 0xB0);
        static readonly Brush ConnectedFg    = MakeBrush(0x4E, 0xC9, 0xB0);

        static Brush MakeBrush(byte r, byte g, byte b)
        {
            var br = new SolidColorBrush(Color.FromRgb(r, g, b));
            br.Freeze(); return br;
        }
        static Brush MakeBrushA(byte a, byte r, byte g, byte b)
        {
            var br = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            br.Freeze(); return br;
        }

        private readonly DispatcherTimer _timer;
        private DispatcherTimer?         _vibStopTimer;
        private uint _playerIndex = uint.MaxValue;

        // Cached state ??only update UI when values change
        private ushort _prevButtons;
        private byte   _prevLT, _prevRT;
        private short  _prevLX, _prevLY, _prevRX, _prevRY;
        private uint   _prevPkt;
        private byte   _prevBatteryType  = 0xFF;
        private byte   _prevBatteryLevel = 0xFF;
        private int    _batteryPollTick;

        // Stick dot neutral centers ??Left stick upper-left, Right stick lower-right
        public MainWindow()
        {
            InitializeComponent();
            ApplyOverlayLayout();
            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void ApplyOverlayLayout()
        {
            SetOverlayPosition(OvLT, ControllerOverlayLayout.LT);
            SetOverlayPosition(OvRT, ControllerOverlayLayout.RT);
            SetOverlayPosition(OvLB, ControllerOverlayLayout.LB);
            SetOverlayPosition(OvRB, ControllerOverlayLayout.RB);

            SetOverlayPosition(OvBack, ControllerOverlayLayout.Back);
            SetOverlayPosition(OvStart, ControllerOverlayLayout.Start);

            SetOverlayPosition(OvL3, ControllerOverlayLayout.L3);
            SetOverlayPosition(OvR3, ControllerOverlayLayout.R3);

            SetOverlayPosition(OvUp, ControllerOverlayLayout.DPadUp);
            SetOverlayPosition(OvDown, ControllerOverlayLayout.DPadDown);
            SetOverlayPosition(OvLeft, ControllerOverlayLayout.DPadLeft);
            SetOverlayPosition(OvRight, ControllerOverlayLayout.DPadRight);

            SetOverlayPosition(OvY, ControllerOverlayLayout.Y);
            SetOverlayPosition(OvX, ControllerOverlayLayout.X);
            SetOverlayPosition(OvB, ControllerOverlayLayout.B);
            SetOverlayPosition(OvA, ControllerOverlayLayout.A);

            SetOverlayPosition(LSStickDot, ControllerOverlayLayout.LeftStickDotNeutral);
            SetOverlayPosition(RSStickDot, ControllerOverlayLayout.RightStickDotNeutral);
        }

        private static void SetOverlayPosition(UIElement element, OverlayPoint point)
        {
            Canvas.SetLeft(element, point.Left);
            Canvas.SetTop(element, point.Top);
        }

        private void OnTick(object? sender, EventArgs e)
        {
            try
            {
                Poll();
                if (BottomErrorText.Visibility == Visibility.Visible)
                {
                    BottomErrorText.Text = string.Empty;
                    BottomErrorText.ToolTip = null;
                    BottomErrorText.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                BottomErrorText.Text = $"입력 경고: {ex.Message}";
                BottomErrorText.ToolTip = ex.ToString();
                BottomErrorText.Visibility = Visibility.Visible;
            }
        }

        private void Poll()
        {
            if (_playerIndex == uint.MaxValue)
            {
                _playerIndex = FindConnectedController();
                if (_playerIndex == uint.MaxValue)
                {
                    StatusText.Text       = "컨트롤러를 찾을 수 없습니다.";
                    StatusText.Foreground = Brushes.OrangeRed;
                    return;
                }
                Vibrate(_playerIndex, 20000, 20000, 250);
                _batteryPollTick = 179;
            }

            var state = new XINPUT_STATE();
            if (XInputGetState(_playerIndex, ref state) != ERROR_SUCCESS)
            {
                StatusText.Text       = "컨트롤러 연결이 끊어졌습니다.";
                StatusText.Foreground = Brushes.OrangeRed;
                _playerIndex      = uint.MaxValue;
                _prevPkt          = 0;
                _prevBatteryType  = 0xFF;
                _prevBatteryLevel = 0xFF;
                BatteryBar.Text = BatteryText.Text = string.Empty;
                return;
            }

            StatusText.Text       = "연결됨 · 월평동 이상목 X360 조이스틱";
            StatusText.Foreground = ConnectedFg;

            if (++_batteryPollTick >= 180) { _batteryPollTick = 0; UpdateBattery(); }

            if (state.dwPacketNumber == _prevPkt) return;
            _prevPkt = state.dwPacketNumber;
            UpdateUI(ref state.Gamepad);
        }

        private void UpdateUI(ref XINPUT_GAMEPAD pad)
        {
            // ?? Buttons ??
            if (pad.wButtons != _prevButtons)
            {
                SetOverlay(OvA,     pad.wButtons, _prevButtons, (ushort)GamepadButtons.A);
                SetOverlay(OvB,     pad.wButtons, _prevButtons, (ushort)GamepadButtons.B);
                SetOverlay(OvX,     pad.wButtons, _prevButtons, (ushort)GamepadButtons.X);
                SetOverlay(OvY,     pad.wButtons, _prevButtons, (ushort)GamepadButtons.Y);
                SetOverlay(OvLB,    pad.wButtons, _prevButtons, (ushort)GamepadButtons.LEFT_SHOULDER);
                SetOverlay(OvRB,    pad.wButtons, _prevButtons, (ushort)GamepadButtons.RIGHT_SHOULDER);
                SetOverlay(OvStart, pad.wButtons, _prevButtons, (ushort)GamepadButtons.START);
                SetOverlay(OvBack,  pad.wButtons, _prevButtons, (ushort)GamepadButtons.BACK);
                SetOverlay(OvUp,    pad.wButtons, _prevButtons, (ushort)GamepadButtons.DPAD_UP);
                SetOverlay(OvDown,  pad.wButtons, _prevButtons, (ushort)GamepadButtons.DPAD_DOWN);
                SetOverlay(OvLeft,  pad.wButtons, _prevButtons, (ushort)GamepadButtons.DPAD_LEFT);
                SetOverlay(OvRight, pad.wButtons, _prevButtons, (ushort)GamepadButtons.DPAD_RIGHT);
                SetOverlay(OvL3,    pad.wButtons, _prevButtons, (ushort)GamepadButtons.LEFT_THUMB);
                SetOverlay(OvR3,    pad.wButtons, _prevButtons, (ushort)GamepadButtons.RIGHT_THUMB);
                _prevButtons = pad.wButtons;
            }

            // ?? Triggers (analog opacity) ??
            if (pad.bLeftTrigger != _prevLT)
            {
                OvLT.Opacity = 0.05 + 0.80 * (pad.bLeftTrigger / 255.0);
                LTBar.Value  = pad.bLeftTrigger;
                LTText.Text  = $"{pad.bLeftTrigger} / 255";
                _prevLT = pad.bLeftTrigger;
            }
            if (pad.bRightTrigger != _prevRT)
            {
                OvRT.Opacity = 0.05 + 0.80 * (pad.bRightTrigger / 255.0);
                RTBar.Value  = pad.bRightTrigger;
                RTText.Text  = $"{pad.bRightTrigger} / 255";
                _prevRT = pad.bRightTrigger;
            }

            // ?? Sticks (dot position + text) ??
            if (pad.sThumbLX != _prevLX || pad.sThumbLY != _prevLY)
            {
                int lx = ApplyDeadzone(pad.sThumbLX);
                int ly = ApplyDeadzone(pad.sThumbLY);
                (double l, double t) = CalculateStickDotPosition(lx, ly, ControllerOverlayLayout.LeftStickCenter);
                Canvas.SetLeft(LSStickDot, l);
                Canvas.SetTop(LSStickDot, t);
                LSXText.Text = lx.ToString();
                LSYText.Text = ly.ToString();
                _prevLX = pad.sThumbLX;
                _prevLY = pad.sThumbLY;
            }
            if (pad.sThumbRX != _prevRX || pad.sThumbRY != _prevRY)
            {
                int rx = ApplyDeadzone(pad.sThumbRX);
                int ry = ApplyDeadzone(pad.sThumbRY);
                (double l, double t) = CalculateStickDotPosition(rx, ry, ControllerOverlayLayout.RightStickCenter);
                Canvas.SetLeft(RSStickDot, l);
                Canvas.SetTop(RSStickDot, t);
                RSXText.Text = rx.ToString();
                RSYText.Text = ry.ToString();
                _prevRX = pad.sThumbRX;
                _prevRY = pad.sThumbRY;
            }
        }

        // Flip Ellipse fill only when pressed-state changes
        private static void SetOverlay(Ellipse e, ushort cur, ushort prev, ushort mask)
        {
            bool was = (prev & mask) != 0;
            bool now = (cur  & mask) != 0;
            if (was == now) return;
            e.Fill   = now ? PressedOverlay : Brushes.Transparent;
            e.Stroke = now ? PressedOverlay : new SolidColorBrush(Color.FromArgb(51, 255, 255, 255));
        }

        static int ApplyDeadzone(short v) => Math.Abs((int)v) < DEADZONE ? 0 : v;

        static (double Left, double Top) CalculateStickDotPosition(int axisX, int axisY, OverlayPoint center)
        {
            // XInput range is [-32768, 32767], so normalize with sign-aware divisors.
            double nx = axisX >= 0 ? axisX / 32767.0 : axisX / 32768.0;
            double ny = axisY >= 0 ? axisY / 32767.0 : axisY / 32768.0;

            nx = Math.Clamp(nx, -1.0, 1.0);
            ny = Math.Clamp(ny, -1.0, 1.0);

            double left = center.Left + nx * ControllerOverlayLayout.StickRange - ControllerOverlayLayout.DotHalf;
            double top = center.Top - ny * ControllerOverlayLayout.StickRange - ControllerOverlayLayout.DotHalf;

            if (!double.IsFinite(left)) left = center.Left - ControllerOverlayLayout.DotHalf;
            if (!double.IsFinite(top)) top = center.Top - ControllerOverlayLayout.DotHalf;

            return (left, top);
        }

        static uint FindConnectedController()
        {
            var s = new XINPUT_STATE();
            for (uint i = 0; i < 4; i++)
                if (XInputGetState(i, ref s) == ERROR_SUCCESS) return i;
            return uint.MaxValue;
        }

        // ?? Battery ??????????????????????????????????????????????????????????

                private void UpdateBattery()
        {
            var info = new XINPUT_BATTERY_INFORMATION();
            if (XInputGetBatteryInformation(_playerIndex, Battery.DEVTYPE_GAMEPAD, ref info) != ERROR_SUCCESS)
                return;
            if (info.BatteryType == _prevBatteryType && info.BatteryLevel == _prevBatteryLevel) return;
            _prevBatteryType  = info.BatteryType;
            _prevBatteryLevel = info.BatteryLevel;

            if (info.BatteryType == Battery.TYPE_WIRED)
            {
                BatteryBar.Text = "▰▰▰▰▰";
                BatteryBar.Foreground = ConnectedFg;
                BatteryText.Text = "배터리 충분";
                BatteryText.Foreground = ConnectedFg;
                return;
            }
            if (info.BatteryType == Battery.TYPE_DISCONNECTED)
            {
                BatteryBar.Text = BatteryText.Text = string.Empty;
                return;
            }

            (string bar, string lbl, Brush col) = info.BatteryLevel switch
            {
                Battery.LEVEL_FULL   => ("▰▰▰▰▰", "충분", ConnectedFg),
                Battery.LEVEL_MEDIUM => ("▰▰▰▱▱", "보통", MakeBrush(0xFF, 0xD7, 0x00)),
                Battery.LEVEL_LOW    => ("▰▰▱▱▱", "부족", MakeBrush(0xFF, 0x80, 0x00)),
                Battery.LEVEL_EMPTY  => ("▱▱▱▱▱", "방전", Brushes.OrangeRed),
                _                    => ("?????", "알 수 없음", Brushes.Gray),
            };
            BatteryBar.Text = bar;
            BatteryBar.Foreground = col;
            BatteryText.Text = $"배터리 {lbl}";
            BatteryText.Foreground = col;
        }
        // Vibration UI ?????????????????????????????????????????????????????

        private void MotorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LeftMotorText == null || RightMotorText == null) return;
            LeftMotorText.Text  = $"{(int)(LeftMotorSlider.Value  / 65535.0 * 100):D3}%";
            RightMotorText.Text = $"{(int)(RightMotorSlider.Value / 65535.0 * 100):D3}%";
        }

        private void VibLeftBtn_Click (object s, RoutedEventArgs e) =>
            StartVibration((ushort)LeftMotorSlider.Value, 0, "왼쪽 모터 (저주파)");
        private void VibRightBtn_Click(object s, RoutedEventArgs e) =>
            StartVibration(0, (ushort)RightMotorSlider.Value, "오른쪽 모터 (고주파)");
        private void VibBothBtn_Click (object s, RoutedEventArgs e) =>
            StartVibration((ushort)LeftMotorSlider.Value, (ushort)RightMotorSlider.Value, "양쪽 동시");

        private int _altStep;
        private void VibAltBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_playerIndex == uint.MaxValue) { VibStatusText.Text = "컨트롤러 없음"; return; }
            CancelAutoStop();
            _altStep = 0;
            var L = (ushort)LeftMotorSlider.Value;
            var R = (ushort)RightMotorSlider.Value;
            void Step()
            {
                if (_altStep >= 6) { StopVibration(); return; }
                bool lt = (_altStep % 2 == 0);
                SetVib(lt ? L : (ushort)0, lt ? (ushort)0 : R);
                VibStatusText.Text = lt ? $"→ 왼쪽 [{_altStep+1}/6]" : $"→ 오른쪽 [{_altStep+1}/6]";
                _altStep++;
            }
            Step();
            _vibStopTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(400) };
            _vibStopTimer.Tick += (_, _) => Step();
            _vibStopTimer.Start();
        }

        private void VibStopBtn_Click(object sender, RoutedEventArgs e) { CancelAutoStop(); StopVibration(); }

        private void StartVibration(ushort left, ushort right, string label)
        {
            if (_playerIndex == uint.MaxValue) { VibStatusText.Text = "컨트롤러 없음"; return; }
            uint r = SetVib(left, right);
            if (r != ERROR_SUCCESS) { VibStatusText.Text = $"실패 0x{r:X4}"; return; }
            VibStatusText.Text = $"{label}  [{left:N0}/{right:N0}]";
            CancelAutoStop();
            _vibStopTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(1500) };
            _vibStopTimer.Tick += (s, _) => { ((DispatcherTimer)s!).Stop(); StopVibration(); };
            _vibStopTimer.Start();
        }

        private void StopVibration()
        {
            CancelAutoStop();
            if (_playerIndex == uint.MaxValue) return;
            uint r = SetVib(0, 0);
            VibStatusText.Text = r == ERROR_SUCCESS ? "진동 정지" : $"정지 실패 0x{r:X4}";
        }

        private void CancelAutoStop() { _vibStopTimer?.Stop(); _vibStopTimer = null; }

        private uint SetVib(ushort l, ushort r)
        {
            var v = new XINPUT_VIBRATION { wLeftMotorSpeed = l, wRightMotorSpeed = r };
            return XInputSetState(_playerIndex, ref v);
        }

        void Vibrate(uint index, ushort left, ushort right, int ms)
        {
            var v = new XINPUT_VIBRATION { wLeftMotorSpeed = left, wRightMotorSpeed = right };
            if (XInputSetState(index, ref v) != ERROR_SUCCESS || ms <= 0) return;
            var t = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(ms) };
            t.Tick += (s, _) => { ((DispatcherTimer)s!).Stop(); var stop = new XINPUT_VIBRATION(); XInputSetState(index, ref stop); };
            t.Start();
        }
    }
}

